import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { PageShell } from '@/widgets/page-shell';
import { BigVideoCard } from '@/entities/post';
import { getRecommendation } from '@/shared/api/generated/recommendation/recommendation';
import { getSearch } from '@/shared/api/generated/search/search';
import defaultProfilePic from '@/shared/assets/defaultProfilePic.png';
import { Link } from 'react-router-dom';
import './MainPage.css';

const PAGE_SIZE = 10;
const RECOMMENDATION_BATCH_SIZE = Math.ceil(PAGE_SIZE / 2);
const SKELETON_COUNT = 6;
const FEED_FILTERS = [
    { value: 'all', label: 'Все' },
    { value: 'video', label: 'Видео' },
    { value: 'text', label: 'Статьи' },
];

const normalizeFeedItems = (items, kind) => (items ?? []).map((item) => ({ ...item, kind }));

const interleaveFeedItems = (videoItems, textItems) => {
    const result = [];
    const maxLength = Math.max(videoItems.length, textItems.length);

    for (let index = 0; index < maxLength; index += 1) {
        if (videoItems[index]) result.push(videoItems[index]);
        if (textItems[index]) result.push(textItems[index]);
    }

    return result;
};

const getPlainText = (html = '') => {
    const template = document.createElement('template');
    template.innerHTML = html;
    return template.content.textContent?.replace(/\s+/g, ' ').trim() ?? '';
};

