import { useCallback, useEffect, useRef, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import API from "../../scripts/apiMethod";
import styles from './ChannelPage.module.css';
import DefaultProfileIcon from '../../defaultProfilePic.png';
import { getLocalDateTime } from "../../scripts/LocalDate";
import SideBar from "../../components/sidebar/SideBar";

const ChannelPage = () => {
    const { channelId } = useParams();
    const navigate = useNavigate();
    const [channel, setChannel] = useState({
        photoUrl: DefaultProfileIcon,
        name: "Название канала",
        description: "Описание канала",
        subscribersCount: 0,
        createdAt: new Date().toISOString(),
        isSubscribed: false
    });

    const [videos, setVideos] = useState([]);
    const [playlists, setPlaylists] = useState([]);
    const [activeTab, setActiveTab] = useState('videos');
    const [page, setPage] = useState(1);
    const [hasMore, setHasMore] = useState(true);
    const observer = useRef();
    const pageSize = 10;

    // Для бесконечной подгрузки
    const lastVideoRef = useCallback(node => {
        if (observer.current) observer.current.disconnect();
        observer.current = new IntersectionObserver(entries => {
            if (entries[0].isIntersecting && hasMore) {
                setPage(prev => prev + 1);
            }
        });
        if (node) observer.current.observe(node);
    }, [hasMore]);

    // Загрузка данных канала
    useEffect(() => {
        const loadChannelData = async () => {
            try {
                const response = await API.get(`/video/api/Channel/${channelId}`);
                if (response.status === 200) {
                    setChannel({
                        ...response.data,
                        photoUrl: response.data.photoUrl || DefaultProfileIcon
                    });
                }
            } catch (error) {
                console.error("Ошибка загрузки канала:", error);
            }
        };

        loadChannelData();
    }, [channelId]);

useEffect(() => {
    const loadVideos = async () => {
        try {
            const response = await API.get(
                `/video/api/Channel/posts/${channelId}?page=${page}&size=${pageSize}`
            );
            if (response.status === 200) {
                // Преобразуем данные из API в нужный формат
                const formattedVideos = response.data.posts.map(post => ({
                    id: post.id,
                    title: post.title,
                    description: post.description,
                    previewUrl: post.previewId,
                    duration: formatDuration(post.videoData?.duration),
                    views: 0, // Добавьте реальное количество просмотров, если оно есть в API
                    createdAt: post.createdAt,
                    state: post.state,
                    errorMessage: post.errorMessage
                }));

                setVideos(prev => [...prev, ...formattedVideos]);
                setHasMore(page < response.data.totalPageCount);
            }
        } catch (error) {
            console.error("Ошибка загрузки видео:", error);
        }
    };

    if (activeTab === 'videos') {
        loadVideos();
    }
}, [channelId, page, activeTab]);

// Вспомогательная функция для форматирования длительности
const formatDuration = (seconds) => {
    if (!seconds) return '0:00';
    
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = Math.floor(seconds % 60);
    return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
};

    // Загрузка плейлистов
    useEffect(() => {
        const loadPlaylists = async () => {
            try {
                const response = await API.get(
                    `/video/api/PlayList/channel/${channelId}`
                );
                if (response.status === 200) {
                    setPlaylists(response.data);
                }
            } catch (error) {
                console.error("Ошибка загрузки плейлистов:", error);
            }
        };

        if (activeTab === 'playlists') {
            loadPlaylists();
        }
    }, [channelId, activeTab]);

    // Подписка/отписка
    const handleSubscribe = async () => {
        try {
            const endpoint = channel.isSubscribed
                ? `/video/api/Subscriber/unsubscribe/${channelId}`
                : `/video/api/Subscriber/subscribe/${channelId}`;

            const response = await API.post(endpoint);

            if (response.status === 200) {
                setChannel(prev => ({
                    ...prev,
                    isSubscribed: !prev.isSubscribed,
                    subscribersCount: prev.isSubscribed
                        ? prev.subscribersCount - 1
                        : prev.subscribersCount + 1
                }));
            }
        } catch (error) {
            console.error("Ошибка подписки:", error);
        }
    };

    // Рендер видео
    const renderVideos = () => {
    return videos.map((video, index) => (
        <div
            key={video.id}
            className={styles.videoCard}
            ref={videos.length === index + 1 ? lastVideoRef : null}
            onClick={() => video.state === 1 && navigate(`/video/${video.id}`)}
        >
            <div className={styles.thumbnail}>
                <img src={video.previewUrl} alt={video.title} />
                <span className={styles.duration}>{video.duration}</span>
                {video.state !== 1 && (
                    <div className={styles.videoStatus}>
                        {video.state === 0 ? 'В обработке' : video.errorMessage || 'Ошибка'}
                    </div>
                )}
            </div>
            <div className={styles.videoInfo}>
                <h3>{video.title}</h3>
                <div className={styles.meta}>
                    <span>{video.views} просмотров</span>
                    <span>•</span>
                    <span>{new Date(video.createdAt).toLocaleDateString()}</span>
                </div>
                {video.description && (
                    <p className={styles.videoDescription}>{video.description}</p>
                )}
            </div>
        </div>
    ));
};

    // Рендер плейлистов
    const renderPlaylists = () => {
        return playlists.map(playlist => (
            <div
                key={playlist.id}
                className={styles.playlistCard}
                onClick={() => navigate(`/playlist/${playlist.id}`)}
            >
                <div className={styles.playlistThumbnail}>
                    <img src={playlist.thumbnailUrl} alt={playlist.title} />
                    <span className={styles.videoCount}>{playlist.videosCount} видео</span>
                </div>
                <div className={styles.playlistInfo}>
                    <h3>{playlist.title}</h3>
                    <p>{playlist.description}</p>
                </div>
            </div>
        ));
    };

    return (
        <div className={styles.channelContainer}>
            <SideBar />
            <div className={styles.profileContainer}>
                {/* Шапка канала */}
                <div className={styles.profileHeader}>
                    <div className={styles.avatarSection}>
                        <div className={styles.avatarWrapper}>
                            <img
                                src={channel.photoUrl}
                                alt={channel.name}
                                className={styles.profileAvatar}
                            />
                        </div>

                        <div className={styles.profileInfo}>
                            <h1 className={styles.blogTitle}>{channel.name}</h1>
                            <div className={styles.profileMeta}>
                                <span>👥 {channel.subscribersCount} подписчиков</span>
                                <span>📅 Канал создан: {getLocalDateTime(channel.createdAt)}</span>
                            </div>
                            <p className={styles.postDescription}>{channel.description}</p>
                        </div>
                    </div>

                    <button
                        className={`${styles.btn} ${channel.isSubscribed ? styles.btnSecondary : styles.btnPrimary}`}
                        onClick={handleSubscribe}
                    >
                        {channel.isSubscribed ? 'Вы подписаны' : 'Подписаться'}
                    </button>
                </div>

                {/* Табы */}
                <div className={styles.postSectionHeader}>
                    <div className={styles.tabButtons}>
                        <button
                            className={`${styles.tabButton} ${activeTab === 'videos' ? styles.active : ''}`}
                            onClick={() => setActiveTab('videos')}
                        >
                            Видео
                        </button>
                        <button
                            className={`${styles.tabButton} ${activeTab === 'playlists' ? styles.active : ''}`}
                            onClick={() => setActiveTab('playlists')}
                        >
                            Плейлисты
                        </button>
                        <button
                            className={`${styles.tabButton} ${activeTab === 'about' ? styles.active : ''}`}
                            onClick={() => setActiveTab('about')}
                        >
                            О канале
                        </button>
                    </div>
                </div>

                {/* Контент */}
                <div className={styles.postsSection}>
                    {activeTab === 'videos' && (
                        <div className={styles.postsGrid}>
                            {videos.length > 0 ? renderVideos() : <p>Нет доступных видео</p>}
                        </div>
                    )}

                    {activeTab === 'playlists' && (
                        <div className={styles.postsGrid}>
                            {playlists.length > 0 ? renderPlaylists() : <p>Нет доступных плейлистов</p>}
                        </div>
                    )}

                    {activeTab === 'about' && (
                        <div className={styles.aboutSection}>
                            <h2>О канале</h2>
                            <p>{channel.description || 'Описание отсутствует'}</p>

                            <div className={styles.details}>
                                <div className={styles.detailItem}>
                                    <span>Дата создания:</span>
                                    <span>{getLocalDateTime(channel.createdAt)}</span>
                                </div>
                                <div className={styles.detailItem}>
                                    <span>Подписчиков:</span>
                                    <span>{channel.subscribersCount}</span>
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            </div></div>
    );
};

export default ChannelPage;