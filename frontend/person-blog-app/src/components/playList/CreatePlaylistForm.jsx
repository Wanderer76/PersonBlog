import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { DragDropContext, Droppable, Draggable } from "react-beautiful-dnd";
import API from "../../scripts/apiMethod";
import { JwtTokenService } from '../../scripts/TokenStrorage';
import styles from './CreatePlaylistForm.module.css';

const CreatePlaylistForm = () => {
    const [playlistForm, setPlaylistForm] = useState({
        title: "",
        description: "",
        thumbnailId: null,
        thumbnailUrl: null,
        isPublic: true

    });
    const [availableVideos, setAvailableVideos] = useState([]);
    const [selectedVideos, setSelectedVideos] = useState([]);
    const [isLoading, setIsLoading] = useState(false);
    const navigate = useNavigate();

    useEffect(() => {
        fetchAvailableVideos();
    }, []);

    const fetchAvailableVideos = async () => {
        const response = await API.get("profile/api/Playlist/availableVideos");
        if (response.status === 200) {
            setAvailableVideos(response.data);
        }
    };

    const handleFormChange = (e) => {
        const { name, value, type, checked } = e.target;
        setPlaylistForm(prev => ({
            ...prev,
            [name]: type === 'checkbox' ? checked : value
        }));
    };

    const handleCoverChange = async (e) => {
        const file = e.target.files[0];
        if (file) {
            const formData = new FormData();
            formData.append('thumbnail', file);
            var response = await API.post("profile/api/Playlist/loadThumbnail", formData, {
                headers: {
                    ['Content-Type']: 'multipart/form-data'
                }
            });
            if (response.status === 200)
                setPlaylistForm(prev => ({ ...prev, thumbnailId: response.data.thumbnailId, thumbnailUrl: response.data.thumbnailUrl }));
        }
    };

    const handleAddVideo = (video) => {
        if (!selectedVideos.some(v => v.id === video.id)) {
            setSelectedVideos(prev => [
                ...prev,
                { ...video, position: prev.length + 1 }
            ]);
        }
    };

    const handleRemoveVideo = (videoId) => {
        setSelectedVideos(prev =>
            prev.filter(v => v.id !== videoId)
                .map((v, index) => ({ ...v, position: index + 1 }))
        );
    };

    const onDragEnd = (result) => {
        if (!result.destination) return;

        const items = Array.from(selectedVideos);
        const [reorderedItem] = items.splice(result.source.index, 1);
        items.splice(result.destination.index, 0, reorderedItem);

        setSelectedVideos(items.map((v, index) => ({
            ...v,
            position: index + 1
        })));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setIsLoading(true);

        try {
            const formData = {
                title: playlistForm.title,
                description: playlistForm.description,
                isPublic: playlistForm.isPublic,
                thumbnailId: playlistForm.thumbnailId,
                postIds: selectedVideos.map(video => video.id)
            };


            const response = await API.post("profile/api/Playlist/create", formData);

            if (response.status === 200) {
                navigate(`/playlist/${response.data.id}`);
            }
        } catch (error) {
            console.error("Error creating playlist:", error);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className={styles.modal}>
            <div className={styles.createPlaylistForm}>
                <h1>Создать новый плейлист</h1>

                <form onSubmit={handleSubmit}>
                    <div className={styles.formGroup}>
                        <label>Название плейлиста</label>
                        <input
                            type="text"
                            name="title"
                            value={playlistForm.title}
                            onChange={handleFormChange}
                            placeholder="Введите название плейлиста"
                            required
                        />
                    </div>

                    <div className={styles.formGroup}>
                        <label>Описание</label>
                        <textarea
                            name="description"
                            value={playlistForm.description}
                            onChange={handleFormChange}
                            placeholder="Добавьте описание плейлиста"
                            rows="4"
                        />
                    </div>

                    <div className={styles.formGroup}>
                        <label>Обложка плейлиста</label>
                        <div className={styles.coverUpload}>
                            <label className={styles.coverUploadBtn}>
                                Выбрать обложку
                                <input
                                    type="file"
                                    accept="image/*"
                                    onChange={handleCoverChange}
                                    hidden
                                />
                            </label>
                            {playlistForm.thumbnailUrl && (
                                <div className={styles.coverPreview}>
                                    <img
                                        src={playlistForm.thumbnailUrl}
                                        alt="Предпросмотр обложки"
                                    />
                                </div>
                            )}
                        </div>
                    </div>

                    <div className={styles.formGroup}>
                        <label className={styles.privacyToggle}>
                            <input
                                type="checkbox"
                                name="isPublic"
                                checked={playlistForm.isPublic}
                                onChange={handleFormChange}
                            />
                            <span className={styles.toggleSlider}></span>
                            <span>{playlistForm.isPublic ? "Публичный" : "Приватный"}</span>
                        </label>
                    </div>

                    <div className={styles.videoSelectionContainer}>
                        <div className={styles.availableVideos}>
                            <h3>Доступные видео</h3>
                            <div className={styles.videoList}>
                                {availableVideos
                                    .filter(v => !selectedVideos.some(sv => sv.id === v.id))
                                    .map(video => (
                                        <div key={video.id} className={styles.videoItem}>
                                            <img src={video.previewUrl} alt={video.title} />
                                            <div className={styles.videoInfo}>
                                                <h4>{video.title}</h4>
                                                <p>{video.viewCount} просмотров</p>
                                            </div>
                                            <button
                                                type="button"
                                                className={styles.addVideoBtn}
                                                onClick={() => handleAddVideo(video)}
                                            >
                                                Добавить
                                            </button>
                                        </div>
                                    ))}
                            </div>
                        </div>

                        <div className={styles.selectedVideos}>
                            <h3>Видео в плейлисте ({selectedVideos.length})</h3>
                            {selectedVideos.length > 0 ? (
                                <DragDropContext onDragEnd={onDragEnd}>
                                    <Droppable droppableId="selectedVideos">
                                        {(provided) => (
                                            <div
                                                {...provided.droppableProps}
                                                ref={provided.innerRef}
                                                className={styles.videoList}
                                            >
                                                {selectedVideos.map((video, index) => (
                                                    <Draggable
                                                        key={video.id}
                                                        draggableId={video.id.toString()}
                                                        index={index}
                                                    >
                                                        {(provided) => (
                                                            <div
                                                                ref={provided.innerRef}
                                                                {...provided.draggableProps}
                                                                {...provided.dragHandleProps}
                                                                className={`${styles.videoItem} ${styles.selected}`}
                                                            >
                                                                <span className={styles.positionBadge}>{video.position}</span>
                                                                <img src={video.previewUrl} alt={video.title} />
                                                                <div className={styles.videoInfo}>
                                                                    <h4>{video.title}</h4>
                                                                    <p>{video.viewCount} просмотров</p>
                                                                </div>
                                                                <button
                                                                    type="button"
                                                                    className={styles.removeVideoBtn}
                                                                    onClick={() => handleRemoveVideo(video.id)}
                                                                >
                                                                    ×
                                                                </button>
                                                            </div>
                                                        )}
                                                    </Draggable>
                                                ))}
                                                {provided.placeholder}
                                            </div>
                                        )}
                                    </Droppable>
                                </DragDropContext>
                            ) : (
                                <div className={styles.emptySelection}>
                                    <p>Добавьте видео из списка доступных</p>
                                </div>
                            )}
                        </div>
                    </div>

                    <div className={styles.actionButtons}>
                        <button
                            type="button"
                            className={`${styles.btn} ${styles.btnSecondary}`}
                            onClick={() => navigate('/profile')}
                        >
                            Отмена
                        </button>
                        <button
                            type="submit"
                            className={`${styles.btn} ${styles.btnPrimary}`}
                            disabled={isLoading || !playlistForm.title || selectedVideos.length === 0}
                        >
                            {isLoading ? "Создание..." : "Создать плейлист"}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default CreatePlaylistForm;