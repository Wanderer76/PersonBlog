import React, { useCallback, useEffect, useRef, useState } from 'react';
import { PageShell } from '@/widgets/page-shell';
import { BigVideoCard } from '@/entities/post';
import { getRecommendation } from '@/shared/api/generated/recommendation/recommendation';
import { getSearch } from '@/shared/api/generated/search/search';
import './MainPage.css';

const PAGE_SIZE = 10;
const SKELETON_COUNT = 6;

const MainPage = function () {
    const [videos, setVideos] = useState([]);
    const [page, setPage] = useState(1);
    const [hasMore, setHasMore] = useState(true);
    const [isLoading, setIsLoading] = useState(true);
    const [loadError, setLoadError] = useState(false);
    const [retryKey, setRetryKey] = useState(0);
    const [searchQuery, setSearchQuery] = useState('');
    const [activeSearchQuery, setActiveSearchQuery] = useState('');
    const loadMoreRef = useRef(null);
    const requestIdRef = useRef(0);
    const recommendationCursorRef = useRef(null);

    const fetchVideos = useCallback(async () => {
        const requestId = ++requestIdRef.current;

        try {
            setIsLoading(true);
            setLoadError(false);

            const response = activeSearchQuery
                ? await getSearch().getApiSearchSearchByTitle({
                    title: activeSearchQuery,
                    page,
                    limit: PAGE_SIZE,
                })
                : await getRecommendation().getApiV1Feed({
                    limit: PAGE_SIZE,
                    cursor: page === 1 ? undefined : recommendationCursorRef.current,
                });

            if (requestId !== requestIdRef.current) return;

            if (response.status === 200) {
                const responseVideos = activeSearchQuery
                    ? (response.data ?? [])
                    : (response.data?.items ?? []);

                setVideos((currentVideos) => (
                    page === 1 ? responseVideos : [...currentVideos, ...responseVideos]
                ));
                if (activeSearchQuery) {
                    setHasMore(responseVideos.length >= PAGE_SIZE);
                } else {
                    recommendationCursorRef.current = response.data?.nextCursor ?? null;
                    setHasMore(Boolean(recommendationCursorRef.current));
                }
            }
        } catch (error) {
            if (requestId !== requestIdRef.current) return;
            console.error('Ошибка при загрузке видео:', error);
            setLoadError(true);
        } finally {
            if (requestId === requestIdRef.current) setIsLoading(false);
        }
    }, [page, activeSearchQuery, retryKey]);

    useEffect(() => {
        fetchVideos();
    }, [fetchVideos]);

    useEffect(() => {
        const sentinel = loadMoreRef.current;
        if (!sentinel || isLoading || loadError || !hasMore) return undefined;

        const observer = new IntersectionObserver(([entry]) => {
            if (entry.isIntersecting) {
                observer.disconnect();
                setPage((currentPage) => currentPage + 1);
            }
        }, { rootMargin: '300px 0px' });

        observer.observe(sentinel);
        return () => observer.disconnect();
    }, [hasMore, isLoading, loadError]);

    const startSearch = (query) => {
        if (query === activeSearchQuery && page === 1) return;
        requestIdRef.current += 1;
        recommendationCursorRef.current = null;
        setVideos([]);
        setHasMore(true);
        setIsLoading(true);
        setLoadError(false);
        setPage(1);
        setActiveSearchQuery(query);
    };

    const handleSearchSubmit = (event) => {
        event.preventDefault();
        startSearch(searchQuery.trim());
    };

    const handleClearSearch = () => {
        setSearchQuery('');
        startSearch('');
    };

    const handleRetry = () => {
        setIsLoading(true);
        setRetryKey((currentKey) => currentKey + 1);
    };

    const isInitialLoading = isLoading && videos.length === 0;
    const pageTitle = activeSearchQuery ? 'Результаты поиска' : 'Рекомендации';
    const pageDescription = activeSearchQuery
        ? `По запросу «${activeSearchQuery}»`
        : 'Видео, подобранные специально для вас';

    return (
        <PageShell className="mainpage-container" contentClassName="mainpage-content">
                <div className="mainpage-content-inner">
                    <section className="mainpage-toolbar" aria-labelledby="mainpage-title">
                        <div className="mainpage-heading">
                            <h1 id="mainpage-title" className="mainpage-title">{pageTitle}</h1>
                            <p className="mainpage-description">{pageDescription}</p>
                        </div>

                        <form
                            onSubmit={handleSearchSubmit}
                            className="search-container"
                            role="search"
                            aria-label="Поиск видео"
                        >
                            <div className="search-field">
                                <label className="visually-hidden" htmlFor="main-video-search">
                                    Поиск по названию видео
                                </label>
                                <input
                                    id="main-video-search"
                                    type="search"
                                    placeholder="Поиск по названию видео..."
                                    value={searchQuery}
                                    onChange={(event) => setSearchQuery(event.target.value)}
                                    className="search-input"
                                />
                                {searchQuery && (
                                    <button
                                        type="button"
                                        onClick={handleClearSearch}
                                        className="clear-button"
                                        aria-label="Очистить поиск"
                                    >
                                        ×
                                    </button>
                                )}
                            </div>
                            <button type="submit" className="search-button">Поиск</button>
                        </form>
                    </section>

                    <section className="mainpage-feed" aria-label={pageTitle}>
                        <div className="mainpage-video-grid">
                            {isInitialLoading
                                ? Array.from({ length: SKELETON_COUNT }, (_, index) => (
                                    <VideoCardSkeleton key={index} />
                                ))
                                : videos.map((video, index) => (
                                    <BigVideoCard
                                        videoCardModel={video}
                                        key={`${video.postId}-${index}`}
                                    />
                                ))}
                        </div>

                        {isLoading && videos.length > 0 && (
                            <div className="loading-more" role="status">
                                <span className="loading-spinner" aria-hidden="true" />
                                Загружаем ещё видео
                            </div>
                        )}

                        {loadError && (
                            <div className="feed-state feed-state-error" role="alert">
                                <h2>Не удалось загрузить видео</h2>
                                <p>Проверьте соединение и попробуйте ещё раз.</p>
                                <button type="button" onClick={handleRetry}>Повторить</button>
                            </div>
                        )}

                        {!isLoading && !loadError && videos.length === 0 && (
                            <div className="feed-state">
                                <h2>{activeSearchQuery ? 'Ничего не найдено' : 'Видео пока нет'}</h2>
                                <p>
                                    {activeSearchQuery
                                        ? 'Попробуйте изменить запрос или проверить написание.'
                                        : 'Новые рекомендации появятся здесь позже.'}
                                </p>
                                {activeSearchQuery && (
                                    <button type="button" onClick={handleClearSearch}>
                                        Вернуться к рекомендациям
                                    </button>
                                )}
                            </div>
                        )}

                        {!isLoading && !loadError && !hasMore && videos.length > 0 && (
                            <div className="end-message">
                                {activeSearchQuery ? 'Больше результатов нет' : 'Вы посмотрели все рекомендации'}
                            </div>
                        )}

                        {videos.length > 0 && hasMore && !isLoading && !loadError && (
                            <div ref={loadMoreRef} className="load-more-sentinel" aria-hidden="true" />
                        )}
                    </section>
                </div>
        </PageShell>
    );
};

const VideoCardSkeleton = () => (
    <div className="video-card-skeleton" aria-hidden="true">
        <div className="skeleton-thumbnail" />
        <div className="skeleton-body">
            <div className="skeleton-line skeleton-title" />
            <div className="skeleton-channel">
                <div className="skeleton-avatar" />
                <div className="skeleton-line skeleton-name" />
            </div>
            <div className="skeleton-line skeleton-meta" />
        </div>
    </div>
);

export default MainPage;
