import { useLocation, useParams, useSearchParams } from 'react-router-dom';
import VideoPlayer from '../../components/VideoPlayer/VideoPlayer';
import './VideoPage.css';
import React, { useEffect, useState } from 'react';
import API, { BaseApUrl } from '../../scripts/apiMethod';

export const VideoPage = function (props) {
    const searchParams = useParams();

    const [video, setVideoData] = useState([]);

    function getUrl(postId, objectName) {
        if (postId !== null && objectName !== null)
            return `${BaseApUrl}/video/Video/video/v2/${postId}/chunks/${objectName}`;
    }

    useEffect(() => {
        API.get(`/video/Video/video/${searchParams.postId}`)
            .then(response => {
                if (response.status === 200) {
                    setVideoData([response.data.post]);
                }
            })

    }, [])

    return (
        <div className="container">
            <div className="main-content">
                <div className="video-player">
                    {video.map(post => {
                        return <VideoPlayer className="myVideo"
                            thumbnail={post.previewUrl}
                            path={
                                {
                                    url: getUrl(post.id, post.videoData.objectName),
                                    label: 'd',
                                    postId: post.id,
                                    res: 0,
                                    objectName: post.videoData.objectName
                                }
                            }
                        />
                    })}
                </div>

                <div className="video-metadata">
                    <h1 className="video-title">Удивительные факты о технологиях | Интересный документальный фильм</h1>

                    <div className="video-stats">
                        <div className="views-date">
                            <span>1 234 567 просмотров</span> •
                            <span>5 дней назад</span>
                        </div>
                        <div className="video-actions">
                            <button className="action-button">
                                <span>👍</span> 123K
                            </button>
                            <button className="action-button">
                                <span>👎</span> 456
                            </button>
                            <button className="action-button">
                                <span>📁</span> Поделиться
                            </button>
                        </div>
                    </div>
                </div>

                <div className="channel-info">
                    <div className="channel-left">
                        <img src="https://picsum.photos/48/48" className="channel-avatar" alt="Аватар канала" />
                        <div>
                            <div className="channel-name">TechWorld</div>
                            <div className="subscribers-count">1,23 млн подписчиков</div>
                        </div>
                    </div>
                    <button className="subscribe-button">Подписаться</button>
                </div>

                <div className="video-description">
                    В этом видео мы рассмотрим удивительные технологические достижения последних лет.
                    Вы узнаете о новейших разработках в области искусственного интеллекта, квантовых
                    вычислений и робототехники. Не забудьте подписаться на канал!
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
                <div className="recommended-video">
                    <img src="https://picsum.photos/168/94?1" className="recommended-thumbnail" alt="Превью" />
                    <div className="recommended-info">
                        <div className="recommended-title">Будущее искусственного интеллекта: что нас ждет?</div>
                        <div className="recommended-channel">TechVision</div>
                        <div className="recommended-stats">256K просмотров • 2 дня назад</div>
                    </div>
                </div>

                <div className="recommended-video">
                    <img src="https://picsum.photos/168/94?2" className="recommended-thumbnail" alt="Превью" />
                    <div className="recommended-info">
                        <div className="recommended-title">10 технологий, которые изменят мир</div>
                        <div className="recommended-channel">Future Tech</div>
                        <div className="recommended-stats">1,2M просмотров • 1 неделя назад</div>
                    </div>
                </div>
            </aside>
        </div>
    );
}
