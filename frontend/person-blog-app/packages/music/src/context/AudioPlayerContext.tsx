// context/AudioPlayerContext.tsx
import React, { createContext, useContext, type ReactNode } from 'react';
import { useAudioPlayer } from '../hooks/useAudioPlayer';

const AudioPlayerContext = createContext<ReturnType<typeof useAudioPlayer> | null>(null);

export const AudioPlayerProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const audioPlayer = useAudioPlayer();

  return (
    <AudioPlayerContext.Provider value={audioPlayer}>
      {children}
    </AudioPlayerContext.Provider>
  );
};

export const useAudioPlayerContext = () => {
  const context = useContext(AudioPlayerContext);
  if (!context) {
    throw new Error('useAudioPlayerContext must be used within AudioPlayerProvider');
  }
  return context;
};