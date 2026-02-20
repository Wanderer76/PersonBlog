import { useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import VideoPlayer from '../../components/VideoPlayer/VideoPlayer';
import './VideoPage.css';
import React, { useEffect, useRef, useState } from 'react';
import API, { BaseApUrl } from '../../lib/api/client';
import { getLocalDateTime } from '../../shared/LocalDate';
import logo from '../../defaultProfilePic.png';
import { JwtTokenService } from '../../shared/TokenStrorage.js';
import SmallVideoCard from '../../components/VideoCards/SmallVideoCard';
import SideBar from '../../components/sidebar/SideBar';
import CommentsList from '../../components/comment/Comment';
import { getVideo } from '@/lib/api/generated/video/video';

const VideoPage = function (props) {
    const searchParams = useParams();
    const queryParams = new URLSearchParams(window.location.search)
    const [time, setTime] = useState(queryParams.get('time'))
    const [isLoading, setIsLoading] = useState(true);
    const [recommendations, setRecommendations] = useState([]);
    const watchedTime = useRef(0);
    const [comments, setComments] = useState([]);
    const [commentCount, setCommentCount] = useState(0);
    const [newCommentText, setNewCommentText] = useState('');
    const limit = 40;
    const navigate = useNavigate();
    const nextThresholdRef = useRef(30);
    const lastCallTimeRef = useRef(0);

    const [post, setPostData] = useState({
        id: null,
        previewUrl: null,
        createdAt: null,
        viewCount: 0,
        description: null,
        title: null,
        type: 1,
        videoData: {
            id: null,
            length: 0,
            contentType: null,
            objectName: null
        },
        isProcessed: true,
    });

    const [userView, setUserView] = useState({
        isViewed: true,
        isLike: false,
        hasSubscription: false
    })

    const [blog, setBlog] = useState({
    });

    function getUrl(blogId, postId, objectName) {
        if (postId !== null && objectName !== null)
            return `${BaseApUrl}/video/Video/${blogId}/${postId}/${objectName}`;
    }

    useEffect(() => {
        watchedTime.current = 0;

        API.get(`/video/Video/video/${searchParams.postId}`)
            .then(response => {
                if (response.status === 200) {
                    if (response.data.userPostInfo) {
                        setTime(response.data.userPostInfo.watchedTime)
                    }
                    setPostData(response.data.post ?? null);
                    setBlog(response.data.blog ?? null);
                    setUserView(response.data.userPostInfo ?? null)
                    setIsLoading(false)
                }
            });

        API.get(`/video/recommendations?page=${1}&limit=${limit}&currentPostId=${searchParams.postId}`)
            .then(response => {
                if (response.status === 200) {
                    setRecommendations(response.data);
                }
            });

        API.get(`video/api/Comments/list?postId=${searchParams.postId}`)
            .then(response => {
                if (response.status === 200) {
                    setComments(response.data.comments);
                    setCommentCount(response.data.count);
                }
            });
    }, [searchParams.postId])


    const handleAddComment = async (replyId = null) => {
        if (!newCommentText.trim()) return;

        try {
            const response = await API.post(`video/api/Comments/create`, {
                postId: searchParams.postId,
                replyTo: replyId,
                text: newCommentText
            });

            if (response.status === 200) {
                API.get(`video/api/Comments/list?postId=${searchParams.postId}`)
                    .then(response => {
                        if (response.status === 200) {
                            setComments(response.data.comments);
                            setCommentCount(response.data.count);
                        }
                    });
                setNewCommentText('');
            }
        } catch (error) {
            console.error("Ошибка при добавлении комментария:", error);
        }
    };



    async function setReaction(isLike) {
        await API.post(`/video/Video/setReaction/${post.id}?isLike=${isLike}`)

        if (isLike === true) {
            if (userView.isLike === true) {
                // Если лайк уже был поставлен - снимаем его
                setPostData((prev) => ({
                    ...prev,
                    likeCount: prev.likeCount - 1,
                }));
            } else {
                // Если был дизлайк или не было реакции
                setPostData((prev) => ({
                    ...prev,
                    likeCount: prev.likeCount + 1,
                    dislikeCount: userView.isLike === false
                        ? prev.dislikeCount - 1
                        : prev.dislikeCount,
                }));
            }
        } else {
            if (userView.isLike === false) {
                // Если дизлайк уже был поставлен - снимаем его
                setPostData((prev) => ({
                    ...prev,
                    dislikeCount: prev.dislikeCount - 1,
                }));
            } else {
                // Если был лайк или не было реакции
                setPostData((prev) => ({
                    ...prev,
                    dislikeCount: prev.dislikeCount + 1,
                    likeCount: userView.isLike === true
                        ? prev.likeCount - 1
                        : prev.likeCount,
                }));
            }
        }
        setUserView((prev) => ({
            ...prev,
            isLike: prev.isLike === isLike ? null : isLike
        }))
    }

    async function setView(player) {
        if (!JwtTokenService.isAuth()) return;

        const duration = player.duration();
        if (!duration || duration <= 0) return;

        // Минимальный интервал между вызовами (в секундах)
        const minInterval = duration <= 60 ? Math.max(2, duration * 0.1) : 5;

        const now = Date.now();
        const timeSinceLastCall = (now - lastCallTimeRef.current) / 1000;

        // Пропускаем вызов, если прошло меньше минимального интервала
        if (timeSinceLastCall < minInterval) {
            return;
        }

        const currentWatchedTime = player.currentTime();
        const currentPercent = (currentWatchedTime / duration) * 100;

        const sendViewData = async () => {
            try {
                await getVideo().postVideoSetView({
                    postId: post.id,
                    time: currentWatchedTime,
                    isComplete: currentWatchedTime >= duration * 0.85,
                })
                // await API.post('/video/Video/setView', {
                //     postId: post.id,
                //     time: currentWatchedTime,
                //     isComplete: currentWatchedTime >= duration * 0.85,
                // });
            } catch (e) {
                console.error("Ошибка при отправке данных просмотра:", e);
            }
        };


        if (currentPercent >= nextThresholdRef.current) {
            sendViewData();
            lastCallTimeRef.current = now; // Обновляем время последнего вызова

            // Обновляем порог
            if (nextThresholdRef.current === 30) {
                nextThresholdRef.current = 40;
            } else {
                nextThresholdRef.current += 10;
            }

            if (nextThresholdRef.current > 100) {
                nextThresholdRef.current = Infinity; // Больше не вызывать
            }
        }
    }

    async function setViewEnd(player) {
        if (!JwtTokenService.isAuth())
            return;

        const watchedTime = player.currentTime();
        await API.post('/video/Video/setView', {
            postId: post.id,
            time: watchedTime,
            isComplete: true
        });
    }

    async function handleSubscribe() {
        const current = userView?.hasSubscription;
        var isError = false;
        await API.post(`video/api/Subscriber/${userView?.hasSubscription ? 'unsubscribe' : 'subscribe'}/${blog.id}`).catch(e => {
            isError = true
            alert(e.response.data)
        }).finally(() => {
            if (!isError) {
                setUserView((prev => ({
                    ...prev,
                    hasSubscription: !current
                })))
                setBlog((prev => ({
                    ...prev,
                    subscribersCount: !current ? prev.subscribersCount + 1 : prev.subscribersCount - 1
                })))
            }
        })
    }

    async function onPaused(player) {
        if (!JwtTokenService.isAuth())
            return;
        const currentWathcedTime = player.currentTime();
        const duration = player.duration();
        if (watchedTime.current != currentWathcedTime) {
            watchedTime.current = currentWathcedTime;
            await API.post('/video/Video/setView', {
                postId: post.id,
                time: currentWathcedTime,
                isComplete: currentWathcedTime >= duration * 0.85
            });
        }
    }

    if (isLoading) {
        return <></>;
    }

    return (
        <div className='page-layout'>
            <SideBar />
            <div className="video-content-container">
                <div className="video-container">
                    <div className="main-content">

                        {videoWindow()}
                        {videoMetadata(post, userView, setReaction, navigate)}
                        {channelInfo(blog, handleSubscribe, userView, navigate)}

                        <div className="video-description">
                            <span>Описание: </span>
                            {post?.description}
                        </div>

                        <div className="comments-section">
                            <h3>{commentCount} комментария</h3>
                            {JwtTokenService.isAuth() && (
                                <div className="add-comment">
                                    <img src={"https://picsum.photos/40/40"}
                                        className="comment-avatar" alt="Ваш аватар" />
                                    <div className="comment-input-container">
                                        <input
                                            type="text"
                                            value={newCommentText}
                                            onChange={(e) => setNewCommentText(e.target.value)}
                                            placeholder="Добавьте комментарий..."
                                            className="comment-input"
                                        />
                                        <button
                                            onClick={() => handleAddComment()}
                                            className="btn btnPrimary"
                                            disabled={!newCommentText.trim()}>
                                            Отправить
                                        </button>
                                    </div>
                                </div>
                            )}
                            <div className="comments-list">
                                <CommentsList comments={comments} postId={searchParams.postId} />
                            </div>

                        </div>
                    </div>

                    <aside className="recommendation-sidebar">
                        {recommendations?.map(video => {
                            return <SmallVideoCard videoCardModel={video} navigate={navigate} key={video.postId} />
                        })}

                    </aside>
                </div>
            </div>
        </div >
    );


    function videoWindow() {
        return <div className="video-player">
            <VideoPlayer key={post.id} className="myVideo"
                thumbnail={post.previewUrl}
                path={{
                    url: getUrl(blog.id, post.id, post.videoData.objectName),
                    label: '',
                    postId: post.id,
                    blogId: blog.id,
                    autoplay: false,
                    objectName: post.videoData.objectName
                }}
                currentTime={time}
                onTimeupdate={setView}
                onEnded={setViewEnd}
                onPause={onPaused} />
        </div>;
    }
}

async function createConference(postId, navigate) {
    const response = await API.post(`video/api/ConferenceRoom/createConferenceToPost?postId=${postId}`, null);

    if (response.status === 200) {
        navigate(`/conference/${response.data.id}`)
    }

}

function videoMetadata(post, userView, setReaction, navigate) {
    return <div className="video-metadata">
        <h1 className="video-title">{post.title}</h1>

        <div className="video-stats">
            <div className="views-date">
                <span>{post.viewCount} Просмотров </span> •
                <span> Опубликовано {getLocalDateTime(post.createdAt)}</span>
            </div>
            <div className="video-actions">
                <button className={`action-button ${userView?.isLike === true ? 'action-button-active' : ''}`} onClick={() => { setReaction(true); }}>
                    <span>👍</span> {post.likeCount}
                </button>
                <button className={`action-button ${userView?.isLike === false ? 'action-button-active' : ''}`} onClick={() => { setReaction(false); }}>
                    <span>👎</span> {post.dislikeCount}
                </button>
                <button className="action-button">
                    <span>📁</span> Поделиться
                </button>

                <button className="action-button" onClick={() => { createConference(post.id, navigate) }}>
                    <span>📁</span> Совместный просмотр
                </button>
            </div>
        </div>
    </div>;
}


function channelInfo(blog, handleSubscribe, userView, navigate) {
    return <div className="channel-info">
        <div className="channel-left" onClick={() => {
            navigate(`/channel/${blog.id}`)
        }}>
            <img src={blog.photoUrl === null ? logo : blog.photoUrl} className="channel-avatar" alt="Аватар канала" />
            <div>
                <div className="channel-name">{blog.name}</div>
                <div className="subscribers-count">{blog.subscribersCount} подписчиков</div>
            </div>
        </div>
        <button className="subscribe-button" onClick={() => handleSubscribe()}>{userView?.hasSubscription ? 'Вы подписаны' : 'Подписаться'}</button>
    </div>;
}




export default VideoPage;