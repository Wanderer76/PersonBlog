import { useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import VideoPlayer from '../../components/VideoPlayer/VideoPlayer';
import './PostPage.css';
import React, { useEffect, useState } from 'react';
import API, { BaseApUrl } from '../../scripts/apiMethod';
import { getLocalDateTime } from '../../scripts/LocalDate';
import logo from '../../defaultProfilePic.png';
import { JwtTokenService } from '../../scripts/TokenStrorage';

export const VideoPage = function (props) {
    const searchParams = useParams();
    const [isLoading, setIsLoading] = useState(true);
    let viewRecorded = false; let inProgress = false;
    const [recommendations, setRecommendations] = useState([]);
    const limit = 40;
    const navigate = useNavigate();
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
        isSubscribe: false
    })

    const [blog, setBlog] = useState({
    });

    function getUrl(postId, objectName) {
        if (postId !== null && objectName !== null)
            return `${BaseApUrl}/video/Video/video/v2/${postId}/chunks/${objectName}`;
    }

    useEffect(() => {
        API.get(`/video/Video/video/${searchParams.postId}`, {
            headers: {
                'Authorization': JwtTokenService.isAuth() ? JwtTokenService.getFormatedTokenForHeader() : null
            }
        })
            .then(response => {
                if (response.status === 200) {
                    setPostData(response.data.post);
                    setBlog(response.data.blog);
                    setUserView(response.data.userPostInfo)
                    setIsLoading(false)
                    viewRecorded = response.data.userPostInfo.isViewed;
                }
            });

        API.get(`/video/recommendations?page=${1}&limit=${limit}&currentPostId=${searchParams.postId}`)
            .then(response => {
                if (response.status === 200) {
                    setRecommendations(response.data);
                }
            });


    }, [searchParams.videoId])

    async function setReaction(isLike) {
        await API.post(`profile/api/Post/setReaction/${post.id}?isLike=${isLike}`, null, {
            headers: {
                'Authorization': JwtTokenService.isAuth() ? JwtTokenService.getFormatedTokenForHeader() : null
            }
        }).catch(e => { })
            .finally(() => {

            })

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
            'isLike': prev.isLike === isLike ? null : isLike
        }))
    }


    async function setView(player) {
        const watchedTime = player.currentTime();
        const duration = player.duration();

        if (!viewRecorded && userView.isViewed === false && !inProgress &&
            (watchedTime >= 30 || watchedTime >= duration * 0.5 || watchedTime === duration * 0.85)) {
            inProgress = true;
            await API.post(`video/Video/setReaction/${post.id} ${userView.isLike == null ? '' : `?isLike=${userView.isLike}`}`, null, {
                headers: {
                    'Authorization': JwtTokenService.isAuth() ? JwtTokenService.getFormatedTokenForHeader() : null
                }
            }).catch(e => { })
                .finally(() => {
                    viewRecorded = true;
                    inProgress = false;
                })
        }
    }

    async function handleSubscribe() {
        const current = userView.isSubscribe;
        var isError = false;
        await API.post(`profile/api/Subscription/${userView.isSubscribe ? 'unsubscribe' : 'subscribe'}/${blog.id}`).catch(e => {
            isError = true
            alert(e.response.data)
        }).finally(() => {
            if (!isError) {
                setUserView((prev => ({
                    ...prev,
                    isSubscribe: !current
                })))
                setBlog((prev => ({
                    ...prev,
                    subscribersCount: !current ? prev.subscribersCount + 1 : prev.subscribersCount - 1
                })))
            }
        })
    }

    if (isLoading) {
        return <></>;
    }

    return (
        <div className="video-container">
            <div className="main-content">

                {videoWindow()}
                {videoMetadata(post, userView, setReaction)}
                {channelInfo(blog, handleSubscribe, userView)}

                <div className="video-description">
                    <span>Описание: </span>
                    {post.description}
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

            <aside className="sidebar">
                {recommendations.map(video => {
                    return <VideoCard videoCardModel={video} navigate={navigate} key={video.postId} />
                })}

            </aside>
        </div>
    );


    function videoWindow() {
        return <div className="video-player">
            <VideoPlayer className="myVideo"
                thumbnail={post.previewUrl}
                path={{
                    url: getUrl(post.id, post.videoData.objectName),
                    label: '',
                    postId: post.id,
                    autoplay: false,
                    objectName: post.videoData.objectName
                }}
                onTimeupdate={setView} />
        </div>;
    }
}

const VideoCard = function ({ videoCardModel, navigate }) {

    return <div className="recommended-video" onClick={(e) => {
        e.preventDefault();
        navigate(`/video/${videoCardModel.postId}/${videoCardModel.videoId}`);
    }}>
        <img src={videoCardModel.previewUrl} className="recommended-thumbnail" alt="Превью" />
        <div className="recommended-info">
            <div className="recommended-title">{videoCardModel.title}</div>
            <div className="recommended-channel">{videoCardModel.blogName}</div>
            <div className="recommended-stats"> {videoCardModel.viewCount} просмотров • 2 дня назад</div>
        </div>
    </div>
}

function videoMetadata(post, userView, setReaction) {
    return <div className="video-metadata">
        <h1 className="video-title">{post.title}</h1>

        <div className="video-stats">
            <div className="views-date">
                <span>{post.viewCount} Просмотров </span> •
                <span> Опубликовано {getLocalDateTime(post.createdAt)}</span>
            </div>
            <div className="video-actions">
                <button className={`action-button ${userView.isLike === true ? 'action-button-active' : ''}`} onClick={() => { setReaction(true); }}>
                    <span>👍</span> {post.likeCount}
                </button>
                <button className={`action-button ${userView.isLike === false ? 'action-button-active' : ''}`} onClick={() => { setReaction(false); }}>
                    <span>👎</span> {post.dislikeCount}
                </button>
                <button className="action-button">
                    <span>📁</span> Поделиться
                </button>
            </div>
        </div>
    </div>;
}


function channelInfo(blog, handleSubscribe, userView) {
    return <div className="channel-info">
        <div className="channel-left">
            <img src={blog.photoUrl === null ? logo : blog.photoUrl} className="channel-avatar" alt="Аватар канала" />
            <div>
                <div className="channel-name">{blog.name}</div>
                <div className="subscribers-count">{blog.subscribersCount} подписчиков</div>
            </div>
        </div>
        <button className="subscribe-button" onClick={() => handleSubscribe()}>{userView.isSubscribe ? 'Вы подписаны' : 'Подписаться'}</button>
    </div>;
}

