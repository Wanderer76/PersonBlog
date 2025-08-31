// components/PlayerControls.tsx
import React from 'react';
import {
  Box,
  Slider,
  Typography,
  IconButton,
  Paper,
  CircularProgress,
  Tooltip
} from '@mui/material';
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
  isLoading?: boolean;
  onPlay: () => void;
  onPause: () => void;
  onNext: () => void;
  onPrevious: () => void;
  onSeek: (time: number) => void;
  onVolumeChange: (volume: number) => void;
  onToggleLike: (track: TrackViewItem) => void; // Добавлено
}

const formatTime = (seconds: number): string => {
  if (isNaN(seconds)) return '0:00';
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
};

const PlayerControls: React.FC<PlayerControlsProps> = ({
  currentTrack,
  isPlaying,
  currentTime,
  duration,
  volume,
  isLoading = false,
  onPlay,
  onPause,
  onNext,
  onPrevious,
  onSeek,
  onVolumeChange,
  onToggleLike
}) => {
  if (!currentTrack) return null;

  const handleSeek = (event: Event, value: number | number[]) => {
    onSeek(value as number);
  };

  const handleVolumeChange = (event: Event, value: number | number[]) => {
    onVolumeChange(value as number);
  };

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
          <div className={styles.trackTitleContainer}>
            <Tooltip title={currentTrack.name} placement="top">
              <Typography
                component="h3"
                className={`${styles.trackName} ${isLoading ? styles.loading : ''}`}
              >
                {currentTrack.name}
              </Typography>
            </Tooltip>

            {/* Кнопка "Добавить в избранное" */}
            <Tooltip title={currentTrack.isLiked ? 'Удалить из избранного' : 'Добавить в избранное'}>
              <IconButton
                onClick={() => onToggleLike(currentTrack)}
                size="small"
                disabled={isLoading}
                aria-label={currentTrack.isLiked ? 'Удалить из избранного' : 'Добавить в избранное'}
                sx={{
                  color: currentTrack.isLiked ? '#ff7b00' : '#999',
                  '&:hover': {
                    color: '#ff7b00',
                  },
                }}
              >
                <i
                  className={currentTrack.isLiked ? 'fas fa-heart' : 'far fa-heart'}
                  style={{ fontSize: '16px' }}
                />
              </IconButton>
            </Tooltip>
          </div>

          <Tooltip title={currentTrack.artists.map(a => a.name).join(', ')} placement="top">
            <Typography
              component="p"
              className={styles.artistName}
            >
              {currentTrack.artists.map((a) => a.name).join(', ')}
            </Typography>
          </Tooltip>
        </div>

        {/* Основные кнопки и прогресс */}
        <div className={styles.playbackControls}>
          <div className={styles.playbackButtons}>
            <Tooltip title="Предыдущий трек">
              <IconButton
                onClick={onPrevious}
                size="large"
                aria-label="Предыдущий трек"
                disabled={isLoading}
              >
                <SkipPreviousIcon />
              </IconButton>
            </Tooltip>

            <Tooltip title={isPlaying ? 'Пауза' : 'Воспроизвести'}>
              <IconButton
                onClick={isPlaying ? onPause : onPlay}
                size="large"
                className={styles.playButton}
                aria-label={isPlaying ? 'Пауза' : 'Воспроизвести'}
                disabled={isLoading}
              >
                {isLoading ? (
                  <CircularProgress size={24} sx={{ color: 'white' }} />
                ) : isPlaying ? (
                  <PauseIcon />
                ) : (
                  <PlayArrowIcon />
                )}
              </IconButton>
            </Tooltip>

            <Tooltip title="Следующий трек">
              <IconButton
                onClick={onNext}
                size="large"
                aria-label="Следующий трек"
                disabled={isLoading}
              >
                <SkipNextIcon />
              </IconButton>
            </Tooltip>
          </div>

          {/* Прогресс-бар времени */}
          <div className={styles.sliderContainer}>
            <Typography variant="caption" className={styles.timeLabel}>
              {formatTime(currentTime)}
            </Typography>
            <Slider
              value={currentTime}
              max={duration > 0 ? duration : 100}
              onChange={handleSeek}
              disabled={isLoading}
              className={styles.progressSlider}
              sx={{
                color: '#ff7b00',
                '& .MuiSlider-thumb': {
                  backgroundColor: '#ff7b00',
                  '&:hover, &.Mui-focusVisible': {
                    boxShadow: '0px 0px 0px 8px rgba(255, 123, 0, 0.16)',
                  },
                },
                '& .MuiSlider-rail': {
                  backgroundColor: '#e0e0e0',
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
          <VolumeUpIcon fontSize="small" sx={{ color: '#666' }} />
          <Slider
            value={volume}
            max={100}
            onChange={handleVolumeChange}
            className={styles.volumeSlider}
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