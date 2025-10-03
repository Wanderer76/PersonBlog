import React from 'react';
import styles from './TrackTable.module.css';
import TrackRow from './TrackRow';
import type { TrackViewItem } from '../../types/music';

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
    currentPlayingTrackId?: string;
    isPlaying: boolean;
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
    showPagination = false,
    currentPlayingTrackId,
    isPlaying = false
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
            {/* Заголовки как div */}
            <div className={styles.trackList}>
                <div className={styles.trackHeaderRow}>
                    <div className={styles.trackHeader}>#</div>
                    <div className={styles.trackHeader}>Название</div>
                    <div className={styles.trackHeader}>Альбом</div>
                    <div className={styles.trackHeader}>Длительность</div>
                    <div className={styles.trackHeader}>Действия</div>
                </div>
                
                {/* Строки с треками */}
                <div className={styles.trackRows}>
                    {tracks.map((track, index) => (
                        <TrackRow
                            key={track.id}
                            track={track}
                            index={index}
                            currentPage={currentPage}
                            pageSize={pageSize}
                            currentPlayingTrackId={currentPlayingTrackId}
                            isPlaying={isPlaying}
                            deletingTrackId={deletingTrackId}
                            onPlayTrack={onPlayTrack}
                            onDeleteTrack={onDeleteTrack}
                            onUnlikeTrack={onUnlikeTrack}
                            onLikeTrack={onLikeTrack}
                        />
                    ))}
                </div>
            </div>

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