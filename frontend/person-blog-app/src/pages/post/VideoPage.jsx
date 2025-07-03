import { useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import VideoPlayer from '../../components/VideoPlayer/VideoPlayer';
import './VideoPage.css';
import React, { useEffect, useRef, useState } from 'react';
import API, { BaseApUrl } from '../../scripts/apiMethod';
import { getLocalDateTime } from '../../scripts/LocalDate';
import logo from '../../defaultProfilePic.png';
import { JwtTokenService } from '../../scripts/TokenStrorage';
import SmallVideoCard from '../../components/VideoCards/SmallVideoCard';
import SideBar from '../../components/sidebar/SideBar';

const VideoPage = function (props) {
    const searchParams = useParams();
    const queryParams = new URLSearchParams(window.location.search)
    const [isLoading, setIsLoading] = useState(true);
    const watchedTime = useRef(0);
    const [recommendations, setRecommendations] = useState([]);
    const limit = 40;
    const navigate = useNavigate();
    const [time, setTime] = useState(queryParams.get('time'))
    const nextThresholdRef = useRef(30);

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

    function getUrl(postId, objectName) {
        if (postId !== null && objectName !== null)
            return `${BaseApUrl}/video/Video/video/v2/${postId}/chunks/${objectName}`;
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


    }, [searchParams.postId])

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

        if (!JwtTokenService.isAuth())
            return;

        const duration = player.duration();
        var interval = Math.round(duration / 10);

        if (duration <= 60) {
            interval = duration * 0.5;
        }

        const currentWathcedTime = player.currentTime();
        const currentPercent = (currentWathcedTime / duration) * 100;
        const sendViewData = async () => {
            try {
                await API.post('/video/Video/setView', {
                    postId: post.id,
                    time: currentWathcedTime,
                    isComplete: currentWathcedTime >= duration * 0.85,
                });
            } catch (e) {
                console.error("Ошибка при отправке данных просмотра:", e);
            }
        };
        console.log(currentPercent)
        console.log(nextThresholdRef.current)
        if (currentPercent >= nextThresholdRef.current) {
            await sendViewData()

            if (nextThresholdRef.current === 30) {
                nextThresholdRef.current = 40;
            } else {
                nextThresholdRef.current += 10;
            }
            if (nextThresholdRef.current > 100) {
                return
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
        watchedTime.current = currentWathcedTime;
        await API.post('/video/Video/setView', {
            postId: post.id,
            time: currentWathcedTime,
            isComplete: currentWathcedTime >= duration * 0.85
        });
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
                            <h3>432 комментария</h3>

                            <div className="comment">
                                <img src="https://picsum.photos/40/40" className="comment-avatar" alt="Аватар пользователя" />
                                <div className="comment-content">
                                    <div className="comment-author">Иван Петров</div>
                                    <div className="comment-text">Отличное видео! Очень познавательно и интересно подано.</div>
                                </div>
                            </div>

                            <div className="comment">
                                <img src="https://picsum.photos/40/40" className="comment-avatar" alt="Аватар пользователя" />
                                <div className="comment-content">
                                    <div className="comment-author">Иван Петров</div>
                                    <div className="comment-text">Отличное видео! Очень познавательно и интересно подано.</div>
                                </div>
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
        </div>
    );


    function videoWindow() {
        return <div className="video-player">
            <VideoPlayer key={post.id} className="myVideo"
                thumbnail={post.previewUrl}
                path={{
                    url: getUrl(post.id, post.videoData.objectName),
                    label: '',
                    postId: post.id,
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
    const response = await API.post(`video/api/ConferenceRoom/createConferenceToPost?postId=${postId}`, null, {
        headers: { Authorization: JwtTokenService.getFormatedTokenForHeader() }
    });

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