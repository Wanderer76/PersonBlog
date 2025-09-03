// pages/HomePage.tsx
import React, { useState, useEffect, useCallback, useRef } from 'react';
import {
  Container,
  Box,
  CircularProgress,
  TextField,
  InputAdornment,
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import TrackCardList from '../components/trackCardList/TrackCardList';
import type { TrackViewItem } from '../types/music';
import { musicApi } from '../services/api';
import { useAudioPlayerContext } from '../context/AudioPlayerContext';

const HomePage: React.FC = () => {
  const [tracks, setTracks] = useState<TrackViewItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [initialLoad, setInitialLoad] = useState(true);

  const observer = useRef<IntersectionObserver>(null);
  const loadMoreRef = useRef<HTMLDivElement>(null);

  const audioPlayer = useAudioPlayerContext();

  useEffect(() => {
    if (initialLoad) {
      fetchTracks(1);
      setInitialLoad(false);
    }
  }, [initialLoad]);

  // Наблюдатель для бесконечной прокрутки
  useEffect(() => {
    if (loadingMore || !hasMore || searchQuery) return;

    const observerCallback = (entries: IntersectionObserverEntry[]) => {
      if (entries[0].isIntersecting && hasMore) {
        loadNextPage();
      }
    };

    if (loadMoreRef.current) {
      observer.current = new IntersectionObserver(observerCallback, {
        root: null,
        rootMargin: '100px',
        threshold: 0.1,
      });

      observer.current.observe(loadMoreRef.current);
    }

    return () => {
      if (observer.current) {
        observer.current.disconnect();
      }
    };
  }, [loadingMore, hasMore, searchQuery]);

  const fetchTracks = async (page: number = 1) => {
    try {
      if (page === 1) {
        setLoading(true);
      } else {
        setLoadingMore(true);
      }

      const response = await musicApi.getTracks(page, 5);
      
      if (page === 1) {
        setTracks(response.items);
      } else {
        setTracks(prev => [...prev, ...response.items]);
      }

      setCurrentPage(page);
      setHasMore(page < response.totalPageCount);
    } catch (error) {
      console.error('Error fetching tracks:', error);
    } finally {
      setLoading(false);
      setLoadingMore(false);
    }
  };

  const loadNextPage = useCallback(() => {
    if (!loadingMore && hasMore) {
      fetchTracks(currentPage + 1);
    }
  }, [currentPage, loadingMore, hasMore]);

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

  const handleSearchChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setSearchQuery(event.target.value);
  };

  const handleSearchSubmit = () => {
    // При поиске сбрасываем состояние и загружаем первую страницу
    setCurrentPage(1);
    setHasMore(true);
    fetchTracks(1);
  };

  const filteredTracks = searchQuery
    ? tracks.filter(track =>
        track.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        track.artists.some(artist =>
          artist.name.toLowerCase().includes(searchQuery.toLowerCase())
        )
      )
    : tracks;

  return (
    <Box sx={{ flexGrow: 1, pb: 10 }}>
      <Container maxWidth="xl" sx={{ mt: 4 }}>
        {/* Поисковая строка */}
        <Box sx={{ mb: 3 }}>
          <TextField
            fullWidth
            variant="outlined"
            placeholder="Поиск треков или исполнителей..."
            value={searchQuery}
            onChange={handleSearchChange}
            onKeyPress={(e) => {
              if (e.key === 'Enter') {
                handleSearchSubmit();
              }
            }}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon />
                </InputAdornment>
              ),
            }}
            sx={{
              '& .MuiOutlinedInput-root': {
                borderRadius: 2,
                backgroundColor: 'background.paper',
              }
            }}
          />
        </Box>

        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
            <CircularProgress sx={{ color: '#ff7b00' }} />
          </Box>
        ) : (
          <>
            <TrackCardList
              tracks={filteredTracks}
              currentPlayingTrack={audioPlayer.currentTrack}
              isPlaying={audioPlayer.isPlaying}
              onPlay={handlePlay}
              onPause={handlePause}
              title={searchQuery 
                ? `Результаты поиска: "${searchQuery}" (${filteredTracks.length} найдено)` 
                : 'Популярные треки'
              }
            />

            {/* Элемент для наблюдения за прокруткой */}
            {!searchQuery && hasMore && (
              <Box ref={loadMoreRef} sx={{ display: 'flex', justifyContent: 'center', mt: 4, py: 2 }}>
                {loadingMore && <CircularProgress sx={{ color: '#ff7b00' }} />}
              </Box>
            )}

            {/* Сообщение о конце списка */}
            {!hasMore && tracks.length > 0 && (
              <Box sx={{ textAlign: 'center', mt: 3, color: 'text.secondary' }}>
                Вы просмотрели все треки
              </Box>
            )}

            {/* Кнопка для ручной загрузки, если IntersectionObserver не сработал */}
            {!searchQuery && hasMore && !loadingMore && (
              <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
                <button
                  onClick={loadNextPage}
                  style={{
                    padding: '10px 20px',
                    backgroundColor: '#ff7b00',
                    color: 'white',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: 'pointer',
                    fontSize: '14px',
                  }}
                >
                  Загрузить еще
                </button>
              </Box>
            )}
          </>
        )}
      </Container>
    </Box>
  );
};

export default HomePage;