const MainPage = function () {
    const [posts, setPosts] = useState([]);
    const [page, setPage] = useState(1);
    const [hasMore, setHasMore] = useState(true);
    const [isLoading, setIsLoading] = useState(true);
    const [loadError, setLoadError] = useState(false);
    const [retryKey, setRetryKey] = useState(0);
    const [searchQuery, setSearchQuery] = useState('');
    const [activeSearchQuery, setActiveSearchQuery] = useState('');
    const [feedFilter, setFeedFilter] = useState('all');
    const loadMoreRef = useRef(null);
    const requestIdRef = useRef(0);
    const recommendationCursorsRef = useRef({ video: undefined, text: undefined });
    const activeRecommendationFilter = activeSearchQuery ? 'all' : feedFilter;

    const fetchPosts = useCallback(async () => {
        const requestId = ++requestIdRef.current;

        try {
            setIsLoading(true);
            setLoadError(false);

            let responsePosts = [];
            let canLoadMore = false;

            if (activeSearchQuery) {
                const response = await getSearch().getApiSearchSearchByTitle({
                    title: activeSearchQuery,
                    page,
                    limit: PAGE_SIZE,
                });

                if (requestId !== requestIdRef.current) return;
                const searchResults = response.data ?? [];
                responsePosts = normalizeFeedItems(searchResults, 'video');
                canLoadMore = searchResults.length >= PAGE_SIZE;
            } else {
                const cursors = recommendationCursorsRef.current;
                const activeKinds = activeRecommendationFilter === 'all'
                    ? ['video', 'text']
                    : [activeRecommendationFilter];
                const requestedKinds = page === 1
                    ? activeKinds
                    : activeKinds.filter((kind) => cursors[kind]);
                const batchSize = requestedKinds.length === 1
                    ? PAGE_SIZE
                    : RECOMMENDATION_BATCH_SIZE;

                const responses = await Promise.all(requestedKinds.map(async (kind) => {
                    const response = await getRecommendation().getApiV1Feed({
                        limit: batchSize,
                        cursor: page === 1 ? undefined : cursors[kind],
                        postType: kind === 'text' ? 'Text' : 'Video',
                    });
                    return { kind, response };
                }));

                if (requestId !== requestIdRef.current) return;

                const batches = { video: [], text: [] };
                const nextCursors = { ...cursors };

                responses.forEach(({ kind, response }) => {
                    if (response.status !== 200) return;
                    batches[kind] = normalizeFeedItems(response.data?.items, kind);
                    nextCursors[kind] = response.data?.nextCursor ?? null;
                });

                recommendationCursorsRef.current = nextCursors;
                responsePosts = interleaveFeedItems(batches.video, batches.text);
                canLoadMore = activeKinds.some((kind) => Boolean(nextCursors[kind]));
            }

            setPosts((currentPosts) => (
                page === 1 ? responsePosts : [...currentPosts, ...responsePosts]
            ));
            setHasMore(canLoadMore);
        } catch (error) {
            if (requestId !== requestIdRef.current) return;
            console.error('Ошибка при загрузке публикаций:', error);
            setLoadError(true);
        } finally {
            if (requestId === requestIdRef.current) setIsLoading(false);
        }
    }, [page, activeSearchQuery, activeRecommendationFilter, retryKey]);

    useEffect(() => {
        fetchPosts();
    }, [fetchPosts]);

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
        recommendationCursorsRef.current = { video: undefined, text: undefined };
        setPosts([]);
        setHasMore(true);
        setIsLoading(true);
        setLoadError(false);
        setPage(1);
        setActiveSearchQuery(query);
        if (query) setFeedFilter('all');
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

    const handleFeedFilterChange = (nextFilter) => {
        if (nextFilter === feedFilter) return;

        if (activeSearchQuery) {
            setFeedFilter(nextFilter);
            return;
        }

        requestIdRef.current += 1;
        recommendationCursorsRef.current = { video: undefined, text: undefined };
        setPosts([]);
        setHasMore(true);
        setIsLoading(true);
        setLoadError(false);
        setPage(1);
        setFeedFilter(nextFilter);
    };

    const visiblePosts = useMemo(() => (
        feedFilter === 'all' ? posts : posts.filter((post) => post.kind === feedFilter)
    ), [feedFilter, posts]);

    const isInitialLoading = isLoading && posts.length === 0;
    const pageTitle = activeSearchQuery ? 'Результаты поиска' : 'Рекомендации';
    const pageDescription = activeSearchQuery
        ? `По запросу «${activeSearchQuery}»`
        : 'Видео и статьи, подобранные специально для вас';

    return (
        <PageShell className="mainpage-container" contentClassName="mainpage-content">
            <div className="mainpage-content-inner">
                <section className="mainpage-toolbar" aria-labelledby="mainpage-title">
                    <div className="mainpage-toolbar-row">
                        <div className="mainpage-heading">
                            <h1 id="mainpage-title" className="mainpage-title">{pageTitle}</h1>
                            <p className="mainpage-description">{pageDescription}</p>
                        </div>

                        <form onSubmit={handleSearchSubmit} className="search-container" role="search" aria-label="Поиск публикаций">
                            <div className="search-field">
                                <label className="visually-hidden" htmlFor="main-post-search">Поиск по названию публикации</label>
                                <input
                                    id="main-post-search"
                                    type="search"
                                    placeholder="Поиск публикаций..."
                                    value={searchQuery}
                                    onChange={(event) => setSearchQuery(event.target.value)}
                                    className="search-input"
                                />
                                {searchQuery && (
                                    <button type="button" onClick={handleClearSearch} className="clear-button" aria-label="Очистить поиск">×</button>
                                )}
                            </div>
                            <button type="submit" className="search-button">Поиск</button>
                        </form>
                    </div>

                    <div className="feed-filters" aria-label="Тип публикаций">
                        {FEED_FILTERS.map((filter) => (
                            <button
                                key={filter.value}
                                type="button"
                                className="feed-filter-button"
                                aria-pressed={feedFilter === filter.value}
                                onClick={() => handleFeedFilterChange(filter.value)}
                            >
                                {filter.label}
                            </button>
                        ))}
                    </div>
                </section>

                <section className="mainpage-feed" aria-label={pageTitle}>
                    <div className="mainpage-post-grid">
                        {isInitialLoading
                            ? Array.from({ length: SKELETON_COUNT }, (_, index) => (
                                <PostCardSkeleton key={index} text={index % 3 === 1} />
                            ))
                            : visiblePosts.map((post, index) => (
                                post.kind === 'text' ? (
                                    <TextFeedCard post={post} key={`text-${post.postId}-${index}`} />
                                ) : (
                                    <div className="mainpage-video-item" key={`video-${post.postId}-${index}`}>
                                        <BigVideoCard videoCardModel={post} />
                                    </div>
                                )
                            ))}
                    </div>

                    {isLoading && posts.length > 0 && (
                        <div className="loading-more" role="status"><span className="loading-spinner" aria-hidden="true" />Загружаем ещё публикации</div>
                    )}

                    {loadError && (
                        <div className="feed-state feed-state-error" role="alert">
                            <h2>Не удалось загрузить публикации</h2>
                            <p>Проверьте соединение и попробуйте ещё раз.</p>
                            <button type="button" onClick={handleRetry}>Повторить</button>
                        </div>
                    )}

                    {!isLoading && !loadError && visiblePosts.length === 0 && (
                        <div className="feed-state">
                            <h2>{activeSearchQuery ? 'Ничего не найдено' : 'Публикаций пока нет'}</h2>
                            <p>{activeSearchQuery ? 'Попробуйте изменить запрос или проверить написание.' : 'Новые рекомендации появятся здесь позже.'}</p>
                            {activeSearchQuery && <button type="button" onClick={handleClearSearch}>Вернуться к рекомендациям</button>}
                        </div>
                    )}

                    {!isLoading && !loadError && !hasMore && posts.length > 0 && (
                        <div className="end-message">{activeSearchQuery ? 'Больше результатов нет' : 'Вы посмотрели все рекомендации'}</div>
                    )}

                    {visiblePosts.length > 0 && hasMore && !isLoading && !loadError && (
                        <div ref={loadMoreRef} className="load-more-sentinel" aria-hidden="true" />
                    )}
                </section>
            </div>
        </PageShell>
    );
};

