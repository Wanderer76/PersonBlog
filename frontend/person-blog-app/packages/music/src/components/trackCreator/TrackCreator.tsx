import React, { useState, useEffect, useRef } from 'react';
import styles from './TrackCreator.module.css';
import API from '../../scripts/apiMethod';
import type { MultiValue, ActionMeta, InputActionMeta } from 'react-select';
import CreatableSelect from 'react-select/creatable';
import AsyncSelect from 'react-select/async';
import type { AxiosResponse } from 'axios';
import type { Artist, ArtistOption, CreateView, Genre, GenreOption, TrackCreateRequest, TrackFileMetadata } from '../../types/trackCreate';
import { useNavigate } from 'react-router-dom';
import { base64ToFile } from '../../scripts/helper';

const LOCAL_STORAGE_TRACK_KEY = 'uploadedTrackMetadata';

interface TrackCreatorProps {
    onSuccessRedirect?: string;
}

// --- Компонент создания трека ---
const TrackCreator: React.FC<TrackCreatorProps> = ({ onSuccessRedirect = '/profile' }) => {
    const [genres, setGenres] = useState<Genre[]>([]);
    const [selectedGenres, setSelectedGenres] = useState<string[]>([]);
    const [trackFile, setTrackFile] = useState<File | null>(null);
    const [thumbnail, setThumbnail] = useState<File | null>(null);
    const [trackMetadata, setTrackMetadata] = useState<TrackFileMetadata | null>(null);
    const [isUploading, setIsUploading] = useState(false);
    const [uploadProgress, setUploadProgress] = useState(0);
    const navigate = useNavigate();

    // --- Восстановление из localStorage ---
    useEffect(() => {
        const saved = localStorage.getItem(LOCAL_STORAGE_TRACK_KEY);
        if (saved) {
            try {
                const parsed: TrackFileMetadata = JSON.parse(saved);
                if (parsed.trackFileId && parsed.title) {
                    setTrackMetadata(parsed);
                    console.log('Состояние трека восстановлено из localStorage');
                }
            } catch (e) {
                console.warn('Не удалось восстановить данные из localStorage');
                localStorage.removeItem(LOCAL_STORAGE_TRACK_KEY);
            }
        }

        const fetchGenres = async () => {
            try {
                const response = await API.get<CreateView>('/track/create');
                setGenres(response.data.genres || []);
            } catch (error) {
                console.error('Ошибка загрузки жанров:', error);
            }
        };

        fetchGenres();
    }, []);

    // --- Управление localStorage ---
    const saveToLocalStorage = (meta: TrackFileMetadata) => {
        try {
            localStorage.setItem(LOCAL_STORAGE_TRACK_KEY, JSON.stringify(meta));
        } catch (e) {
            console.warn('Не удалось сохранить в localStorage', e);
        }
    };

    const clearFromLocalStorage = () => {
        localStorage.removeItem(LOCAL_STORAGE_TRACK_KEY);
    };

    // --- Загрузка аудиофайла ---
    const handleTrackUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (!file) return;

        // Сброс состояния
        clearFromLocalStorage();
        setTrackMetadata(null);
        setThumbnail(null);
        setSelectedGenres([]);
        setIsUploading(true);
        setUploadProgress(0);

        if (!isValidMp3File(file)) {
            alert('Недопустимый формат файла. Разрешены только MP3 файлы.');
            setIsUploading(false);
            event.target.value = '';
            return;
        }

        setTrackFile(file);

        const formData = new FormData();
        formData.append('track', file);

        try {
            const response = await API.post('/track/uploadTrackFile', formData, {
                headers: {
                    'Content-Type': 'multipart/form-data',
                },
                onUploadProgress: (progressEvent) => {
                    const percentCompleted = Math.round(
                        (progressEvent.loaded * 100) / (progressEvent.total || 1)
                    );
                    setUploadProgress(percentCompleted);
                },
            });

            const metadata: TrackFileMetadata = response.data;
            setTrackMetadata(metadata);
            saveToLocalStorage(metadata);

            // Автозагрузка обложки из coverBase64
            if (metadata.hasCover && metadata.coverBase64 && metadata.coverMimeType) {
                const coverFile = base64ToFile(
                    `${metadata.coverMimeType};base64,${metadata.coverBase64}`,
                    `cover-${metadata.trackFileId}.jpg`,
                    metadata.coverMimeType
                );
                setThumbnail(coverFile);

                const thumbFormData = new FormData();
                thumbFormData.append('thumbnail', coverFile);

                try {
                    const thumbResponse = await API.post('/track/uploadTrackThumbnail', thumbFormData, {
                        headers: { 'Content-Type': 'multipart/form-data' },
                    });
                    const updatedMetadata = {
                        ...metadata,
                        thumbnailId: thumbResponse.data,
                    };
                    setTrackMetadata(updatedMetadata);
                    saveToLocalStorage(updatedMetadata);
                } catch (error) {
                    console.error('Ошибка загрузки обложки из метаданных:', error);
                }
            }

            setIsUploading(false);
            setUploadProgress(0);
        } catch (error) {
            console.error('Ошибка загрузки трека:', error);
            setIsUploading(false);
            alert('Ошибка загрузки трека');
        }

        event.target.value = '';
    };

    // --- Загрузка обложки ---
    const handleThumbnailUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (!file) return;

        setThumbnail(file);

        const formData = new FormData();
        formData.append('thumbnail', file);

        try {
            const response = await API.post('/track/uploadTrackThumbnail', formData, {
                headers: { 'Content-Type': 'multipart/form-data' },
            });

            if (trackMetadata) {
                const updatedMetadata = {
                    ...trackMetadata,
                    thumbnailId: response.data,
                };
                setTrackMetadata(updatedMetadata);
                saveToLocalStorage(updatedMetadata);
            }
        } catch (error) {
            console.error('Ошибка загрузки обложки:', error);
            alert('Ошибка загрузки обложки');
        }
    };

    // --- Создание трека ---
    const handleCreateTrack = async (formData: TrackCreateRequest) => {
        try {
            await API.post('/track/create', formData);
            alert('Трек успешно создан!');

            clearFromLocalStorage();
            setTrackMetadata(null);
            setTrackFile(null);
            setThumbnail(null);
            setSelectedGenres([]);
            navigate(onSuccessRedirect);
        } catch (error) {
            console.error('Ошибка создания трека:', error);
            alert('Ошибка создания трека');
        }
    };

    // --- Валидация MP3 ---
    const isValidMp3File = (file: File): boolean => {
        const allowedExtensions = ['.mp3', '.MP3'];
        const extension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
        return allowedExtensions.includes(extension) && file.type.includes('audio/mpeg');
    };

    // --- Если нет метаданных — показываем загрузку ---
    if (!trackMetadata) {
        return (
            <div className={styles.trackCreator}>
                <div className={styles.uploadSection}>
                    <h2>Загрузите аудиофайл</h2>
                    <input
                        type="file"
                        accept=".mp3,.MP3,audio/mpeg"
                        onChange={handleTrackUpload}
                        disabled={isUploading}
                    />
                    {isUploading && (
                        <div className={styles.progressBar}>
                            <div
                                className={styles.progressFill}
                                style={{ width: `${uploadProgress}%` }}
                            />
                            <span>{uploadProgress}%</span>
                        </div>
                    )}
                </div>
            </div>
        );
    }

    // --- Переходим к форме ---
    return (
        <div className={styles.trackCreator}>
            <TrackForm
                metadata={trackMetadata}
                genres={genres}
                selectedGenres={selectedGenres}
                onGenreChange={setSelectedGenres}
                onThumbnailUpload={handleThumbnailUpload}
                onSubmit={handleCreateTrack}
                onReset={() => {
                    setTrackMetadata(null);
                    setTrackFile(null);
                    setThumbnail(null);
                    setSelectedGenres([]);
                    clearFromLocalStorage();
                }}
            />
        </div>
    );
};

