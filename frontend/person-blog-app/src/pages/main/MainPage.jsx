import React, { useEffect, useState, useRef, useCallback } from "react";
import './MainPage.css';
import API from "../../scripts/apiMethod";
import logo from '../../defaultProfilePic.png';
import { NavLink, useNavigate } from "react-router-dom";

const MainPage = function () {
    const [videos, setVideos] = useState([]);
    const [page, setPage] = useState(1);
    const [hasMore, setHasMore] = useState(true);
    const [isLoading, setIsLoading] = useState(false);
    const limit = 10;
    const observer = useRef();

    const lastVideoRef = useCallback(node => {
        if (isLoading) return;
        if (observer.current) observer.current.disconnect();

        observer.current = new IntersectionObserver(entries => {
            if (entries[0].isIntersecting && hasMore) {
                setPage(prev => prev + 1);
            }
        });

        if (node) observer.current.observe(node);
    }, [hasMore, isLoading]);

    useEffect(() => {
        const fetchVideos = async () => {
            try {
                setIsLoading(true);
                const response = await API.get(`/video/recommendations?page=${page}&limit=${limit}`);

                if (response.status === 200) {
                    setVideos(prev => [...prev, ...response.data]);
                    setHasMore(response.data.length >= limit);
                }
            } catch (error) {
                console.error('Ошибка при загрузке видео:', error);
            } finally {
                setIsLoading(false);
            }
        };

        fetchVideos();
    }, [page]);

    return (
        <>
            <div className="mainpage-container">

                <div className="manipage-sidebar">
                    <nav className="mainpage-sidebar-nav">
                        <NavLink to="/history" className="mainpage-sidebar-item">
                            <HistoryIcon />
                            <span>История просмотров</span>
                        </NavLink>

                        <NavLink to="/subscriptions" className="mainpage-sidebar-item">
                            <SubscriptionsIcon />
                            <span>Подписки</span>
                        </NavLink>

                        <div className="mainpage-sidebar-section-divider"></div>

                        <NavLink to="/playlists" className="mainpage-sidebar-item">
                            <PlaylistIcon />
                            <span>Сохраненные плейлисты</span>
                        </NavLink>

                        <NavLink to="/liked" className="mainpage-sidebar-item">
                            <LikeIcon />
                            <span>Понравившиеся</span>
                        </NavLink>

                        <NavLink to="/watch-later" className="mainpage-sidebar-item">
                            <ClockIcon />
                            <span>Смотреть позже</span>
                        </NavLink>
                    </nav>
                </div>

                <div className="mainpage-content">
                    <div className="mainpage-video-grid">
                        {videos.map((x, index) => {
                            if (videos.length === index + 1) {
                                return (
                                    <VideoCard
                                        ref={lastVideoRef}
                                        videoCardModel={x}
                                        key={x.postId}
                                    />
                                );
                            }
                            return <VideoCard videoCardModel={x} key={x.postId} />;
                        })}
                        {isLoading && <div className="loading">Загрузка...</div>}
                        {!hasMore && <div className="end-message">Больше видео нет</div>}
                    </div>

                </div>
            </div>
        </>
    );
};

const VideoCard = React.forwardRef(function ({ videoCardModel }, ref) {
    const navigate = useNavigate();
    return (
        <div ref={ref} className="mainpage-video-card">
            <div
                className="mainpage-thumbnail-container"
                onClick={() => navigate(`/video/${videoCardModel.postId}`)}
            >
                <img
                    src={videoCardModel.previewUrl}
                    className="mainpage-thumbnail"
                    alt="Превью видео"
                />
                <div className="mainpage-play-icon"></div>
            </div>
            <div className="mainpage-video-info">
                <h3 className="mainpage-video-title">{videoCardModel.title}</h3>
                <div className="mainpage-channel-info">
                    <img
                        src={videoCardModel.blogLogo || logo}
                        className="mainpage-channel-icon"
                        alt="Логотип канала"
                    />
                    <span className="mainpage-channel-name">{videoCardModel.blogName}</span>
                </div>
                <div className="mainpage-video-stats">
                    Просмотров: {videoCardModel.viewCount}
                </div>
            </div>
        </div>
    );
});

const HistoryIcon = () => (
    <svg viewBox="0 0 24 24" className="mainpage-sidebar-icon">
        <path d="M13 3c-4.97 0-9 4.03-9 9H1l3.89 3.89.07.14L9 12H6c0-3.87 3.13-7 7-7s7 3.13 7 7-3.13 7-7 7c-1.93 0-3.68-.79-4.94-2.06l-1.42 1.42C8.27 19.99 10.51 21 13 21c4.97 0 9-4.03 9-9s-4.03-9-9-9zm-1 5v5l4.28 2.54.72-1.21-3.5-2.08V8H12z" />
    </svg>
);

const SubscriptionsIcon = () => (
    <svg viewBox="0 0 24 24" className="mainpage-sidebar-icon">
        <path d="M10 18v-6l5 3-5 3zm7-15H7v1h10V3zm3 3H4v1h16V6zm2 3H2v12h20V9zM3 10h18v10H3V10z" />
    </svg>
);

const PlaylistIcon = () => (
    <svg viewBox="0 0 24 24" className="mainpage-sidebar-icon">
        <path d="M15 6H3v2h12V6zm0 4H3v2h12v-2zM3 16h8v-2H3v2zM17 6v8.18c-.31-.11-.65-.18-1-.18-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3V8h3V6h-5z" />
    </svg>
);

const LikeIcon = () => (
    <svg viewBox="0 0 24 24" className="mainpage-sidebar-icon">
        <path d="M1 21h4V9H1v12zm22-11c0-1.1-.9-2-2-2h-6.31l.95-4.57.03-.32c0-.41-.17-.79-.44-1.06L14.17 1 7.59 7.59C7.22 7.95 7 8.45 7 9v10c0 1.1.9 2 2 2h9c.83 0 1.54-.5 1.84-1.22l3.02-7.05c.09-.23.14-.47.14-.73v-2z" />
    </svg>
);

const ClockIcon = () => (
    <svg viewBox="0 0 24 24" className="mainpage-sidebar-icon">
        <path d="M12 2C6.5 2 2 6.5 2 12s4.5 10 10 10 10-4.5 10-10S17.5 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67V7z" />
    </svg>
);

export default MainPage;