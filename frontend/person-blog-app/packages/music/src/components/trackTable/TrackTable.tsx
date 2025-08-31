import React from 'react';
import styles from './TrackTable.module.css';
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

export interface TrackViewItem {
    id: string;
    name: string;
    isLiked: boolean;
    thumbnailUrl: string | null;
    albumId: string | null;
    trackInfo: TrackFileInfo;
    artists: ArtistInfo[];
}

interface TrackTableProps {
    tracks: TrackViewItem[];
    currentPage: number;
    pageSize: number;
    loading?: boolean;
    error?: string | null;
    totalPages?: number;
    deletingTrackId?: string | null;
    onPlayTrack: (trackId: string) => void;
    onDeleteTrack: (trackId: string) => Promise<void>;
    onUnlikeTrack: (trackId: string) => Promise<void>;
    onLikeTrack: (trackId: string) => Promise<void>;
    onPageChange?: (newPage: number) => void;
    showPagination?: boolean;
}

const TrackTable: React.FC<TrackTableProps> = ({
    tracks,
    currentPage,
    pageSize,
    loading = false,
    error = null,
    totalPages = 1,
    deletingTrackId = null,
    onPlayTrack,
    onDeleteTrack,
    onUnlikeTrack,
    onLikeTrack,
    onPageChange,
    showPagination = false
}) => {
    const handlePageChange = (newPage: number) => {
        if (onPageChange && newPage >= 1 && newPage <= totalPages) {
            onPageChange(newPage);
        }
    };

    if (loading) {
        return (
            <div className={styles.loading}>Загрузка треков...</div>
        );
    }

    if (error) {
        return (
            <div className={styles.error}>
                <p>{error}</p>
                <button className={styles.retryButton} onClick={() => onPageChange?.(currentPage)}>
                    Попробовать снова
                </button>
            </div>
        );
    }

    return (
        <>
            <table className={styles.trackList}>
                <thead>
                    <tr>
                        <th className={styles.trackHeader}>#</th>
                        <th className={styles.trackHeader}>Название</th>
                        <th className={styles.trackHeader}>Альбом</th>
                        <th className={styles.trackHeader}>Длительность</th>
                        <th className={styles.trackHeader}>Действия</th>
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
                                        onClick={() => onPlayTrack(track.id)}
                                        title="Воспроизвести"
                                    >
                                        <i className="fas fa-play"></i>
                                    </button>
                                    <button 
                                        className={styles.actionButton} 
                                        title="Добавить в избранное"
                                        onClick={() => track.isLiked ? onUnlikeTrack(track.id) : onLikeTrack(track.id)}
                                    >
                                        {!track.isLiked && <i className="far fa-heart"></i>}
                                        {track.isLiked && <i className="fas fa-heart"></i>}
                                    </button>
                                    <button
                                        className={styles.actionButton}
                                        onClick={() => onDeleteTrack(track.id)}
                                        disabled={deletingTrackId === track.id}
                                        title="Удалить из плейлиста"
                                    >
                                        {deletingTrackId === track.id ? (
                                            <i className="fas fa-spinner fa-spin"></i>
                                        ) : (
                                            <i className="fas fa-trash"></i>
                                        )}
                                    </button>
                                </div>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>

            {/* Пагинация */}
            {showPagination && totalPages > 1 && (
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
    );
};

export default TrackTable;