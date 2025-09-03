import { useState, useRef, useCallback, useEffect } from 'react';
import { type TrackViewItem, type AudioPlayerState } from '../types/music';
import { musicApi } from '../services/api';

export enum Repeat {
    off,
    one,
    all
}

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

    const [playlist, setPlaylist] = useState<TrackViewItem[]>([]);
    const [currentIndex, setCurrentIndex] = useState<number>(-1);
    const [shuffle, setShuffle] = useState<boolean>(false);
    const [repeat, setRepeat] = useState<Repeat>(Repeat.off);


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


    const updateCurrentTrack = useCallback((track: TrackViewItem) => {

        if (currentTrack?.id != track.id) return;
        setCurrentTrack(track);
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


    const loadPlaylist = useCallback((tracks: TrackViewItem[], startIndex: number | null) => {
        setPlaylist(tracks);
        if (startIndex != null) {
            console.log(startIndex)
            setCurrentIndex(startIndex);
            if (tracks.length > 0) {
                loadAndPlayTrack(tracks[startIndex]);
            }
        }
    }, [loadAndPlayTrack]);


    const nextTrack = useCallback(() => {
        if (playlist.length === 0) return;

        let nextIndex;
        if (shuffle) {
            // Случайный трек, исключая текущий
            do {
                nextIndex = Math.floor(Math.random() * playlist.length);
            } while (nextIndex === currentIndex && playlist.length > 1);
        } else {
            nextIndex = (currentIndex + 1) % playlist.length;
        }

        setCurrentIndex(nextIndex);
        loadAndPlayTrack(playlist[nextIndex]);
    }, [playlist, currentIndex, shuffle, loadAndPlayTrack]);


    const previousTrack = useCallback(() => {
        if (playlist.length === 0) return;

        let prevIndex;
        if (shuffle) {
            // Случайный трек
            prevIndex = Math.floor(Math.random() * playlist.length);
        } else {
            prevIndex = currentIndex === 0 ? playlist.length - 1 : currentIndex - 1;
        }

        setCurrentIndex(prevIndex);
        loadAndPlayTrack(playlist[prevIndex]);
    }, [playlist, currentIndex, shuffle, loadAndPlayTrack]);

    const toggleShuffle = useCallback(() => {
        setShuffle(prev => !prev);
    }, []);

    // Переключение repeat
    const toggleRepeat = useCallback(() => {
        setRepeat(prev => {
            if (prev === Repeat.off) return Repeat.all;
            if (prev === Repeat.all) return Repeat.one;
            return Repeat.off;
        });
    }, []);
    // Обработка окончания трека
    useEffect(() => {
        const audio = audioRef.current;
        if (!audio) return;

        const handleEnded = () => {
            if (repeat === Repeat.one) {
                // Повтор текущего трека
                audio.currentTime = 0;
                audio.play();
            } else if (repeat === Repeat.all || shuffle) {
                // Следующий трек
                nextTrack();
            } else if (currentIndex < playlist.length - 1) {
                // Следующий трек если не последний
                nextTrack();
            } else {
                // Остановка
                setState(prev => ({ ...prev, isPlaying: false, currentTime: 0 }));
            }
        };

        audio.addEventListener('ended', handleEnded);
        return () => audio.removeEventListener('ended', handleEnded);
    }, [repeat, shuffle, currentIndex, playlist.length, nextTrack]);
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

        playlist,
        currentIndex,
        shuffle,
        repeat,

        // Методы
        loadAndPlayTrack,
        updateCurrentTrack,
        play,
        pause,
        seek,
        setVolume,

        loadPlaylist,
        nextTrack,
        previousTrack,
        toggleShuffle,
        toggleRepeat,
    };
};