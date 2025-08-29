// pages/HomePage.tsx
import React, { useState, useEffect } from 'react';
import {
  Container,
  Box,
  CircularProgress,
} from '@mui/material';
import TrackList from '../components/trackList/TrackList';
import PlayerControls from '../components/playerControls/PlayerControls';
import type { TrackViewItem } from '../types/music';
import { musicApi } from '../services/api';
import { useAudioPlayer } from '../hooks/useAudioPlayer';

const HomePage: React.FC = () => {
  const [tracks, setTracks] = useState<TrackViewItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');

  const audioPlayer = useAudioPlayer();

  useEffect(() => {
    fetchTracks();
  }, []);

  const fetchTracks = async () => {
    try {
      setLoading(true);
      const response = await musicApi.getTracks();
      setTracks(response.items);
    } catch (error) {
      console.error('Error fetching tracks:', error);
    } finally {
      setLoading(false);
    }
  };

  const handlePlay = (track: TrackViewItem) => {
    if (audioPlayer.currentTrack?.id === track.id) {
      audioPlayer.play();
    } else {
      audioPlayer.loadAndPlayTrack(track);
    }
  };

  const handlePause = () => {
    audioPlayer.pause();
  };

  const filteredTracks = tracks.filter(track =>
    track.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    track.artists.some(artist =>
      artist.name.toLowerCase().includes(searchQuery.toLowerCase())
    )
  );

  return (
    <Box sx={{ flexGrow: 1, pb: 10 }}>
      <Container maxWidth="xl" sx={{ mt: 4 }}>
        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
            <CircularProgress sx={{ color: '#ff7b00' }} />
          </Box>
        ) : (
          <>
            <TrackList
              tracks={filteredTracks}
              currentPlayingTrack={audioPlayer.currentTrack}
              isPlaying={audioPlayer.isPlaying}
              onPlay={handlePlay}
              onPause={handlePause}
              title={searchQuery ? `Результаты поиска: "${searchQuery}"` : 'Популярные треки'}
            />
          </>
        )}
      </Container>

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
  );
};

export default HomePage;