const TextFeedCard = ({ post }) => {
    const [isExpanded, setIsExpanded] = useState(false);
    const text = useMemo(() => getPlainText(post.description), [post.description]);
    const readingTime = Math.max(1, Math.ceil(text.split(/\s+/).filter(Boolean).length / 180));
    const creator = post.creator;
    const creatorName = creator?.name || 'Неизвестный автор';
    const creatorAvatar = creator?.avatarUrl || defaultProfilePic;
    const creatorContent = (
        <>
            <img className="text-feed-avatar" src={creatorAvatar} alt="" />
            <span className="text-feed-source">{creatorName}</span>
        </>
    );

    return (
        <article className={`text-feed-card ${isExpanded ? 'text-feed-card-expanded' : ''}`}>
            <header className="text-feed-card-header">
                {creator?.blogId ? (
                    <Link className="text-feed-creator" to={`/channel/${creator.blogId}`}>
                        {creatorContent}
                    </Link>
                ) : (
                    <span className="text-feed-creator">{creatorContent}</span>
                )}
                <span className="text-feed-kind">Статья</span>
            </header>
            <h2 className="text-feed-title">{post.title || 'Без названия'}</h2>
            <p className="text-feed-excerpt">{text || 'Автор пока не добавил текст публикации.'}</p>
            <footer className="text-feed-footer">
                <span>{readingTime} мин чтения</span>
                {post.reason && <span className="text-feed-reason">{post.reason}</span>}
                {text.length > 220 && (
                    <button type="button" onClick={() => setIsExpanded((expanded) => !expanded)}>
                        {isExpanded ? 'Свернуть' : 'Читать целиком'}
                    </button>
                )}
            </footer>
        </article>
    );
};

const PostCardSkeleton = ({ text }) => (
    <div className={`post-card-skeleton ${text ? 'post-card-skeleton-text' : ''}`} aria-hidden="true">
        {text ? (
            <>
                <div className="skeleton-channel"><div className="skeleton-avatar" /><div className="skeleton-line skeleton-name" /></div>
                <div className="skeleton-line skeleton-article-title" />
                <div className="skeleton-line skeleton-copy" />
                <div className="skeleton-line skeleton-copy skeleton-copy-short" />
            </>
        ) : (
            <>
                <div className="skeleton-thumbnail" />
                <div className="skeleton-body">
                    <div className="skeleton-line skeleton-title" />
                    <div className="skeleton-channel"><div className="skeleton-avatar" /><div className="skeleton-line skeleton-name" /></div>
                    <div className="skeleton-line skeleton-meta" />
                </div>
            </>
        )}
    </div>
);

export default MainPage;
