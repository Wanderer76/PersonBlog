import React, { useEffect, useState, useRef, useCallback } from "react";
import './MainPage.css';
import API from "../../scripts/apiMethod";
import SideBar from "../../components/sidebar/SideBar";
import BigVideoCard from "../../components/VideoCards/BigVideoCard/BigVideoCard";

const MainPage = function () {
    const [videos, setVideos] = useState([]);
    const [page, setPage] = useState(1);
    const [hasMore, setHasMore] = useState(true);
    const [isLoading, setIsLoading] = useState(false);
    const [searchQuery, setSearchQuery] = useState("");
    const [activeSearchQuery, setActiveSearchQuery] = useState("");
    const limit = 10;
    const observer = useRef();

    const fetchVideos = useCallback(async () => {
        try {
            setIsLoading(true);
            const endpoint = activeSearchQuery
                ? `/video/api/Search/searchByTitle?title=${activeSearchQuery}&page=${page}&limit=${limit}`
                : `/video/recommendations?page=${page}&limit=${limit}`;

            const response = await API.get(endpoint);

            if (response.status === 200) {
                setVideos(prev =>
                    page === 1 ? response.data : [...prev, ...response.data]
                );
                setHasMore(response.data.length >= limit);
            }
        } catch (error) {
            console.error('Ошибка при загрузке видео:', error);
        } finally {
            setIsLoading(false);
        }
    }, [page, activeSearchQuery]);

    useEffect(() => {
        fetchVideos();
    }, [fetchVideos]);

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

    const handleSearchChange = (e) => {
        setSearchQuery(e.target.value);
    };

    const handleSearchSubmit = (e) => {
        e.preventDefault();
        setActiveSearchQuery(searchQuery)
        setPage(1);
    };

    const handleClearSearch = () => {
        setSearchQuery("");
        setActiveSearchQuery("")
        setPage(1);
    };

    return (
        <div className="mainpage-container">
            <SideBar />
            <div className="mainpage-content">
                <form onSubmit={handleSearchSubmit} className="search-container">
                    <input
                        type="text"
                        placeholder="Поиск по названию видео..."
                        value={searchQuery}
                        onChange={handleSearchChange}
                        className="search-input"
                    />
                    <button type="submit" className="search-button">
                        Поиск
                    </button>
                    {searchQuery && (
                        <button
                            type="button"
                            onClick={handleClearSearch}
                            className="clear-button"
                        >
                            ×
                        </button>
                    )}
                </form>

                <div className="mainpage-video-grid">
                    {videos.map((video, index) => {
                        if (videos.length === index + 1) {
                            return (
                                <BigVideoCard
                                    ref={lastVideoRef}
                                    videoCardModel={video}
                                    key={`${video.postId}-${index}`}
                                />
                            );
                        }
                        return <BigVideoCard videoCardModel={video} key={`${video.postId}-${index}`} />;
                    })}

                    {isLoading && <div className="loading">Загрузка...</div>}
                    {!hasMore && videos.length > 0 && <div className="end-message">
                        {searchQuery ? "Больше результатов нет" : "Больше видео нет"}
                    </div>}
                    {!isLoading && videos.length === 0 && (
                        <div className="end-message">
                            {searchQuery ? "Видео не найдены" : "Нет доступных видео"}
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default MainPage;