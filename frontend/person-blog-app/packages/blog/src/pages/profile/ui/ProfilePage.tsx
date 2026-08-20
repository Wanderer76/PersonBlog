import { memo, useCallback, useEffect, useRef, useState } from 'react';
import axios from 'axios';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { useNavigate } from 'react-router-dom';
import { PostCard, TextPostCard } from '@/entities/post';
import { Tabs } from '@/shared/ui/Tabs/Tabs';
import { Button } from '@/shared/ui/Button/Button';
import { useIntersectionObserver } from '@/shared/hooks/useIntersectionObserver';
import { Playlist, PlaylistCard } from '@/entities/playlist';
import DefaultProfileIcon from '@/shared/assets/defaultProfilePic.png';
import styles from '@/pages/profile/ui/ProfilePage.module.css';
import { BaseApUrl } from '@/shared/api/client';
import { getPlayList } from '@/shared/api/generated/play-list/play-list';
import { getProfilePostV2 } from '@/shared/api/generated/profile-post-v2/profile-post-v2';
import { getAccessToken, JwtTokenService } from '@/shared/auth/tokenStorage';
import { ProfileHeader, VideoProcessingProgress } from '@/entities/profile';
import { getBlog } from '@/shared/api/generated/blog/blog';
import { BlogModel, UserPostInfoModel } from '@/shared/api/generated/models';

const PAGE_SIZE = 10;
const POST_TYPE = { text: 0, video: 1 } as const;

type ActivePanel = 'posts' | 'playlists' | 'text';
type ProfileViewModel = BlogModel & { totalPostsCount: number };

const profilePostApi = getProfilePostV2();

const initialProfile: ProfileViewModel = {
    id: '',
    photoUrl: DefaultProfileIcon,
    name: null,
    description: '',
    subscribersCount: 0,
    totalPostsCount: 0
};

const getErrorMessage = (error: unknown, fallback: string) => {
    if (axios.isAxiosError(error)) {
        return typeof error.response?.data === 'string' ? error.response.data : fallback;
    }
    return error instanceof Error ? error.message : fallback;
};

