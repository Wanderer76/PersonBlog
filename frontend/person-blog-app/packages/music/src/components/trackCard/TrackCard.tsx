// components/trackCard/TrackCard.tsx
import React from 'react';
import { Typography } from '@mui/material';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import PauseIcon from '@mui/icons-material/Pause';
import type { TrackViewItem } from '../../types/music';
import styles from './TrackCard.module.css';
import { formatDuration } from '../../scripts/helper';

interface TrackCardProps {
  track: TrackViewItem;
  isPlaying?: boolean;
  currentPlayingTrack?: TrackViewItem;
  onPlay: (track: TrackViewItem) => void;
  onPause: () => void;
}


const TrackCard: React.FC<TrackCardProps> = ({
  track,
  isPlaying = false,
  currentPlayingTrack = null,
  onPlay,
  onPause,
}) => {
  const handlePlayPause = () => {
    if (isPlaying && track.id == currentPlayingTrack?.id) {
      onPause();
    } else {
      onPlay(track);
    }
  };
  return (
    <div
      className={styles.card}
      role="article"
      aria-label={`Трек: ${track.name}, исполнитель: ${track.artists.map(a => a.name).join(', ')}`}
    >
      {/* Обложка с фиксированным соотношением сторон */}
      <div className={styles.imageContainer}>
        <img
          src={track.thumbnailUrl ?? undefined}
          alt={track.name}
          className={styles.image}
        />
        <button
          className={`${styles.playButton} ${isPlaying ? styles.playing : ''}`}
          onClick={handlePlayPause}
          aria-label={isPlaying ? 'Пауза' : 'Воспроизвести'}
        >
          {isPlaying && currentPlayingTrack?.id == track.id ? (
            <PauseIcon className={styles.playIcon} />
          ) : (
            <PlayArrowIcon className={styles.playIcon} />
          )}
        </button>

        {/* Индикатор воспроизведения */}
        {isPlaying && currentPlayingTrack?.id == track.id && (
          <div className={styles.playingIndicator}>
            {[1, 2, 3, 4, 5].map((i) => (
              <div key={i} className={styles.bar} />
            ))}
          </div>
        )}
      </div>

      {/* Информация */}
      <div className={styles.content}>
        <Typography
          component="h3"
          className={styles.trackName}
          title={track.name}
        >
          {track.name.length > 20 ? `${track.name.substring(0, 20)}...` : track.name}
        </Typography>
        <Typography
          component="p"
          className={styles.artists}
          title={track.artists.map((a) => a.name).join(', ')}
        >
          {track.artists.map((a) => a.name).join(', ')}
        </Typography>
        <Typography
          component="span"
          className={styles.duration}
        >
          {formatDuration(track.trackInfo.duration)}
        </Typography>
      </div>
    </div>
  );
};

export default TrackCard;