const debounce = <T extends (...args: any[]) => Promise<any>>(
    func: T,
    delay: number
) => {
    let timeoutId: NodeJS.Timeout;
    return (...args: Parameters<T>): ReturnType<T> => {
        clearTimeout(timeoutId);
        return new Promise((resolve, reject) => {
            timeoutId = setTimeout(async () => {
                try {
                    const result = await func(...args);
                    resolve(result);
                } catch (error) {
                    reject(error);
                }
            }, delay);
        }) as ReturnType<T>;
    };
};

// --- Форма редактирования ---
interface TrackFormProps {
    metadata: TrackFileMetadata;
    genres: Genre[];
    selectedGenres: string[];
    onGenreChange: (genres: string[]) => void;
    onThumbnailUpload: (event: React.ChangeEvent<HTMLInputElement>) => void;
    onSubmit: (request: TrackCreateRequest) => void;
    onReset: () => void;
}

const TrackForm: React.FC<TrackFormProps> = ({
    metadata,
    genres,
    selectedGenres,
    onGenreChange,
    onThumbnailUpload,
    onSubmit,
    onReset,
}) => {
    const [formData, setFormData] = useState({
        name: metadata.title,
        year: metadata.year,
    });
    const selectRef = useRef<any>(null);

    const [inputArtist, setInputArtist] = useState(metadata.artist);
    const [selectedArtist, setSelectedArtist] = useState<ArtistOption | null>(
        metadata?.artistId == null ? null : { label: metadata.artist, value: metadata.artistId }
    );
    const [debouncedLoadOptions, setDebouncedLoadOptions] = useState<
        (inputValue: string) => Promise<ArtistOption[]>
    >(() => () => Promise.resolve([]));

    // Преобразуем жанры в опции
    const genreOptions: GenreOption[] = genres.map((g) => ({
        value: g.id,
        label: g.name,
    }));

    const selectedGenreOptions: GenreOption[] = genreOptions.filter((opt) =>
        selectedGenres.includes(opt.value)
    );

    useEffect(() => {
        const loadOptions = async (inputValue: string): Promise<ArtistOption[]> => {
            if (!inputValue.trim()) return [];
            try {
                const response: AxiosResponse<Artist[]> = await API.get('/Artist/search', {
                    params: { name: inputValue },
                });
                if (response.data.length > 0) {
                    return response.data.map((artist) => ({
                        value: artist.id,
                        label: artist.name,
                    }));
                }
                return [{ label: inputValue.trim(), value: null }]
            } catch (error) {
                console.error('Ошибка при поиске артистов:', error);
                return [];
            }
        };

        // Применяем дебаунс: 500 мс
        setDebouncedLoadOptions(() => debounce(loadOptions, 500));
    }, []);

    const handleGenreSelectChange = (
        newValue: MultiValue<GenreOption>,
        actionMeta: ActionMeta<GenreOption>
    ) => {
        const selectedIds = newValue.map((opt) => opt.value);
        onGenreChange(selectedIds);
    };

    const handleCreateGenre = (inputValue: string) => {
        const newId = `temp-${Date.now()}`;
        const newOption: GenreOption = { value: newId, label: inputValue };
        const updatedSelected = [...selectedGenreOptions, newOption];
        onGenreChange(updatedSelected.map((opt) => opt.value));
    };

    // Обработчик изменения ввода артиста
    const handleArtistInputChange = (inputValue: string, actionMeta: InputActionMeta) => {
        console.log(inputArtist);
        console.log(selectedArtist);

        setInputArtist(inputValue)

        return inputValue;
    };

    // Обработчик изменения выбора артиста
    const handleArtistChange = (option: ArtistOption | null) => {
        setSelectedArtist(option);
        if (option) {
            setInputArtist(option?.label ?? '');
        } else {
            setSelectedArtist(null);
            setInputArtist('');
        }
        if (selectRef.current) {
            selectRef.current.blur();
        }
    };

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();

        const request: TrackCreateRequest = {
            name: formData.name,
            artistName: inputArtist || metadata.artist,
            trackFileId: metadata.trackFileId,
            genres: selectedGenres,
            artistId: selectedArtist?.value || null,
            thumbnailId: metadata.thumbnailId || null,
            year: formData.year,
            postId: null,
            albumId: null
        };

        onSubmit(request);
    };

    const formatDuration = (millisecond: number): string => {
        const seconds = millisecond / 1000;
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        return `${mins}:${secs < 10 ? '0' : ''}${secs}`;
    };

    return (
        <form className={styles.trackForm} onSubmit={handleSubmit}>
            {/* Уведомление о восстановлении */}
            <div className={styles.restoreNotice}>
                Трек восстановлен. Вы можете изменить данные или загрузить другой.
            </div>

            <div className={styles.formSection}>
                <h2>Информация о треке</h2>

                <div className={styles.formGroup}>
                    <label>Название трека</label>
                    <input
                        type="text"
                        value={formData.name}
                        onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))}
                        className={styles.formInput}
                        required
                    />
                </div>

                <div className={styles.formGroup}>
                    <label>Исполнитель</label>
                    <AsyncSelect<ArtistOption>
                        ref={selectRef}
                        cacheOptions={false}
                        defaultOptions={false}
                        value={selectedArtist}
                        // inputValue={inputArtist}
                        onInputChange={handleArtistInputChange}
                        onChange={handleArtistChange}
                        loadOptions={debouncedLoadOptions}
                        onMenuClose={() => {
                            // Также убираем фокус при закрытии меню без выбора
                            if (selectRef.current) {
                                selectRef.current.blur();
                            }
                        }}
                        placeholder="Начните вводить имя исполнителя..."
                        noOptionsMessage={() => 'Нет совпадений. Введенное имя будет использовано'}
                        isClearable
                        styles={{
                            control: (base, state) => ({
                                ...base,
                                borderColor: state.isFocused ? '#ff7a00' : '#ddd',
                                boxShadow: state.isFocused ? '0 0 0 1px #ff7a00' : 'none',
                                minHeight: '40px',
                                borderRadius: '4px',
                            }),
                        }}
                    />
                </div>

                <div className={styles.formGroup}>
                    <label>Год</label>
                    <input
                        type="number"
                        value={formData.year}
                        onChange={(e) => setFormData((prev) => ({ ...prev, year: Number(e.target.value) }))}
                        className={styles.formInput}
                        min="1900"
                        max="2099"
                    />
                </div>

                {/* Жанры */}
                <div className={styles.formGroup}>
                    <label>Жанры</label>
                    <CreatableSelect
                        isMulti
                        options={genreOptions}
                        value={selectedGenreOptions}
                        onChange={handleGenreSelectChange}
                        onCreateOption={handleCreateGenre}
                        placeholder="Выберите или добавьте жанр..."
                        noOptionsMessage={() => 'Начните вводить...'}
                        formatCreateLabel={(inputValue) => `+ Добавить "${inputValue}"`}
                        styles={{
                            control: (base, state) => ({
                                ...base,
                                borderColor: state.isFocused ? '#ff7a00' : '#ddd',
                                boxShadow: state.isFocused ? '0 0 0 1px #ff7a00' : 'none',
                                minHeight: '40px',
                                borderRadius: '4px',
                            }),
                            multiValue: (base) => ({
                                ...base,
                                backgroundColor: '#ff7a00',
                            }),
                            multiValueLabel: (base) => ({
                                ...base,
                                color: 'white',
                                fontWeight: 'bold',
                            }),
                            multiValueRemove: (base) => ({
                                ...base,
                                color: 'white',
                                ':hover': {
                                    backgroundColor: '#c85a00',
                                    color: 'white',
                                },
                            }),
                            dropdownIndicator: (base) => ({
                                ...base,
                                color: '#ff7a00',
                            }),
                            indicatorSeparator: () => ({ display: 'none' }),
                        }}
                    />
                </div>
            </div>

            {/* Обложка */}
            <div className={styles.formSection}>
                <h2>Обложка</h2>
                {metadata.hasCover && metadata.coverBase64 ? (
                    <img
                        src={`data:${metadata.coverMimeType};base64,${metadata.coverBase64}`}
                        alt="Обложка трека"
                        className={styles.coverPreview}
                    />
                ) : (
                    <div className={styles.noCover}>Обложка не найдена в MP3 файле</div>
                )}

                <input
                    type="file"
                    accept="image/*"
                    onChange={onThumbnailUpload}
                    className={styles.fileInput}
                />
                <small>Вы можете загрузить свою обложку</small>
            </div>

            {/* Информация о файле */}
            <div className={styles.trackInfo}>
                <h3>Информация о файле</h3>
                <p>Длительность: {formatDuration(metadata.duration)}</p>
                <p>Битрейт: {metadata.bitrate}</p>
                <p>Размер: {(metadata.fileSize / 1024 / 1024).toFixed(2)} MB</p>
                <p>Формат: MP3</p>
            </div>

            {/* Кнопки */}
            <div className={styles.formActions}>
                <button type="button" onClick={onReset} className={styles.changeTrackButton}>
                    Загрузить другой трек
                </button>
                <button type="submit" className={styles.submitButton}>
                    Создать трек
                </button>
            </div>
        </form>
    );
};

export default TrackCreator;