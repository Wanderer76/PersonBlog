import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import styles from './PlaylistPage.module.css';
import API from '../../scripts/apiMethod';
import TrackTable from '../../components/trackTable/TrackTable';
import { useAudioPlayerContext } from '../../context/AudioPlayerContext';
import type { TrackViewItem } from '../../types/music';

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
    canEdit: boolean;
    canDelete: boolean;
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
    const audioPlayer = useAudioPlayerContext();

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
                const fetchedTracks = response.data;
                setTracks(fetchedTracks);
                setTotalPages(Math.ceil((playlist?.trackCount || 0) / pageSize));

                // Если это текущий плейлист в плеере, обновляем его
                if (audioPlayer.playlist.length > 0 &&
                    audioPlayer.playlist[0]?.id === id) {
                    audioPlayer.loadPlaylist(fetchedTracks, 0);
                }
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

                // Обновляем плейлист в плеере если он активен
                if (audioPlayer.playlist.length > 0 &&
                    audioPlayer.playlist[0]?.id === id) {
                    const updatedPlaylist = tracks.filter(track => track.id !== trackId);
                    audioPlayer.loadPlaylist(updatedPlaylist, audioPlayer.currentIndex);
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
                // Обновляем локальное состояние
                setTracks(prevTracks =>
                    prevTracks.map(track =>
                        track.id === trackId
                            ? { ...track, isLiked: false }
                            : track
                    )
                );


                if (playlist && playlist.type == 'Liked') {
                    setPlaylist(prev => prev ? {
                        ...prev,
                        trackCount: prev.trackCount - 1
                    } : null);
                }
                // Синхронизируем с плеером, если этот плейлист сейчас играет
                if (audioPlayer.currentTrack &&
                    audioPlayer.currentTrack?.id == trackId) {
                    audioPlayer.currentTrack!.isLiked = false;
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
                // Обновляем локальное состояние
                setTracks(prevTracks =>
                    prevTracks.map(track =>
                        track.id === trackId
                            ? { ...track, isLiked: true }
                            : track
                    )
                );

                // Синхронизируем с плеером, если этот плейлист сейчас играет
                if (audioPlayer.currentTrack &&
                    audioPlayer.currentTrack?.id == trackId) {
                    audioPlayer.currentTrack!.isLiked = true;
                }
            }
        } catch (err: any) {
            console.error("Ошибка при лайке трека:", err);
            alert(err.response?.data?.message || "Не удалось добавить в избранное");
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

    // Воспроизведение конкретного трека
    const handlePlayTrack = (trackId: string) => {
        if (audioPlayer.currentTrack?.id === trackId) {
            if (!audioPlayer.isPlaying)
                audioPlayer.play();
            else
                audioPlayer.pause();

        } else {
            audioPlayer.loadPlaylist(tracks, tracks.findIndex(x => x.id == trackId));
        }
    };

    // Воспроизведение всего плейлиста с начала
    const handlePlayAll = () => {
        if (tracks.length > 0) {
            const tracksWithPlaylistInfo = tracks.map(track => ({
                ...track,
                playlistId: id
            }));
            audioPlayer.loadPlaylist(tracksWithPlaylistInfo, 0);
        }
    };

    // Воспроизведение в случайном порядке
    const handleShufflePlay = () => {
        if (tracks.length > 0) {
            const tracksWithPlaylistInfo = tracks.map(track => ({
                ...track,
                playlistId: id
            }));
            audioPlayer.loadPlaylist(tracksWithPlaylistInfo, 0);
            audioPlayer.toggleShuffle(); // Включаем shuffle
        }
    };

    const handlePageChange = (newPage: number) => {
        setCurrentPage(newPage);
    };

    // Проверяем, является ли этот плейлист текущим в плеере
    const isCurrentPlaylist = audioPlayer.playlist.length > 0 &&
        audioPlayer.playlist[0]?.id === id;

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
            <button className={styles.retryButton} onClick={() => navigate(-1)}>
                Назад
            </button>
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
                            {isCurrentPlaylist && (
                                <span className={styles.currentPlayingBadge}>
                                    • Сейчас играет
                                </span>
                            )}
                        </div>
                        <div className={styles.playlistActions}>
                            <button
                                className={styles.playButton}
                                onClick={handlePlayAll}
                                disabled={tracks.length === 0}
                            >
                                <i className="fas fa-play"></i> Слушать
                            </button>
                            <button
                                className={styles.secondaryButton}
                                onClick={handleShufflePlay}
                                disabled={tracks.length === 0}
                            >
                                <i className="fas fa-random"></i> Перемешать
                            </button>
                            <button className={styles.secondaryButton}>
                                <i className="fas fa-heart"></i> Нравится
                            </button>
                            {playlist.canDelete &&
                                <button className={styles.secondaryButton} onClick={() => {
                                    API.post(`ProfilePlayList/remove/${playlist.id}`)
                                        .then(repsonse => {
                                            if (repsonse.status == 200) { navigate(-1); }
                                            else {
                                                alert(repsonse.data)
                                            }
                                        })
                                }}>
                                    <i className="fas fa-trash"></i> Удалить
                                </button>}
                        </div>
                    </div>
                </header>

                {/* Список треков */}
                <section className={styles.tracksSection}>
                    <div className={styles.sectionHeader}>
                        <h2 className={styles.sectionTitle}>Треки</h2>
                        <button className={styles.playButton}>
                            <i className="fas fa-plus"></i>
                            Добавить трек
                        </button>
                        {tracks.length == 0 && (
                            <div className={styles.sectionActions}>
                                <span className={styles.totalDuration}>
                                    Общая продолжительность: {calculateTotalDuration(tracks)}
                                </span>
                            </div>
                        )}
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
                        isPlaying={audioPlayer.isPlaying}
                        currentPlayingTrackId={audioPlayer.currentTrack?.id}
                    />
                </section>
            </div>
        </div>
    );
};

// Вспомогательная функция для расчета общей продолжительности
const calculateTotalDuration = (tracks: TrackViewItem[]): string => {
    const totalMilliseconds = tracks.reduce((total, track) => total + (track.trackInfo.duration || 0), 0);
    const totalSeconds = totalMilliseconds / 1000;
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);

    if (hours > 0) {
        return `${hours} ч ${minutes} мин`;
    }
    return `${minutes} мин`;
};

export default PlaylistPage;