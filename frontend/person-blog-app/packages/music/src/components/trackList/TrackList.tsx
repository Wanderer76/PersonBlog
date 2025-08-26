// components/TrackList.tsx
import React from 'react';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import TrackCard from '../trackCard/TrackCard';
import type { TrackViewItem } from '../../types/music';
import styles from './TrackList.module.css';

interface TrackListProps {
  tracks: TrackViewItem[];
  currentPlayingTrack?: TrackViewItem;
  isPlaying?: boolean;
  onPlay: (track: TrackViewItem) => void;
  onPause: () => void;
  title?: string;
}

const TrackList: React.FC<TrackListProps> = ({
  tracks,
  isPlaying,
  currentPlayingTrack,
  onPlay,
  onPause,
  title = 'Популярные треки',
}) => {
  if (tracks.length === 0) {
    return (
      <Box className={styles.container}>
        <Typography
          variant="h4"
          component="h2"
          className={styles.title}
        >
          {title}
        </Typography>
        <Typography color="text.secondary" sx={{ mt: 2 }}>
          Треки не найдены.
        </Typography>
      </Box>
    );
  }

  return (
    <Box className={styles.container}>
      <Typography
        variant="h4"
        component="h2"
        className={styles.title}
      >
        {title}
      </Typography>

      <div className={styles.grid}>
        {tracks.map((track) => (
          <div key={track.id} className={styles.cardWrapper}>
            <TrackCard
              track={track}
              isPlaying={isPlaying}
              currentPlayingTrack={currentPlayingTrack}
              onPlay={onPlay}
              onPause={onPause}
            />
          </div>
        ))}
      </div>
    </Box>
  );
};

export default TrackList;