export const ProfilePage = memo(() => {
    const navigate = useNavigate();
    const loadingRef = useRef(false);
    const videoHubRef = useRef<HubConnection | null>(null);
    const [profile, setProfile] = useState<ProfileViewModel>(initialProfile);
    const [blogId, setBlogId] = useState<string | null>(null);
    const [posts, setPosts] = useState<UserPostInfoModel[]>([]);
    const [playlists, setPlaylists] = useState<Playlist[]>([]);
    const [page, setPage] = useState(1);
    const [hasMore, setHasMore] = useState(true);
    const [activePanel, setActivePanel] = useState<ActivePanel>('posts');
    const [isLoading, setIsLoading] = useState(false);
    const [isPlaylistsLoading, setIsPlaylistsLoading] = useState(false);
    const [hasLoadedPlaylists, setHasLoadedPlaylists] = useState(false);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);
    const [videoProgress, setVideoProgress] = useState<Record<string, VideoProcessingProgress>>({});
    const [postsReloadKey, setPostsReloadKey] = useState(0);

    useEffect(() => {
        let disposed = false;
        const connection = new HubConnectionBuilder()
            .withUrl(`${BaseApUrl}/videohub`, {
                accessTokenFactory: () => getAccessToken() ?? '',
                withCredentials: false
            })
            .withAutomaticReconnect()
            .build();

        const handleProgress = (progress: VideoProcessingProgress) => {
            setVideoProgress(previous => ({ ...previous, [progress.postId]: progress }));
        };

        connection.on('OnVideoConvertProgress', handleProgress);
        videoHubRef.current = connection;

        const startConnection = async () => {
            while (!disposed && connection.state === HubConnectionState.Disconnected) {
                try {
                    await connection.start();
                    return;
                } catch (error) {
                    if (disposed) return;
                    console.error('Не удалось подключиться к прогрессу обработки видео. Повтор через 3 секунды.', error);
                    await new Promise(resolve => window.setTimeout(resolve, 3000));
                }
            }
        };

        void startConnection();

        return () => {
            disposed = true;
            videoHubRef.current = null;
            connection.off('OnVideoConvertProgress', handleProgress);
            void connection.stop();
        };
    }, []);

    const loadMore = useCallback(() => {
        if (!loadingRef.current) setPage(previousPage => previousPage + 1);
    }, []);

    const { lastElementRef } = useIntersectionObserver({
        onLoadMore: loadMore,
        hasMore: hasMore && !isLoading
    });

    useEffect(() => {
        let isActive = true;
        const controller = new AbortController();
        const loadProfile = async () => {
            try {
                const blogApi = getBlog();
                const { data: hasBlogData } = await blogApi.getApiBlogHasUserBlog();
                if (!isActive || !hasBlogData.hasBlog) return;
                const { data } = await blogApi.getApiBlogDetail();
                if (!isActive) return;
                setProfile({ ...data, totalPostsCount: 0 });
                setBlogId(data.id ?? null);

                const [videoPosts, textPosts] = await Promise.all([
                    profilePostApi.getApiProfilePostV2My(
                        { page: 1, pageSize: 1, postType: POST_TYPE.video },
                        { signal: controller.signal }
                    ),
                    profilePostApi.getApiProfilePostV2My(
                        { page: 1, pageSize: 1, postType: POST_TYPE.text },
                        { signal: controller.signal }
                    )
                ]);
                if (!isActive) return;
                setProfile(previous => ({
                    ...previous,
                    totalPostsCount: videoPosts.data.totalPostsCount + textPosts.data.totalPostsCount
                }));
            } catch (error: unknown) {
                if (isActive && !axios.isCancel(error)) {
                    setErrorMessage(getErrorMessage(error, 'Не удалось загрузить профиль'));
                }
            }
        };
        void loadProfile();
        return () => {
            isActive = false;
            controller.abort();
        };
    }, []);

    useEffect(() => {
        if (!blogId || activePanel === 'playlists') return;
        const controller = new AbortController();
        const postType = activePanel === 'text' ? POST_TYPE.text : POST_TYPE.video;

        const loadPosts = async () => {
            loadingRef.current = true;
            setIsLoading(true);
            setErrorMessage(null);
            try {
                const { data } = await profilePostApi.getApiProfilePostV2My(
                    { page, pageSize: PAGE_SIZE, postType },
                    { signal: controller.signal }
                );
                if (controller.signal.aborted) return;
                setPosts(previousPosts => {
                    if (page === 1) return data.items;
                    const ids = new Set(previousPosts.map(post => post.id));
                    return [...previousPosts, ...data.items.filter(post => !ids.has(post.id))];
                });
                setHasMore(page < data.totalPageCount);
            } catch (error: unknown) {
                if (!axios.isCancel(error)) {
                    setErrorMessage(getErrorMessage(error, 'Не удалось загрузить публикации'));
                }
            } finally {
                if (!controller.signal.aborted) {
                    loadingRef.current = false;
                    setIsLoading(false);
                }
            }
        };
        void loadPosts();
        return () => {
            controller.abort();
            loadingRef.current = false;
            setIsLoading(false);
        };
    }, [activePanel, blogId, page, postsReloadKey]);

    useEffect(() => {
        if (!blogId || activePanel !== 'playlists') return;
        const controller = new AbortController();
        const loadPlaylists = async () => {
            setErrorMessage(null);
            setIsPlaylistsLoading(true);
            setHasLoadedPlaylists(false);
            try {
                const { data } = await getPlayList().getApiPlayListMyList({ signal: controller.signal });
                if (controller.signal.aborted) return;
                setPlaylists(data as Playlist[]);
                setHasLoadedPlaylists(true);
            } catch (error: unknown) {
                if (!axios.isCancel(error)) setErrorMessage(getErrorMessage(error, 'Не удалось загрузить плейлисты'));
            } finally {
                if (!controller.signal.aborted) setIsPlaylistsLoading(false);
            }
        };
        void loadPlaylists();
        return () => controller.abort();
    }, [activePanel, blogId]);

    const handleRemovePost = useCallback(async (id: string) => {
        setErrorMessage(null);
        try {
            await profilePostApi.postApiProfilePostV2RemovePostId(id);
            setPosts([]);
            setPage(1);
            setHasMore(true);
            setPostsReloadKey(previous => previous + 1);
            setProfile(previous => ({
                ...previous,
                totalPostsCount: Math.max(0, previous.totalPostsCount - 1)
            }));
        } catch (error: unknown) {
            setErrorMessage(getErrorMessage(error, 'Не удалось удалить публикацию'));
        }
    }, []);

    const handleRemovePlaylist = async (id: string) => {
        setErrorMessage(null);
        try {
            await getPlayList().postApiPlayListRemovePlaylistId(id);
            setPlaylists(previous => previous.filter(playlist => playlist.id !== id));
        } catch (error: unknown) {
            setErrorMessage(getErrorMessage(error, 'Не удалось удалить плейлист'));
        }
    };

    const handleTabChange = (tab: string) => {
        const nextPanel = tab as ActivePanel;
        if (nextPanel === activePanel) return;
        setActivePanel(nextPanel);
        setErrorMessage(null);
        if (nextPanel !== 'playlists') {
            setPosts([]);
            setPage(1);
            setHasMore(true);
        }
    };

    const tabItems = [
        { id: 'posts', label: 'Мои видео' },
        { id: 'playlists', label: 'Плейлисты' },
        { id: 'text', label: 'Посты' }
    ];

    const rightAction = !blogId
        ? null
        : activePanel === 'posts'
        ? <Button onClick={() => navigate('post/create')}>Создать видео</Button>
        : activePanel === 'playlists'
            ? <Button onClick={() => navigate('playList/create')}>Создать плейлист</Button>
            : <Button onClick={() => navigate('textPost/create')}>Создать пост</Button>;

    return (
        <div className={styles.profileContainer}>
            <ProfileHeader
                profile={profile}
                hasBlog={blogId !== null}
                onLogout={() => { JwtTokenService.cleanAuth(); navigate('/'); }}
                onCreateBlog={() => navigate('blog/create')}
            />
            <section className={styles.postsSection}>
                <Tabs activeTab={activePanel} onChange={handleTabChange} items={tabItems} rightAction={rightAction} />
                {errorMessage && <p className={styles.errorState} role="alert">{errorMessage}</p>}
                <div className={styles.postsGrid}>
                    {activePanel === 'posts' && posts.map((post, index) => (
                        <PostCard key={post.id} post={post} isLast={index === posts.length - 1}
                            onRemove={handleRemovePost} observeRef={lastElementRef}
                            processingProgress={post.id ? videoProgress[post.id] : undefined} />
                    ))}
                    {activePanel === 'text' && posts.map((post, index) => (
                        <TextPostCard
                            key={post.id}
                            post={post}
                            isLast={index === posts.length - 1}
                            observeRef={lastElementRef}
                            onRemove={handleRemovePost}
                        />
                    ))}
                    {activePanel === 'playlists' && playlists.map(playlist => (
                        <PlaylistCard key={playlist.id} playlist={playlist} onRemove={handleRemovePlaylist} />
                    ))}
                    {isLoading && activePanel !== 'playlists' && (
                        <div className={page === 1 ? styles.loadingSpinner : styles.loadingMore}>Загрузка...</div>
                    )}
                    {isPlaylistsLoading && activePanel === 'playlists' && (
                        <div className={styles.loadingSpinner}>Загрузка плейлистов...</div>
                    )}
                    {!isLoading && activePanel !== 'playlists' && posts.length === 0 && (
                        <p className={styles.emptyState}>Публикаций пока нет</p>
                    )}
                    {!hasMore && posts.length > 0 && activePanel !== 'playlists' && (
                        <p className={styles.noMore}>Больше публикаций нет</p>
                    )}
                    {blogId && activePanel === 'playlists' && hasLoadedPlaylists && !isPlaylistsLoading && playlists.length === 0 && (
                        <p className={styles.emptyState}>У вас пока нет плейлистов</p>
                    )}
                </div>
            </section>
        </div>
    );
});

ProfilePage.displayName = 'ProfilePage';
export default ProfilePage;
