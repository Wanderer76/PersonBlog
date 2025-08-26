// components/PlayerControls.tsx
import React from 'react';
import {
  Box,
  Slider,
  Typography,
  IconButton,
  Paper,
} from '@mui/material';
import {
  PlayArrow,
  Pause,
  SkipNext,
  SkipPrevious,
  VolumeUp,
} from '@mui/icons-material';
import type { TrackViewItem } from '../types/music';

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
  const formatTime = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  if (!currentTrack) {
    return null;
  }

  return (
    <Paper
      elevation={3}
      sx={{
        position: 'fixed',
        bottom: 0,
        left: 0,
        right: 0,
        p: 2,
        backgroundColor: 'background.paper',
        borderTop: '1px solid',
        borderColor: 'divider',
      }}
    >
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
        <Box sx={{ minWidth: 200 }}>
          <Typography variant="subtitle1" noWrap fontWeight="bold">
            {currentTrack.name}
          </Typography>
          <Typography variant="body2" color="text.secondary" noWrap>
            {currentTrack.artists.map(artist => artist.name).join(', ')}
          </Typography>
        </Box>

        <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', gap: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <IconButton onClick={onPrevious} size="large">
              <SkipPrevious />
            </IconButton>
            <IconButton
              onClick={isPlaying ? onPause : onPlay}
              size="large"
              sx={{
                backgroundColor: '#ff7b00',
                color: 'white',
                '&:hover': {
                  backgroundColor: '#e66a00',
                },
              }}
            >
              {isPlaying ? <Pause /> : <PlayArrow />}
            </IconButton>
            <IconButton onClick={onNext} size="large">
              <SkipNext />
            </IconButton>
          </Box>

          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Typography variant="caption" sx={{ minWidth: 40 }}>
              {formatTime(currentTime)}
            </Typography>
            <Slider
              value={currentTime}
              max={duration}
              onChange={(_, value) => onSeek(value as number)}
              sx={{
                color: '#ff7b00',
                '& .MuiSlider-thumb': {
                  '&:hover, &.Mui-focusVisible': {
                    boxShadow: '0px 0px 0px 8px rgba(255, 123, 0, 0.16)',
                  },
                },
              }}
            />
            <Typography variant="caption" sx={{ minWidth: 40 }}>
              {formatTime(duration)}
            </Typography>
          </Box>
        </Box>

        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, minWidth: 120 }}>
          <VolumeUp />
          <Slider
            value={volume}
            max={100}
            onChange={(_, value) => onVolumeChange(value as number)}
            sx={{
              color: '#ff7b00',
              '& .MuiSlider-thumb': {
                '&:hover, &.Mui-focusVisible': {
                  boxShadow: '0px 0px 0px 8px rgba(255, 123, 0, 0.16)',
                },
              },
            }}
          />
        </Box>
      </Box>
    </Paper>
  );
};

export default PlayerControls;