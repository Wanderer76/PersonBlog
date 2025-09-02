import React from 'react';
import styles from './TrackRow.module.css';
import { formatDuration } from '../../scripts/helper';
import type { TrackViewItem } from '../../types/music';

interface TrackRowProps {
    track: TrackViewItem;
    index: number;
    currentPage: number;
    pageSize: number;
    currentPlayingTrackId?: string;
    isPlaying: boolean;
    deletingTrackId?: string | null;
    onPlayTrack: (trackId: string) => void;
    onDeleteTrack: (trackId: string) => Promise<void>;
    onUnlikeTrack: (trackId: string) => Promise<void>;
    onLikeTrack: (trackId: string) => Promise<void>;
}

const TrackRow: React.FC<TrackRowProps> = ({
    track,
    index,
    currentPage,
    pageSize,
    currentPlayingTrackId,
    isPlaying,
    deletingTrackId,
    onPlayTrack,
    onDeleteTrack,
    onUnlikeTrack,
    onLikeTrack
}) => {

    const truncateTrackName = (name: string, maxLength: number = 20) => {
        if (name.length <= maxLength) return name;
        return name.substring(0, maxLength) + '...';
    };

    return (
        <div className={`${styles.trackRow} ${track.id === currentPlayingTrackId ? styles.playing : ''}`}>
            <div className={`${styles.trackCell} ${styles.trackIndex}`}>
                {(currentPage - 1) * pageSize + index + 1}
            </div>
            <div className={styles.trackCell}>
                <div className={styles.trackInfo}>
                    <img
                        src={track.thumbnailUrl || '/default-track.png'}
                        alt={track.name}
                        className={styles.trackThumbnail}
                    />
                    <div className={styles.trackDetails}>
                        <div className={styles.trackName}>{truncateTrackName(track.name)}</div>
                        <div className={styles.trackArtists}>
                            {track.artists.map(artist => artist.name).join(', ')}
                        </div>
                    </div>
                </div>
            </div>
            <div className={styles.trackCell}>-</div>
            <div className={styles.trackCell}>
                <span className={styles.trackDuration}>
                    {formatDuration(track.trackInfo.duration)}
                </span>
            </div>
            <div className={styles.trackCell}>
                <div className={styles.trackActions}>
                    <button
                        className={styles.actionButton}
                        onClick={() => onPlayTrack(track.id)}
                        title="Воспроизвести"
                    >
                        {(currentPlayingTrackId == track.id && isPlaying) && <i className="fas fa-pause"></i>}
                        {(currentPlayingTrackId == track.id && !isPlaying) && <i className="fas fa-play"></i>}
                        {currentPlayingTrackId != track.id && <i className="fas fa-play"></i>}
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
            </div>
        </div>
    );
};

export default TrackRow;