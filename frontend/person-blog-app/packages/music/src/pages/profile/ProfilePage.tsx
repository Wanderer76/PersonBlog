import React, { useEffect, useState } from 'react';
import styles from './ProfilePage.module.css';
import { useNavigate } from 'react-router-dom';
import API, { AuthUrl } from '../../scripts/apiMethod';
import { JwtTokenService } from '../../scripts/TokenStrorage';

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

enum PlayListType {
    Liked = "Liked",
    Upload = "Upload",
    Created = "Created",
}

interface PlayListItem {
    id: string;
    title: string;
    trackCount: number;
    thumbnailUrl: string;
    type: PlayListType;
}

interface CreatePlaylistRequest {
    title: string;
}

const ProfilePage: React.FC = () => {
    const navigate = useNavigate();
    const [artist, setArtist] = useState<ArtistDetailView | null>(null);
    const [profile, setProfile] = useState<ProfilView | null>(null);
    const [playlists, setPlayLists] = useState<PlayListItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [showCreatePlaylistModal, setShowCreatePlaylistModal] = useState(false);
    const [newPlaylistTitle, setNewPlaylistTitle] = useState('');
    const [isCreating, setIsCreating] = useState(false);

    const getArtistInfo = async () => {
        try {
            const response = await API.get<ArtistDetailView>("Artist/my");
            if (response.status === 200) {
                setArtist(response.data);
            }
        } catch (error: any) {
            if (error.response?.status === 400 || error.response?.status === 403) {
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

    const getPlayListInfo = async () => {
        const response = await API.get<PlayListItem[]>("ProfilePlayList/list");
        if (response.status === 200) {
            setPlayLists(response.data);
        }
    };

    const createPlaylist = async () => {
        if (!newPlaylistTitle.trim()) {
            alert('Введите название плейлиста');
            return;
        }

        setIsCreating(true);
        try {
            const request: CreatePlaylistRequest = {
                title: newPlaylistTitle.trim()
            };

            const response = await API.post<PlayListItem>("ProfilePlayList/create", request);
            if (response.status === 200 || response.status === 201) {
                // Обновляем список плейлистов
                await getPlayListInfo();
                setShowCreatePlaylistModal(false);
                setNewPlaylistTitle('');
            }
        } catch (error: any) {
            console.error("Ошибка при создании плейлиста:", error);
            alert('Не удалось создать плейлист');
        } finally {
            setIsCreating(false);
        }
    };

    useEffect(() => {
        if (profile)
            getPlayListInfo();
    }, [profile])

    useEffect(() => {
        getArtistInfo();
    }, []);

    const handleCreateTrack = () => {
        navigate("/track/create");
    };

    const handleCreatePlaylist = () => {
        setShowCreatePlaylistModal(true);
    };

    const handleCloseModal = () => {
        setShowCreatePlaylistModal(false);
        setNewPlaylistTitle('');
    };

    const handleEditProfile = () => {
        if (artist) {
            navigate("/artist/edit");
        } else {
            alert('Сначала станьте артистом!');
        }
    };

    const handleBecomeArtist = () => {
        navigate("/artist/create");
    };

    const handlePlaylistClick = (id: string) => {
        navigate(`/playlist/${id}`)
    };

    const actionItems = [
        { icon: 'fa-headphones', label: 'Слушать' },
        { icon: 'fa-history', label: 'История' },
        { icon: 'fa-download', label: 'Загрузки' },
        { icon: 'fa-heart', label: 'Избранное' },
        { icon: 'fa-cog', label: 'Настройки' },
    ];

    const playlistsTypes = {
        'Liked': { icon: 'fa-heart', coverClass: styles.likedCover },
        'Upload': { icon: 'fa-cloud-upload-alt', coverClass: styles.uploadedCover },
        'Created': { icon: 'fa-cloud-upload-alt', coverClass: styles.uploadedCover },
    };

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
                        src={artist?.avatarUrl ?? profile?.photoUrl ?? '/no-image.png'}
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
                            <br />
                            <button className={styles.changeTrackButton} onClick={(e) => {
                                JwtTokenService.cleanAuth();
                                navigate("/")
                            }}>
                                <i className="fas fa-edit"></i> Выйти
                            </button>
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
                        <button className={styles.createTrackBtn} onClick={handleCreatePlaylist}>
                            <i className="fas fa-plus"></i> Создать плейлист
                        </button>
                    </div>

                    <div className={styles.playlistsGrid}>
                        {playlists.map((playlist, index) => {
                            return (
                                <div
                                    key={index}
                                    className={styles.playlistCard}
                                    onClick={() => handlePlaylistClick(playlist.id)}>
                                    <div
                                        className={`${styles.playlistCover} ${playlistsTypes[playlist.type].coverClass || ''}`}>
                                        {playlistsTypes[playlist.type].icon && <i className={`fas ${playlistsTypes[playlist.type].icon}`}></i>}
                                        {playlist.thumbnailUrl && (
                                            <img
                                                src={playlist.thumbnailUrl}
                                                alt={playlist.title}
                                                style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                                        )}
                                    </div>
                                    <h3 className={styles.playlistName}>{playlist.title}</h3>
                                    <p className={styles.playlistInfo}>
                                        {playlist.trackCount} треков
                                    </p>
                                </div>
                            );
                        })}
                    </div>
                </section>
            </div>

            {/* Модальное окно создания плейлиста */}
            {showCreatePlaylistModal && (
                <div className={styles.modalOverlay}>
                    <div className={styles.modal}>
                        <div className={styles.modalHeader}>
                            <h3>Создать плейлист</h3>
                            <button className={styles.closeButton} onClick={handleCloseModal}>
                                <i className="fas fa-times"></i>
                            </button>
                        </div>
                        <div className={styles.modalBody}>
                            <input
                                type="text"
                                placeholder="Название плейлиста"
                                value={newPlaylistTitle}
                                onChange={(e) => setNewPlaylistTitle(e.target.value)}
                                className={styles.playlistInput}
                                onKeyPress={(e) => {
                                    if (e.key === 'Enter') {
                                        createPlaylist();
                                    }
                                }}
                            />
                        </div>
                        <div className={styles.modalFooter}>
                            <button
                                className={styles.cancelButton}
                                onClick={handleCloseModal}
                                disabled={isCreating}
                            >
                                Отмена
                            </button>
                            <button
                                className={styles.createButton}
                                onClick={createPlaylist}
                                disabled={isCreating}
                            >
                                {isCreating ? 'Создание...' : 'Создать'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default ProfilePage;