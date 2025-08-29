import { useState, useRef, useCallback, useEffect } from 'react';
import { type TrackViewItem, type AudioPlayerState } from '../types/music';
import { musicApi } from '../services/api';

export const useAudioPlayer = () => {
    const audioRef = useRef<HTMLAudioElement | null>(null);
    const [state, setState] = useState<AudioPlayerState>({
        isPlaying: false,
        currentTime: 0,
        duration: 0,
        volume: 50,
        isLoading: false,
    });
    const [currentTrack, setCurrentTrack] = useState<TrackViewItem>();

    // Инициализация аудио элемента
    useEffect(() => {
        audioRef.current = new Audio();
        audioRef.current.preload = 'metadata';

        const audio = audioRef.current;

        const handleLoadedMetadata = () => {
            setState(prev => ({
                ...prev,
                duration: audio.duration || 0,
                isLoading: false,
            }));
        };

        const handleTimeUpdate = () => {
            setState(prev => ({ ...prev, currentTime: audio.currentTime }));
        };

        const handleEnded = () => {
            setState(prev => ({ ...prev, isPlaying: false, currentTime: 0 }));
        };

        const handlePlay = () => setState(prev => ({ ...prev, isPlaying: true }));
        const handlePause = () => setState(prev => ({ ...prev, isPlaying: false }));
        const handleWaiting = () => setState(prev => ({ ...prev, isLoading: true }));
        const handleCanPlay = () => setState(prev => ({ ...prev, isLoading: false }));

        audio.addEventListener('loadedmetadata', handleLoadedMetadata);
        audio.addEventListener('timeupdate', handleTimeUpdate);
        audio.addEventListener('ended', handleEnded);
        audio.addEventListener('play', handlePlay);
        audio.addEventListener('pause', handlePause);
        audio.addEventListener('waiting', handleWaiting);
        audio.addEventListener('canplay', handleCanPlay);

        return () => {
            audio.removeEventListener('loadedmetadata', handleLoadedMetadata);
            audio.removeEventListener('timeupdate', handleTimeUpdate);
            audio.removeEventListener('ended', handleEnded);
            audio.removeEventListener('play', handlePlay);
            audio.removeEventListener('pause', handlePause);
            audio.removeEventListener('waiting', handleWaiting);
            audio.removeEventListener('canplay', handleCanPlay);
            audio.pause();
        };
    }, []);

    // Загрузка и воспроизведение трека
     const loadAndPlayTrack = useCallback(async (track: TrackViewItem) => {
        if (!audioRef.current) return;

        setState(prev => ({ ...prev, isLoading: true, currentTime: 0 }));

        try {
            // Останавливаем текущее воспроизведение
            audioRef.current.pause();
            audioRef.current.currentTime = 0;

            // Получаем presigned URL с бэкенда
            const presignedUrl = await musicApi.getTrackPresignedUrl(track.id);


            // Сбрасываем предыдущие обработчики
            audioRef.current.oncanplaythrough = null;
            
            audioRef.current.src = presignedUrl;
            audioRef.current.load();

            setCurrentTrack(track);

            // Ждем загрузки метаданных
            await new Promise<void>((resolve) => {
                const handleCanPlay = () => {
                    audioRef.current?.removeEventListener('canplay', handleCanPlay);
                    resolve();
                };
                audioRef.current?.addEventListener('canplay', handleCanPlay);
            });

            // Начинаем воспроизведение
            await audioRef.current.play();
            
        } catch (error) {
            console.error('Error loading track:', error);
            setState(prev => ({
                ...prev,
                isLoading: false,
                error: 'Ошибка загрузки трека'
            }));
        }
    }, []);

    // Управление воспроизведением
    const play = useCallback(async () => {
        if (!audioRef.current) return;

        if (!currentTrack) return;

        if (audioRef.current.src) {
            await audioRef.current.play();
        } else {
            await loadAndPlayTrack(currentTrack);
        }
    }, [currentTrack, loadAndPlayTrack]);

    const pause = useCallback(() => {
        audioRef.current?.pause();
    }, []);

    const seek = useCallback((time: number) => {
        if (audioRef.current) {
            audioRef.current.currentTime = time;
            setState(prev => ({ ...prev, currentTime: time }));
        }
    }, []);

    const setVolume = useCallback((volume: number) => {
        if (audioRef.current) {
            audioRef.current.volume = volume / 100;
            setState(prev => ({ ...prev, volume }));
        }
    }, []);

    return {
        // Состояние
        currentTrack,
        isPlaying: state.isPlaying,
        currentTime: state.currentTime,
        duration: state.duration,
        volume: state.volume,
        isLoading: state.isLoading,
        error: state.error,

        // Методы
        loadAndPlayTrack,
        play,
        pause,
        seek,
        setVolume,
    };
};