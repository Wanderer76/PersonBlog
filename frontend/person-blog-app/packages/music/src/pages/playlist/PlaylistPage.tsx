import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import styles from './PlaylistPage.module.css';
import API from '../../scripts/apiMethod';
import TrackTable, { type TrackViewItem } from '../../components/trackTable/TrackTable';

interface ArtistInfo {
    id: string;
    name: string;
}

interface TrackFileInfo {
    duration: number;
    size: number;
    format: string;
}

enum PlayListType {
    Liked = "Liked",
    Upload = "Upload",
}

interface PlaylistInfo {
    id: string;
    title: string;
    description: string | null;
    thumbnailUrl: string | null;
    trackCount: number;
    ownerName: string;
    type: PlayListType;
}

const PlaylistPage: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const [tracks, setTracks] = useState<TrackViewItem[]>([]);
    const [playlist, setPlaylist] = useState<PlaylistInfo | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [currentPage, setCurrentPage] = useState(1);
    const [pageSize] = useState(10);
    const [totalPages, setTotalPages] = useState(1);
    const [deletingTrackId, setDeletingTrackId] = useState<string | null>(null);

    const fetchPlaylistInfo = async () => {
        try {
            const response = await API.get<PlaylistInfo>(`ProfilePlayList/${id}`);
            if (response.status === 200) {
                setPlaylist(response.data);
            }
        } catch (err) {
            console.error("Ошибка при загрузке информации о плейлисте:", err);
        }
    };

    const fetchTracks = async (page: number) => {
        try {
            setLoading(true);
            setError(null);
            const response = await API.get<TrackViewItem[]>(`ProfilePlayList/${id}/tracks?page=${page}&size=${pageSize}`);

            if (response.status === 200) {
                setTracks(response.data);
                setTotalPages(Math.ceil((playlist?.trackCount || 0) / pageSize));
            }
        } catch (err: any) {
            console.error("Ошибка при загрузке треков плейлиста:", err);
            setError(err.response?.data?.message || "Не удалось загрузить треки");
        } finally {
            setLoading(false);
        }
    };

    const handleDeleteTrack = async (trackId: string) => {
        var removeFromUploadTitle = playlist?.type == PlayListType.Upload ? '\nУдаляя трек из плейлиста "Загруженные" вы удаляете его из системы' : '';
        if (!window.confirm(`Вы уверены, что хотите удалить этот трек из плейлиста?${removeFromUploadTitle}`)) {
            return;
        }

        try {
            setDeletingTrackId(trackId);
            const response = await API.post(`ProfilePlayList/${id}/tracks/${trackId}/delete`);

            if (response.status === 200 || response.status === 204) {
                setTracks(prevTracks => prevTracks.filter(track => track.id !== trackId));

                if (playlist) {
                    setPlaylist(prev => prev ? {
                        ...prev,
                        trackCount: prev.trackCount - 1
                    } : null);
                }

                if (tracks.length === 1 && currentPage > 1) {
                    setCurrentPage(prev => prev - 1);
                } else {
                    fetchTracks(currentPage);
                }
            }
        } catch (err: any) {
            console.error("Ошибка при удалении трека:", err);
            alert(err.response?.data?.message || "Не удалось удалить трек");
        } finally {
            setDeletingTrackId(null);
        }
    };

    const handleUnlikeTrack = async (trackId: string) => {
        try {
            const response = await API.post(`ProfilePlayList/unliked?trackId=${trackId}`);
            if (response.status === 200 || response.status === 204) {
                setTracks(prevTracks => prevTracks.filter(track => track.id !== trackId));

                if (playlist) {
                    setPlaylist(prev => prev ? {
                        ...prev,
                        trackCount: prev.trackCount - 1
                    } : null);
                }

                if (tracks.length === 1 && currentPage > 1) {
                    setCurrentPage(prev => prev - 1);
                } else {
                    fetchTracks(currentPage);
                }
            }
        } catch (err: any) {
            console.error("Ошибка при удалении трека:", err);
            alert(err.response?.data?.message || "Не удалось удалить трек");
        }
    };

    const handleLikeTrack = async (trackId: string) => {
        try {
            const response = await API.post(`ProfilePlayList/liked?trackId=${trackId}`);
            if (response.status === 200 || response.status === 204) {
                setTracks(prevTracks => prevTracks.filter(track => track.id !== trackId));

                if (playlist) {
                    setPlaylist(prev => prev ? {
                        ...prev,
                        trackCount: prev.trackCount - 1
                    } : null);
                }

                if (tracks.length === 1 && currentPage > 1) {
                    setCurrentPage(prev => prev - 1);
                } else {
                    fetchTracks(currentPage);
                }
            }
        } catch (err: any) {
            console.error("Ошибка при удалении трека:", err);
            alert(err.response?.data?.message || "Не удалось удалить трек");
        }
    };

    useEffect(() => {
        if (id) {
            fetchPlaylistInfo();
        }
    }, [id]);

    useEffect(() => {
        if (id && playlist) {
            fetchTracks(currentPage);
        }
    }, [id, currentPage, playlist]);

    const handlePlayTrack = (trackId: string) => {
        console.log("Воспроизведение трека:", trackId);
    };

    const handlePageChange = (newPage: number) => {
        setCurrentPage(newPage);
    };

    if (loading && !playlist) {
        return (
            <div className={styles.container}>
                <div className={styles.loading}>Загрузка плейлиста...</div>
            </div>
        );
    }

    if (error && !playlist) {
        return (
            <div className={styles.container}>
                <div className={styles.error}>
                    <p>{error}</p>
                    <button className={styles.retryButton} onClick={() => fetchPlaylistInfo()}>
                        Попробовать снова
                    </button>
                </div>
            </div>
        );
    }

    if (!playlist) {
        return (
            <div className={styles.container}>
                <div className={styles.error}>
                    <p>Плейлист не найден</p>
                    <button className={styles.retryButton} onClick={() => navigate(-1)}>
                        Назад
                    </button>
                </div>
            </div>
        );
    }

    return (
        <div className={styles.container}>
            <div className={styles.playlistContainer}>
                {/* Шапка плейлиста */}
                <header className={styles.playlistHeader}>
                    <img
                        src={playlist.thumbnailUrl || '/default-playlist.png'}
                        alt={playlist.title}
                        className={styles.playlistCover}
                    />
                    <div className={styles.playlistInfo}>
                        <h1 className={styles.playlistTitle}>{playlist.title}</h1>
                        {playlist.description && (
                            <p className={styles.playlistDescription}>{playlist.description}</p>
                        )}
                        <div className={styles.playlistStats}>
                            <span className={styles.playlistOwner}>{playlist.ownerName}</span>
                            <span>•</span>
                            <span>{playlist.trackCount} треков</span>
                        </div>
                        <div className={styles.playlistActions}>
                            <button className={styles.playButton}>
                                <i className="fas fa-play"></i> Слушать
                            </button>
                            <button className={styles.secondaryButton}>
                                <i className="fas fa-heart"></i> Нравится
                            </button>
                            <button className={styles.secondaryButton}>
                                <i className="fas fa-ellipsis-h"></i> Ещё
                            </button>
                        </div>
                    </div>
                </header>

                {/* Список треков */}
                <section className={styles.tracksSection}>
                    <div className={styles.sectionHeader}>
                        <h2 className={styles.sectionTitle}>Треки</h2>
                    </div>

                    <TrackTable
                        tracks={tracks}
                        currentPage={currentPage}
                        pageSize={pageSize}
                        loading={loading}
                        error={error}
                        totalPages={totalPages}
                        deletingTrackId={deletingTrackId}
                        onPlayTrack={handlePlayTrack}
                        onDeleteTrack={handleDeleteTrack}
                        onUnlikeTrack={handleUnlikeTrack}
                        onLikeTrack={handleLikeTrack}
                        onPageChange={handlePageChange}
                        showPagination={true}
                    />
                </section>
            </div>
        </div>
    );
};

export default PlaylistPage;