import { useCallback, useEffect, useRef, useState, memo } from 'react';
import { useNavigate } from 'react-router-dom';
import { PostCard } from '../../features/post-management/components/PostCard/PostCard';
import { Tabs } from '../../shared/ui/Tabs/Tabs';
import { Button } from '../../shared/ui/Button/Button';
import { useIntersectionObserver } from '../../shared/hooks/useIntersectionObserver';
import { Post, PostsPageResponse } from '../../entities/post/types';
import { Playlist } from '../../entities/playlist/types';
import DefaultProfileIcon from '../../defaultProfilePic.png';
import styles from './ProfilePage.module.css';
import API from '@/lib/api/client';
import { getPlayList } from '@/lib/api/generated/play-list/play-list';
import { JwtTokenService } from '@/shared/TokenStrorage';
import { ProfileHeader } from '@/features/post-management/components/ProfileHeader/ProfileHeader';
import { PlaylistCard } from '@/features/post-management/components/PlayListCars/PlaylistCard';
import { getBlog } from '@/lib/api/generated/blog/blog';
import { BlogModel } from '@/lib/api/generated/models';

const PAGE_SIZE = 10;
type ActivePanel = 'posts' | 'playlists';

export const ProfilePage = memo(() => {
    const navigate = useNavigate();
    const blogIdRef = useRef<string | null>(null);

    // ✅ Флаг для отслеживания первой загрузки постов
    const hasLoadedInitialPosts = useRef(false);

    // State
    const [profile, setProfile] = useState<BlogModel>({
        id: '',
        photoUrl: DefaultProfileIcon,
        name: null,
        description: '',
        createdAt: undefined,
        subscribersCount: 0,
        userId: undefined
    });
    const [posts, setPosts] = useState<Post[]>([]);
    const [playlists, setPlaylists] = useState<Playlist[]>([]);
    const [page, setPage] = useState(1);
    const [hasMore, setHasMore] = useState(true);
    const [activePanel, setActivePanel] = useState<ActivePanel>('posts');
    const [isLoading, setIsLoading] = useState(false);

    // Intersection Observer для infinite scroll
    const { lastElementRef } = useIntersectionObserver({
        onLoadMore: () => setPage(prev => prev + 1),
        hasMore
    });

    // Загрузка профиля
    useEffect(() => {
        const loadProfile = async () => {

            const blogApi = getBlog();

            try {
                const hasBlogData = await blogApi.getApiBlogHasUserBlog();

                if (hasBlogData.hasBlog) {
                    const profileData = await blogApi.getApiBlogDetail();
                    setProfile(profileData);
                    blogIdRef.current = profileData.id!;
                    // ✅ Сбрасываем флаг при получении нового blogId
                    hasLoadedInitialPosts.current = false;
                    await loadPosts(page)
                }
            } catch (error: any) {
                if (error.response?.status === 401) {
                    await JwtTokenService.refreshToken();
                    window.location.reload();
                }
                console.error('Failed to load profile:', error);
            }
        };

        loadProfile();
    }, []);

    // ✅ Загрузка постов с пагинацией
    const loadPosts = useCallback(async (pageNum: number, reset: boolean = false) => {
        if (!blogIdRef.current || !hasMore || isLoading) return;

        setIsLoading(true);
        try {
            const { data }: { data: PostsPageResponse } = await API.get(
                `/profile/api/ProfilePostV2/my?page=${pageNum}&pageSize=${PAGE_SIZE}`
            );

            // ✅ Если reset=true, заменяем посты, иначе добавляем
            setPosts(prev => reset ? data.items : [...prev, ...data.items]);
            setProfile(prev => ({ ...prev, totalPostsCount: data.totalPostsCount }));
            setHasMore(data.totalPageCount > pageNum);
        } catch (error: any) {
            if (error.response?.status === 401) {
                await JwtTokenService.refreshToken();
                window.location.reload();
            }
            console.error('Failed to load posts:', error);
        } finally {
            setIsLoading(false);
        }
    }, [hasMore, isLoading]);

    // ✅ Загрузка постов при изменении страницы ИЛИ при первом получении blogId
    useEffect(() => {
        if (blogIdRef.current && activePanel === 'posts') {
            // Первая загрузка
            if (!hasLoadedInitialPosts.current && page === 1) {
                hasLoadedInitialPosts.current = true;
                loadPosts(1, true);
            }
            // Пагинация
            else if (page > 1) {
                loadPosts(page, false);
            }
        }
    }, [page, activePanel, loadPosts]); // ✅ Убрали posts из зависимостей!

    // Загрузка плейлистов
    useEffect(() => {
        const loadPlaylists = async () => {
            if (!blogIdRef.current) return;
            try {
                const data = await getPlayList().getApiPlayListMyList() as Playlist[];
                setPlaylists(data);
            } catch (error) {
                console.error('Failed to load playlists:', error);
            }
        };
        loadPlaylists();
    }, [blogIdRef.current]);

    // Handlers
    const handleRemovePost = async (id: string) => {
        try {
            await API.post(`profile/api/ProfilePostV2/remove/${id}`);
            setPosts(prev => prev.filter(post => post.id !== id));
        } catch (error) {
            console.error('Failed to remove post:', error);
        }
    };

    const handleRemovePlaylist = async (id: string) => {
        try {
            await getPlayList().postApiPlayListRemovePlaylistId(id);
            setPlaylists(prev => prev.filter(pl => pl.id !== id));
        } catch (error: any) {
            if (error.response?.status === 400) {
                alert(error.response.data);
            }
            console.error('Failed to remove playlist:', error);
        }
    };

    const handleLogout = () => {
        JwtTokenService.cleanAuth();
        navigate('/auth');
    };

    const handleEditBlog = () => {
        navigate(blogIdRef.current ? 'blog/edit' : 'blog/create');
    };

    // Tab configuration
    const tabItems = [
        { id: 'posts', label: 'Мои публикации' },
        { id: 'playlists', label: 'Плейлисты' }
    ];

    const getRightAction = () => {
        if (activePanel === 'posts') {
            return (
                <Button onClick={() => navigate('post/create')}>
                    Создать пост
                </Button>
            );
        }
        if (activePanel === 'playlists') {
            return (
                <Button onClick={() => navigate('playList/create')}>
                    Создать плейлист
                </Button>
            );
        }
        return null;
    };

    // ✅ Обработчик смены таба - сбрасываем посты и загружаем заново
    const handleTabChange = (tab: string) => {
        setActivePanel(tab as ActivePanel);
        if (tab === 'posts') {
            setPage(1);
            hasLoadedInitialPosts.current = false;
            if (blogIdRef.current) {
                loadPosts(1, true);
            }
        }
    };

    return (
        <div className={styles.profileContainer}>
            <ProfileHeader
                profile={profile}
                hasBlog={!!blogIdRef.current}
                onLogout={handleLogout}
                onEditBlog={handleEditBlog}
            />

            <section className={styles.postsSection}>
                <Tabs
                    activeTab={activePanel}
                    onChange={handleTabChange}
                    items={tabItems}
                    rightAction={getRightAction()}
                />

                <div className={styles.postsGrid}>
                    {activePanel === 'posts' && (
                        <>
                            {posts.map((post, index) => (
                                <PostCard
                                    key={post.id}
                                    post={post}
                                    isLast={index === posts.length - 1}
                                    onRemove={handleRemovePost}
                                    observeRef={lastElementRef}
                                />
                            ))}
                            {isLoading && page === 1 && (
                                <div className={styles.loadingSpinner}>Загрузка...</div>
                            )}
                            {isLoading && page > 1 && (
                                <div className={styles.loadingMore}>Загрузка ещё...</div>
                            )}
                            {!hasMore && posts.length > 0 && (
                                <p className={styles.noMore}>Больше постов нет</p>
                            )}
                            {posts.length === 0 && !isLoading && (
                                <p className={styles.emptyState}>У вас пока нет публикаций</p>
                            )}
                        </>
                    )}

                    {activePanel === 'playlists' && (
                        <>
                            {playlists.map(playlist => (
                                <PlaylistCard
                                    key={playlist.id}
                                    playlist={playlist}
                                    onRemove={handleRemovePlaylist}
                                />
                            ))}
                            {playlists.length === 0 && (
                                <p className={styles.emptyState}>У вас пока нет плейлистов</p>
                            )}
                        </>
                    )}
                </div>
            </section>
        </div>
    );
});

ProfilePage.displayName = 'ProfilePage';
export default ProfilePage;