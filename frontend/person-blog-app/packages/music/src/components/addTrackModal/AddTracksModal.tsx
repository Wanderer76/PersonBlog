import React, { useState, useEffect } from 'react';
import styles from './AddTracksModal.module.css';
import API from '../../scripts/apiMethod';
import type { PagedListViewModel, TrackViewItem } from '../../types/music';

interface AddTracksModalProps {
    isOpen: boolean;
    onClose: () => void;
    onAddTrack: (trackId: string) => Promise<void>; // Изменено на onAddTrack для одного трека
    playlistId: string;
    existingTrackIds: string[];
}

const AddTracksModal: React.FC<AddTracksModalProps> = ({
    isOpen,
    onClose,
    onAddTrack, // Принимаем функцию для добавления одного трека
    playlistId,
    existingTrackIds
}) => {
    const [searchQuery, setSearchQuery] = useState('');
    const [tracks, setTracks] = useState<TrackViewItem[]>([]);
    const [loading, setLoading] = useState(false);
    const [searchLoading, setSearchLoading] = useState(false);
    const [addingTrackId, setAddingTrackId] = useState<string | null>(null); // Для отслеживания добавляемого трека

    // Загрузка всех треков для выбора
    useEffect(() => {
        if (isOpen) {
            fetchAllTracks();
        }
    }, [isOpen]);

    const fetchAllTracks = async () => {
        try {
            setLoading(true);
            const response = await API.get<PagedListViewModel<TrackViewItem>>('TrackSearch/filtered');
            if (response.status === 200) {
                // Фильтруем треки, которые уже есть в плейлисте
                const filteredTracks = response.data.items.filter(
                    track => !existingTrackIds.includes(track.id)
                );
                setTracks(filteredTracks);
            }
        } catch (err) {
            console.error('Ошибка при загрузке треков:', err);
        } finally {
            setLoading(false);
        }
    };

    const handleSearch = async () => {
        if (!searchQuery.trim()) {
            fetchAllTracks();
            return;
        }

        try {
            setSearchLoading(true);
            const response = await API.get<PagedListViewModel<TrackViewItem>>(
                `TrackSearch/filtered?Title=${encodeURIComponent(searchQuery)}`
            );
            if (response.status === 200) {
                const filteredTracks = response.data.items.filter(
                    track => !existingTrackIds.includes(track.id)
                );
                setTracks(filteredTracks);
            }
        } catch (err) {
            console.error('Ошибка при поиске треков:', err);
        } finally {
            setSearchLoading(false);
        }
    };

    const handleAddTrack = async (trackId: string) => {
        try {
            setAddingTrackId(trackId); // Устанавливаем ID добавляемого трека
            await onAddTrack(trackId); // Вызываем функцию добавления одного трека
            
            // Удаляем добавленный трек из списка доступных
            setTracks(prevTracks => prevTracks.filter(track => track.id !== trackId));
            
            // Можно закрыть модальное окно после успешного добавления
            // или оставить открытым для добавления других треков
            // onClose(); // Раскомментируйте, если нужно закрывать после добавления
            
        } catch (err) {
            console.error('Ошибка при добавлении трека:', err);
        } finally {
            setAddingTrackId(null); // Сбрасываем ID добавляемого трека
        }
    };

    if (!isOpen) return null;

    return (
        <div className={styles.modalOverlay} onClick={onClose}>
            <div className={styles.modalContent} onClick={e => e.stopPropagation()}>
                <div className={styles.modalHeader}>
                    <h2>Добавить трек в плейлист</h2>
                    <button className={styles.closeButton} onClick={onClose}>
                        <i className="fas fa-times"></i>
                    </button>
                </div>

                <div className={styles.searchSection}>
                    <div className={styles.searchInput}>
                        <input
                            type="text"
                            placeholder="Поиск треков..."
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
                        />
                        <button
                            className={styles.searchButton}
                            onClick={handleSearch}
                            disabled={searchLoading}
                        >
                            {searchLoading ? (
                                <i className="fas fa-spinner fa-spin"></i>
                            ) : (
                                <i className="fas fa-search"></i>
                            )}
                        </button>
                    </div>
                </div>

                <div className={styles.tracksSection}>
                    {loading ? (
                        <div className={styles.loading}>Загрузка треков...</div>
                    ) : (
                        <>
                            <div className={styles.tracksHeader}>
                                <span>{tracks.length} треков найдено</span>
                            </div>

                            <div className={styles.tracksGrid}>
                                {tracks.map(track => (
                                    <div
                                        key={track.id}
                                        className={styles.trackCard}
                                    >
                                        <img
                                            src={track.thumbnailUrl || '/default-track.png'}
                                            alt={track.name}
                                            className={styles.trackImage}
                                        />
                                        <div className={styles.trackInfo}>
                                            <div className={styles.trackName} title={track.name}>
                                                {track.name.length > 20
                                                    ? `${track.name.substring(0, 20)}...`
                                                    : track.name
                                                }
                                            </div>
                                            <div className={styles.trackArtists}>
                                                {track.artists.map(a => a.name).join(', ')}
                                            </div>
                                            <div className={styles.trackDuration}>
                                                {Math.floor(track.trackInfo.duration / 1000 / 60)}:
                                                {Math.floor((track.trackInfo.duration / 1000) % 60)
                                                    .toString()
                                                    .padStart(2, '0')}
                                            </div>
                                        </div>
                                        <button
                                            className={styles.addTrackButton}
                                            onClick={() => handleAddTrack(track.id)}
                                            disabled={addingTrackId === track.id}
                                            title="Добавить трек"
                                        >
                                            {addingTrackId === track.id ? (
                                                <i className="fas fa-spinner fa-spin"></i>
                                            ) : (
                                                <i className="fas fa-plus"></i>
                                            )}
                                        </button>
                                    </div>
                                ))}
                            </div>

                            {tracks.length === 0 && !loading && (
                                <div className={styles.noTracks}>
                                    <i className="fas fa-music" style={{fontSize: '3rem', marginBottom: '1rem', color: '#ccc'}}></i>
                                    <p>Нет доступных треков для добавления</p>
                                    <p style={{fontSize: '0.9rem', color: '#999'}}>
                                        Все треки уже добавлены в плейлист или недоступны
                                    </p>
                                </div>
                            )}
                        </>
                    )}
                </div>

                <div className={styles.modalFooter}>
                    <div className={styles.footerActions}>
                        <button
                            className={styles.cancelButton}
                            onClick={onClose}
                        >
                            Закрыть
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default AddTracksModal;