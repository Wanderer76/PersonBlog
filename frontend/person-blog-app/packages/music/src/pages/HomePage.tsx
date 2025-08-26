// pages/HomePage.tsx
import React, { useState, useEffect } from 'react';
import {
  Container,
  AppBar,
  Toolbar,
  Typography,
  Box,
  TextField,
  InputAdornment,
  CircularProgress,
} from '@mui/material';
import { Search, MusicNote } from '@mui/icons-material';
import TrackList from '../components/trackList/TrackList';
import PlayerControls from '../components/PlayerControls';
import type { TrackViewItem, PagedListViewModel } from '../types/music';
import { musicApi } from '../services/api';

const HomePage: React.FC = () => {
  const [tracks, setTracks] = useState<TrackViewItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [currentTrack, setCurrentTrack] = useState<TrackViewItem>();
  const [isPlaying, setIsPlaying] = useState(false);
  const [currentTime, setCurrentTime] = useState(0);
  const [volume, setVolume] = useState(50);

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
    if (currentTrack?.id === track.id) {
      setIsPlaying(true);
    } else {
      setCurrentTrack(track);
      setIsPlaying(true);
      setCurrentTime(0);
    }
  };

  const handlePause = () => {
    setIsPlaying(false);
  };

  const handleSeek = (time: number) => {
    setCurrentTime(time);
  };

  const handleVolumeChange = (newVolume: number) => {
    setVolume(newVolume);
  };

  const filteredTracks = tracks.filter(track =>
    track.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    track.artists.some(artist =>
      artist.name.toLowerCase().includes(searchQuery.toLowerCase())
    )
  );

  return (
    <Box sx={{ flexGrow: 1, pb: 10 }}>
      <AppBar 
        position="static" 
        sx={{ 
          backgroundColor: 'background.paper',
          color: 'text.primary',
          boxShadow: 1,
        }}
      >
        <Toolbar>
          <MusicNote sx={{ color: '#ff7b00', mr: 2 }} />
          <Typography
            variant="h6"
            noWrap
            component="div"
            sx={{ 
              flexGrow: 1, 
              display: { xs: 'none', sm: 'block' },
              fontWeight: 'bold',
            }}
          >
            MusicStream
          </Typography>
          
          <TextField
            placeholder="Поиск треков и исполнителей..."
            variant="outlined"
            size="small"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            sx={{
              width: 300,
              '& .MuiOutlinedInput-root': {
                borderRadius: 2,
              },
            }}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <Search />
                </InputAdornment>
              ),
            }}
          />
        </Toolbar>
      </AppBar>

      <Container maxWidth="xl" sx={{ mt: 4 }}>
        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
            <CircularProgress sx={{ color: '#ff7b00' }} />
          </Box>
        ) : (
          <>
            <TrackList
              tracks={filteredTracks}
              currentPlayingTrack={currentTrack}
              onPlay={handlePlay}
              onPause={handlePause}
              title={searchQuery ? `Результаты поиска: "${searchQuery}"` : 'Популярные треки'}
            />
          </>
        )}
      </Container>

      <PlayerControls
        currentTrack={currentTrack}
        isPlaying={isPlaying}
        currentTime={currentTime}
        duration={currentTrack?.trackInfo.duration || 0}
        volume={volume}
        onPlay={() => setIsPlaying(true)}
        onPause={handlePause}
        onNext={() => {/* Implement next track logic */}}
        onPrevious={() => {/* Implement previous track logic */}}
        onSeek={handleSeek}
        onVolumeChange={handleVolumeChange}
      />
    </Box>
  );
};

export default HomePage;