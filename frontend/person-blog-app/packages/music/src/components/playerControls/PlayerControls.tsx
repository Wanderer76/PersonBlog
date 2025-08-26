// components/PlayerControls.tsx
import React from 'react';
import { Box, Slider, Typography, IconButton, Paper } from '@mui/material';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import PauseIcon from '@mui/icons-material/Pause';
import SkipNextIcon from '@mui/icons-material/SkipNext';
import SkipPreviousIcon from '@mui/icons-material/SkipPrevious';
import VolumeUpIcon from '@mui/icons-material/VolumeUp';
import type { TrackViewItem } from '../../types/music';
import styles from './PlayerControls.module.css';

interface PlayerControlsProps {
  currentTrack?: TrackViewItem;
  isPlaying: boolean;
  currentTime: number;
  duration: number;
  volume: number;
  onPlay: () => void;
  onPause: () => void;
  onNext: () => void;
  onPrevious: () => void;
  onSeek: (time: number) => void;
  onVolumeChange: (volume: number) => void;
}

const formatTime = (seconds: number): string => {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toFixed(0)}`;
};

const PlayerControls: React.FC<PlayerControlsProps> = ({
  currentTrack,
  isPlaying,
  currentTime,
  duration,
  volume,
  onPlay,
  onPause,
  onNext,
  onPrevious,
  onSeek,
  onVolumeChange,
}) => {
  if (!currentTrack) return null;

  return (
    <Paper
      elevation={3}
      className={styles.player}
      role="region"
      aria-label="Аудио плеер"
    >
      <Box className={styles.controlsContainer}>
        {/* Информация о треке */}
        <div className={styles.trackInfo}>
          <Typography component="h3" className={styles.trackName} title={currentTrack.name}>
            {currentTrack.name}
          </Typography>
          <Typography component="p" className={styles.artistName} title={currentTrack.artists.map(a => a.name).join(', ')}>
            {currentTrack.artists.map((a) => a.name).join(', ')}
          </Typography>
        </div>

        {/* Основные кнопки и прогресс */}
        <div className={styles.playbackControls}>
          <div className={styles.playbackButtons}>
            <IconButton onClick={onPrevious} size="large" aria-label="Предыдущий трек">
              <SkipPreviousIcon />
            </IconButton>
            <IconButton
              onClick={isPlaying ? onPause : onPlay}
              size="large"
              className={styles.playButton}
              aria-label={isPlaying ? 'Пауза' : 'Воспроизвести'}
            >
              {isPlaying ? <PauseIcon /> : <PlayArrowIcon />}
            </IconButton>
            <IconButton onClick={onNext} size="large" aria-label="Следующий трек">
              <SkipNextIcon />
            </IconButton>
          </div>

          {/* Прогресс-бар времени */}
          <div className={styles.sliderContainer}>
            <Typography variant="caption" className={styles.timeLabel}>
              {formatTime(currentTime)}
            </Typography>
            <Slider
              value={currentTime}
              max={duration || 100}
              onChange={(_, value) => onSeek(value as number)}
              className="progress-slider"
              sx={{
                color: '#ff7b00',
                '& .MuiSlider-thumb': {
                  backgroundColor: '#ff7b00',
                  '&:hover, &.Mui-focusVisible': {
                    boxShadow: '0px 0px 0px 8px rgba(255, 123, 0, 0.16)',
                  },
                },
              }}
            />
            <Typography variant="caption" className={styles.timeLabel}>
              {formatTime(duration)}
            </Typography>
          </div>
        </div>

        {/* Регулятор громкости */}
        <div className={styles.volumeContainer}>
          <VolumeUpIcon fontSize="small" />
          <Slider
            value={volume}
            max={100}
            onChange={(_, value) => onVolumeChange(value as number)}
            className="volume-slider"
            sx={{
              color: '#ff7b00',
              '& .MuiSlider-thumb': {
                backgroundColor: '#ff7b00',
                '&:hover, &.Mui-focusVisible': {
                  boxShadow: '0px 0px 0px 8px rgba(255, 123, 0, 0.16)',
                },
              },
            }}
          />
        </div>
      </Box>
    </Paper>
  );
};

export default PlayerControls;