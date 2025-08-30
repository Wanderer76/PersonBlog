// components/Layout.tsx
import React from 'react';
import { Box } from '@mui/material';
import PlayerControls from '../playerControls/PlayerControls';
import { useAudioPlayer } from '../../hooks/useAudioPlayer';
import { useAudioPlayerContext } from '../../context/AudioPlayerContext';

interface LayoutProps {
  children: React.ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
  const audioPlayer = useAudioPlayerContext(); // Используем контекст вместо хука

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      {/* Основное содержимое */}
      <Box sx={{ flex: 1, pb: 10 }}> {/* Отступ снизу для плеера */}
        {children}
      </Box>

      {/* Глобальный плеер */}
      <Box
        sx={{
          position: 'fixed',
          bottom: 0,
          left: 0,
          right: 0,
          zIndex: 1000,
          backgroundColor: 'background.paper',
          borderTop: 1,
          borderColor: 'divider'
        }}
      >
        <PlayerControls
          currentTrack={audioPlayer.currentTrack}
          isPlaying={audioPlayer.isPlaying}
          currentTime={audioPlayer.currentTime}
          duration={audioPlayer.duration}
          volume={audioPlayer.volume}
          isLoading={audioPlayer.isLoading}
          onPlay={audioPlayer.play}
          onPause={audioPlayer.pause}
          onNext={() => {/* Implement next track logic */}}
          onPrevious={() => {/* Implement previous track logic */}}
          onSeek={audioPlayer.seek}
          onVolumeChange={audioPlayer.setVolume}
        />
      </Box>
    </Box>
  );
};

export default Layout;