import React, { useEffect, useState } from 'react';
import styles from './ProfilePage.module.css';
import { useNavigate } from 'react-router-dom';
import API from '../../scripts/apiMethod';


interface ArtistDetailView {
    avatarUrl: string | null;
    id: string;
    name: string;
    description: string;
    trackCount: number;
}

const ProfilePage: React.FC = () => {
    const navigate = useNavigate();
    const [artist, setArtist] = useState<ArtistDetailView>();

    const getArtistInfo = async () => {
        const response = await API.get<ArtistDetailView>("Artist/my");
        if (response.status == 200) {
            setArtist(response.data);
        }
    };

    useEffect(() => {
        getArtistInfo();
    }, [])

    const handleCreateTrack = () => {
        navigate("/track/create");
    };

    const handleEditProfile = () => {
        alert('Редактирование профиля будет доступно здесь!');
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

    return (
        <div className={styles.container}>
            <div className={styles.profileContainer}>
                <header className={styles.profileHeader}>
                    <img
                        src={artist?.avatarUrl ?? 'vite.svg'}
                        alt="Аватар"
                        className={styles.avatar}
                    />
                    <div className={styles.profileInfo}>
                        <h1>{artist?.name}</h1>
                        <p>{artist?.description}</p>
                        <div className={styles.stats}>
                            <div className={styles.statItem}>
                                <span className={styles.statNumber}>3,245</span>
                                <span>слушателей</span>
                            </div>
                            <div className={styles.statItem}>
                                <span className={styles.statNumber}>{artist?.trackCount}</span>
                                <span>треков</span>
                            </div>
                            <div className={styles.statItem}>
                                <span className={styles.statNumber}>0</span>
                                <span>плейлистов</span>
                            </div>
                        </div>
                        <button className={styles.editBtn} onClick={handleEditProfile}>
                            <i className="fas fa-edit"></i> Редактировать профиль
                        </button>
                    </div>
                </header>
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

                {/* Playlists */}
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
                                    className={`${styles.playlistCover} ${playlist.coverClass || ''
                                        }`}
                                >
                                    {playlist.icon && <i className={`fas ${playlist.icon}`}></i>}
                                    {playlist.img && (
                                        <img src={playlist.img} alt={playlist.name} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
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