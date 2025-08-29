import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import styles from './PlaylistPage.module.css';
import API from '../../scripts/apiMethod';
import { formatDuration } from '../../scripts/helper';

interface ArtistInfo {
    id: string;
    name: string;
}

interface TrackFileInfo {
    duration: number;
    size: number;
    format: string;
}

interface TrackViewItem {
    id: string;
    name: string;
    thumbnailUrl: string | null;
    albumId: string | null;
    trackInfo: TrackFileInfo;
    artists: ArtistInfo[];
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

    const fetchPlaylistInfo = async () => {
        try {
            // Предположим, что у нас есть endpoint для получения информации о плейлисте
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
                // Предположим, что API возвращает информацию о пагинации в заголовках
                // В реальности вам может потребоваться адаптировать это под ваш API
                setTotalPages(Math.ceil((playlist?.trackCount || 0) / pageSize));
            }
        } catch (err: any) {
            console.error("Ошибка при загрузке треков плейлиста:", err);
            setError(err.response?.data?.message || "Не удалось загрузить треки");
        } finally {
            setLoading(false);
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
        // Реализация воспроизведения трека
        console.log("Воспроизведение трека:", trackId);
    };

    const handlePageChange = (newPage: number) => {
        if (newPage >= 1 && newPage <= totalPages) {
            setCurrentPage(newPage);
        }
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
    const playlistsTypes = {
        'Liked': { icon: 'fa-heart', coverClass: styles.likedCover },
        'Upload': { icon: 'fa-cloud-upload-alt', coverClass: styles.uploadedCover },
    };

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

                    {loading ? (
                        <div className={styles.loading}>Загрузка треков...</div>
                    ) : error ? (
                        <div className={styles.error}>
                            <p>{error}</p>
                            <button className={styles.retryButton} onClick={() => fetchTracks(currentPage)}>
                                Попробовать снова
                            </button>
                        </div>
                    ) : (
                        <>
                            <table className={styles.trackList}>
                                <thead>
                                    <tr>
                                        <th className={styles.trackHeader}>#</th>
                                        <th className={styles.trackHeader}>Название</th>
                                        <th className={styles.trackHeader}>Альбом</th>
                                        <th className={styles.trackHeader}>Длительность</th>
                                        <th className={styles.trackHeader}></th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {tracks.map((track, index) => (
                                        <tr key={track.id} className={styles.trackRow}>
                                            <td className={`${styles.trackCell} ${styles.trackIndex}`}>
                                                {(currentPage - 1) * pageSize + index + 1}
                                            </td>
                                            <td className={styles.trackCell}>
                                                <div className={styles.trackInfo}>
                                                    <img
                                                        src={track.thumbnailUrl || '/default-track.png'}
                                                        alt={track.name}
                                                        className={styles.trackThumbnail}
                                                    />
                                                    <div className={styles.trackDetails}>
                                                        <div className={styles.trackName}>{track.name}</div>
                                                        <div className={styles.trackArtists}>
                                                            {track.artists.map(artist => artist.name).join(', ')}
                                                        </div>
                                                    </div>
                                                </div>
                                            </td>
                                            <td className={styles.trackCell}>-</td>
                                            <td className={styles.trackCell}>
                                                <span className={styles.trackDuration}>
                                                    {formatDuration(track.trackInfo.duration)}
                                                </span>
                                            </td>
                                            <td className={styles.trackCell}>
                                                <div className={styles.trackActions}>
                                                    <button
                                                        className={styles.actionButton}
                                                        onClick={() => handlePlayTrack(track.id)}
                                                    >
                                                        <i className="fas fa-play"></i>
                                                    </button>
                                                    <button className={styles.actionButton}>
                                                        <i className="fas fa-heart"></i>
                                                    </button>
                                                    <button className={styles.actionButton}>
                                                        <i className="fas fa-ellipsis-h"></i>
                                                    </button>
                                                </div>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>

                            {/* Пагинация */}
                            {totalPages > 1 && (
                                <div className={styles.pagination}>
                                    <button
                                        className={styles.paginationButton}
                                        onClick={() => handlePageChange(currentPage - 1)}
                                        disabled={currentPage === 1}
                                    >
                                        <i className="fas fa-chevron-left"></i> Назад
                                    </button>
                                    <span className={styles.paginationInfo}>
                                        Страница {currentPage} из {totalPages}
                                    </span>
                                    <button
                                        className={styles.paginationButton}
                                        onClick={() => handlePageChange(currentPage + 1)}
                                        disabled={currentPage === totalPages}
                                    >
                                        Вперед <i className="fas fa-chevron-right"></i>
                                    </button>
                                </div>
                            )}
                        </>
                    )}
                </section>
            </div>
        </div>
    );
};

export default PlaylistPage;