import React, { useState } from 'react';
import './ChannelPage.css';

const ChannelPage = () => {
    const [isSubscribed, setIsSubscribed] = useState(false);
    const [videos] = useState([
        {
            id: 1,
            title: "Как создать крутой видео-контент",
            views: "125 тыс. просмотров",
            date: "2 дня назад",
            duration: "12:45",
            thumbnail: "https://via.placeholder.com/320x180"
        },
        // Добавьте больше видео...
    ]);

    return (
        <div className="blogContainer">
            {/* Шапка канала */}
            <header className="channelHeader">
                <div className="channelInfo">
                    <div className="channelAvatar">
                        <img src="https://via.placeholder.com/80" alt="Аватар канала" />
                    </div>
                    <div className="channelMeta">
                        <h1>Creative Video Hub</h1>
                        <div className="channelStats">
                            <span>150 тыс. подписчиков</span> • <span>45 видео</span>
                        </div>
                        <p className="channelDescription">
                            Канал о создании профессионального видео-контента. 
                            Обзоры оборудования, уроки монтажа и советы по съёмке.
                        </p>
                    </div>
                </div>
                <button 
                    className={`btn ${isSubscribed ? 'btnSecondary' : 'btnPrimary'}`}
                    onClick={() => setIsSubscribed(!isSubscribed)}
                >
                    {isSubscribed ? '✓ Подписан' : 'Подписаться'}
                </button>
            </header>

            {/* Сетка видео */}
            <section className="videoGrid">
                {videos.map(video => (
                    <article className="videoCard" key={video.id}>
                        <div className="videoThumbnail">
                            <img src={video.thumbnail} alt="Превью видео" />
                            <span className="videoDuration">{video.duration}</span>
                        </div>
                        <div className="videoDetails">
                            <h3 className="videoTitle">{video.title}</h3>
                            <div className="videoMeta">
                                <span>Creative Video Hub</span>
                                <span>{video.views}</span>
                                <span>{video.date}</span>
                            </div>
                        </div>
                    </article>
                ))}
            </section>
        </div>
    );
};

export default ChannelPage;