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

                <SideBar />


                <div className="mainpage-content">
                <input/>
                    <div className="mainpage-video-grid">
                        {videos.map((x, index) => {
                            if (videos.length === index + 1) {
                                return (
                                    <BigVideoCard
                                        ref={lastVideoRef}
                                        videoCardModel={x}
                                        key={x.postId}
                                    />
                                );
                            }
                            return <BigVideoCard videoCardModel={x} key={x.postId} />;
                        })}
                        {isLoading && <div className="loading">Загрузка...</div>}
                        {!hasMore && <div className="end-message">Больше видео нет</div>}
                    </div>

                </div>
            </div>
        </>
    );
};

export default MainPage;