// components/trackCard/TrackCard.tsx
import React from 'react';
import { Box, Typography } from '@mui/material';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import PauseIcon from '@mui/icons-material/Pause';
import type { TrackViewItem } from '../../types/music';
import styles from './TrackCard.module.css';

interface TrackCardProps {
  track: TrackViewItem;
  isPlaying?: boolean;
  onPlay: (track: TrackViewItem) => void;
  onPause: () => void;
}

const formatDuration = (seconds: number): string => {
  const mins = Math.floor(seconds / 60);
  const secs = seconds % 60;
  return `${mins}:${secs.toString().padStart(2, '0')}`;
};

const TrackCard: React.FC<TrackCardProps> = ({
  track,
  isPlaying = false,
  onPlay,
  onPause,
}) => {
  const handlePlayPause = () => {
    if (isPlaying) {
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
      {/* Обложка */}
      <div className={styles.mediaContainer}>
        <img
          src={track.thumbnailUrl || '/images/default-thumbnail.jpg'}
          alt={track.name}
          style={{
            width: '100%',
            height: '100%',
            objectFit: 'cover',
            borderRadius: '12px 12px 0 0',
          }}
        />
        <button
          className={styles.playButton}
          onClick={handlePlayPause}
          aria-label={isPlaying ? 'Пауза' : 'Воспроизвести'}
        >
          {isPlaying ? (
            <PauseIcon fontSize="small" sx={{ fontSize: 20 }} />
          ) : (
            <PlayArrowIcon fontSize="small" sx={{ fontSize: 20 }} />
          )}
        </button>
      </div>

      {/* Информация */}
      <div className={styles.content}>
        <Typography
          component="h3"
          className={styles.trackName}
          title={track.name}
        >
          {track.name}
        </Typography>
        <Typography
          component="p"
          className={styles.artistNames}
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