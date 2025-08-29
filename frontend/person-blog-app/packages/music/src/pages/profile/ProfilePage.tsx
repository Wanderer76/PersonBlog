import React, { useEffect, useState } from 'react';
import styles from './ProfilePage.module.css';
import { useNavigate } from 'react-router-dom';
import API, { AuthUrl } from '../../scripts/apiMethod';

interface ArtistDetailView {
    avatarUrl: string | null;
    id: string;
    name: string;
    description: string;
    trackCount: number;
}

interface ProfilView {
    id?: string;
    name: string;
    email?: string;
    birthdate?: string;
    userId?: string;
    photoUrl?: string;
    profileState?: string;
}

const ProfilePage: React.FC = () => {
    const navigate = useNavigate();
    const [artist, setArtist] = useState<ArtistDetailView | null>(null); // null = артист не создан
    const [profile, setProfile] = useState<ProfilView | null>(null); // null = артист не создан
    const [loading, setLoading] = useState(true);

    const getArtistInfo = async () => {
        try {
            const response = await API.get<ArtistDetailView>("Artist/my");
            if (response.status === 200) {
                setArtist(response.data);
            }
        } catch (error: any) {
            // Если артист не найден (например, 404), считаем, что он не создан
            if (error.response?.status === 400) {
                setArtist(null);
                var profileResp = await API.get<ProfilView>(`${AuthUrl}/video/api/Profile/my`);
                if (profileResp.status == 200) {
                    setProfile(profileResp.data);
                }
            } else {
                console.error("Ошибка при загрузке профиля артиста:", error);
            }
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        getArtistInfo();
    }, []);

    const handleCreateTrack = () => {
        navigate("/track/create");
    };

    const handleEditProfile = () => {
        if (artist) {
            navigate("/artist/edit"); // Переход на редактирование артиста
        } else {
            alert('Сначала станьте артистом!');
        }
    };

    const handleBecomeArtist = () => {
        navigate("/artist/create"); // Переход на создание артиста
    };

    const handlePlaylistClick = (name: string) => {
        alert(`Открывается плейлист: ${name}`);
    };

    const actionItems = [
        { icon: 'fa-headphones', label: 'Слушать' },
        { icon: 'fa-history', label: 'История' },
        { icon: 'fa-download', label: 'Загрузки' },
        { icon: 'fa-heart', label: 'Избранное' },
        { icon: 'fa-cog', label: 'Настройки' },
    ];

    const playlists = [
        {
            name: 'Мне нравится',
            tracks: '124 трека',
            badge: 'Приватный',
            coverClass: styles.likedCover,
            icon: 'fa-heart',
            img: null
        },
        {
            name: 'Загруженные',
            tracks: '87 треков',
            badge: 'Только я',
            coverClass: styles.uploadedCover,
            icon: 'fa-cloud-upload-alt',
            img: null
        },
    ];

    if (loading) {
        return (
            <div className={styles.container}>
                <div className={styles.profileContainer}>
                    <p>Загрузка профиля...</p>
                </div>
            </div>
        );
    }

    return (
        <div className={styles.container}>
            <div className={styles.profileContainer}>
                {/* Заголовок профиля */}
                <header className={styles.profileHeader}>
                    <img
                        src={artist?.avatarUrl ?? profile?.photoUrl ?? '/no-image.png'} // Заглушка
                        alt="Аватар"
                        className={styles.avatar}
                    />
                    <div className={styles.profileInfo}>
                        {artist ? (
                            <>
                                <h1>{artist.name}</h1>
                                <p>{artist.description}</p>
                                <div className={styles.stats}>
                                    <div className={styles.statItem}>
                                        <span className={styles.statNumber}>3,245</span>
                                        <span>слушателей</span>
                                    </div>
                                    <div className={styles.statItem}>
                                        <span className={styles.statNumber}>{artist.trackCount}</span>
                                        <span>треков</span>
                                    </div>
                                    <div className={styles.statItem}>
                                        <span className={styles.statNumber}>0</span>
                                        <span>плейлистов</span>
                                    </div>
                                </div>
                            </>
                        ) : (
                            <>
                                <h1>{profile?.name}</h1>
                            </>
                        )}

                        <div className={styles.buttonGroup}>
                            {artist ? (
                                <button className={styles.editBtn} onClick={handleEditProfile}>
                                    <i className="fas fa-edit"></i> Редактировать профиль
                                </button>
                            ) : (
                                <button className={styles.changeTrackButton} onClick={handleBecomeArtist}>
                                    Стать артистом
                                </button>
                            )}
                        </div>
                    </div>
                </header>

                {/* Панель действий */}
                <div className={styles.actionBar}>
                    {actionItems.map((item, index) => (
                        <div key={index} className={styles.actionItem}>
                            <div className={styles.actionIcon}>
                                <i className={`fas ${item.icon}`}></i>
                            </div>
                            <span>{item.label}</span>
                        </div>
                    ))}
                </div>

                {/* Плейлисты */}
                <section>
                    <div className={styles.sectionTitle}>
                        <h2>Мои плейлисты</h2>
                        <button className={styles.createTrackBtn} onClick={handleCreateTrack}>
                            <i className="fas fa-plus"></i> Создать трек
                        </button>
                    </div>

                    <div className={styles.playlistsGrid}>
                        {playlists.map((playlist, index) => (
                            <div
                                key={index}
                                className={styles.playlistCard}
                                onClick={() => handlePlaylistClick(playlist.name)}
                            >
                                <div
                                    className={`${styles.playlistCover} ${playlist.coverClass || ''}`}
                                >
                                    {playlist.icon && <i className={`fas ${playlist.icon}`}></i>}
                                    {playlist.img && (
                                        <img
                                            src={playlist.img}
                                            alt={playlist.name}
                                            style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                                        />
                                    )}
                                </div>
                                <h3 className={styles.playlistName}>{playlist.name}</h3>
                                <p className={styles.playlistInfo}>
                                    {playlist.tracks}
                                    {playlist.badge && (
                                        <span className={styles.badge}>{playlist.badge}</span>
                                    )}
                                </p>
                            </div>
                        ))}
                    </div>
                </section>
            </div>
        </div>
    );
};

export default ProfilePage;