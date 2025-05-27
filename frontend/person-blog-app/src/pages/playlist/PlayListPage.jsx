import { useState, useEffect, useCallback, memo } from "react";
import { JwtTokenService } from '../../scripts/TokenStrorage';
import API from "../../scripts/apiMethod";
import { useNavigate, useParams } from "react-router-dom";
import SideBar from "../../components/sidebar/SideBar";
import { DragDropContext, Droppable, Draggable } from 'react-beautiful-dnd';
import './PlaylistPage.css';
import { secondsToHumanReadable } from "../../scripts/LocalDate";

// Вынесенные компоненты
const AddVideoModal = memo(({
    show,
    onClose,
    availableVideos,
    selectedVideos,
    onToggleSelection,
    onAddVideos
}) => {
    if (!show) return null;

    return (
        <div className="modal-overlay">
            <div className="add-video-modal">
                <div className="modal-header">
                    <h2>Добавить видео в плейлист</h2>
                    <button className="close-modal" onClick={onClose}>×</button>
                </div>
                <div className="modal-content">
                    <div className="available-videos">
                        {availableVideos.map(video => (
                            <VideoOption
                                key={video.id}
                                video={video}
                                isSelected={selectedVideos.some(v => v.id === video.id)}
                                onToggle={onToggleSelection}
                            />
                        ))}
                    </div>
                </div>
                <div className="modal-footer">
                    <div className="selected-count">Выбрано: {selectedVideos.length}</div>
                    <div className="modal-actions">
                        <button className="btn btnSecondary" onClick={onClose}>Отмена</button>
                        <button
                            className="btn btnPrimary"
                            onClick={onAddVideos}
                            disabled={selectedVideos.length === 0}
                        >
                            Добавить выбранные
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
});

const VideoOption = memo(({ video, isSelected, onToggle }) => (
    <div
        className={`video-option ${isSelected ? 'selected' : ''}`}
        onClick={() => onToggle(video)}
    >
        <img src={video.previewUrl} alt="Превью" />
        <div className="video-info">
            <h4>{video.title}</h4>
            <p>{video.viewCount} просмотров</p>
        </div>
        <div className="selection-checkbox">
            {isSelected ? '✓' : ''}
        </div>
    </div>
));

const PlaylistItem = memo(({ video, onRemove, index, isDragDisabled }) => {
    const navigate = useNavigate();

    return (
        <Draggable draggableId={video.id.toString()} index={index} isDragDisabled={isDragDisabled}>
            {(provided, snapshot) => (
                <div
                    ref={provided.innerRef}
                    {...provided.draggableProps}
                    {...provided.dragHandleProps}
                    className={`playlist-item ${snapshot.isDragging ? 'dragging' : ''}`}
                >
                    <span className="position-badge">{video.position}</span>
                    <img
                        src={video.previewUrl}
                        alt="Превью"
                        className="thumbnail"
                        onClick={() => navigate(`/video/${video.id}`)}
                    />
                    <div
                        className="details"
                        onClick={() => navigate(`/video/${video.id}`)}
                    >
                        <h3 className="title">{video.title}</h3>
                        <div className="meta">
                            <div className="stats">
                                <span>{video.viewCount} просмотров</span>
                                <span>•</span>
                                <span>{secondsToHumanReadable(video?.videoData?.duration)}</span>
                            </div>
                        </div>
                    </div>
                    <div className="postActions">
                        <button
                            className="btn btnSecondary"
                            onClick={(e) => {
                                e.stopPropagation();
                                onRemove(video.id);
                            }}
                        >
                            Удалить
                        </button>
                    </div>
                </div>
            )}
        </Draggable>
    );
});

const PlaylistPage = () => {
    const [playlist, setPlaylist] = useState({
        title: '',
        thumbnailUrl: '',
        posts: []
    });
    const { playlistId } = useParams();
    const [isEditingTitle, setIsEditingTitle] = useState(false);
    const [showAddVideoModal, setShowAddVideoModal] = useState(false);
    const [availableVideos, setAvailableVideos] = useState([]);
    const [selectedVideos, setSelectedVideos] = useState([]);
    const [isUpdatingOrder, setIsUpdatingOrder] = useState(false);

    const fetchPlaylistData = useCallback(async () => {
        const response = await API.get(`profile/api/Playlist/item/${playlistId}`);
        if (response.status === 200) {
            const videosWithPositions = response.data.posts.map((video, index) => ({
                ...video,
                position: index + 1
            }));
            setPlaylist({ ...response.data, posts: videosWithPositions });
        }
    }, [playlistId]);

    const fetchAvailableVideos = useCallback(async () => {
        const response = await API.get(`profile/api/Playlist/availableVideos?playlistId=${playlistId}`);
        if (response.status === 200) {
            setAvailableVideos(response.data);
            setSelectedVideos([]);
        }
    }, [playlistId]);

    useEffect(() => {
        fetchPlaylistData();
    }, [playlistId, fetchPlaylistData]);

    const handleTitleChange = useCallback((e) => {
        setPlaylist(prev => ({ ...prev, title: e.target.value }));
    }, []);

    const handleCoverChange = useCallback(async (e) => {
        const file = e.target.files[0];
        if (!file) return;

        const formData = new FormData();
        formData.append('thumbnail', file);

        const thumbnailId = await API.post("profile/api/Playlist/loadThumbnail", formData, true);
        if (thumbnailId.status === 200) {
            const response = await API.post("profile/api/Playlist/update", { playListId: playlistId, thumbnailId: thumbnailId.data.thumbnailId });
            setIsEditingTitle(false);
            const videosWithPositions = response.data.posts.map((video, index) => ({
                ...video,
                position: index + 1
            }));
            setPlaylist({ ...response.data, posts: videosWithPositions });
        }
    }, []);


    const saveTitleChanges = useCallback(async () => {
        const response = await API.post("profile/api/Playlist/update", { playListId: playlistId, title: playlist.title });
        setIsEditingTitle(false);
        const videosWithPositions = response.data.posts.map((video, index) => ({
            ...video,
            position: index + 1
        }));
        setPlaylist({ ...response.data, posts: videosWithPositions });
    }, [playlist.title]);


    const removeVideo = useCallback(async (videoId) => {
        await API.post(`profile/api/Playlist/removeVideo`, {
            playListId: playlistId,
            postId: videoId
        });
        setPlaylist(prev => ({
            ...prev,
            posts: prev.posts.filter(v => v.id !== videoId)
                .map((video, index) => ({ ...video, position: index + 1 }))
        }));
    }, [playlistId]);

    const toggleVideoSelection = useCallback((video) => {
        setSelectedVideos(prev =>
            prev.some(v => v.id === video.id)
                ? prev.filter(v => v.id !== video.id)
                : [...prev, { ...video, position: playlist.posts.length + prev.length + 1 }]
        );
    }, [playlist.posts.length]);

    const addVideos = useCallback(async () => {
        if (selectedVideos.length === 0) return;

        const response = await API.post("profile/api/Playlist/addVideo", {
            playlistId: playlistId,
            items: selectedVideos.map(video => ({
                postId: video.id,
                position: video.position
            }))
        });

        if (response.status === 200) {
            setPlaylist(prev => ({
                ...prev,
                posts: [...response.data.posts.map((video, index) => ({
                    ...video,
                    position: index + 1
                }))]
            }));
            setShowAddVideoModal(false);
        }
    }, [playlistId, selectedVideos]);

    const onDragEnd = useCallback(async (result) => {
        if (!result.destination) return;

        const items = Array.from(playlist.posts);
        const [reorderedItem] = items.splice(result.source.index, 1);
        console.log(result)
        console.log(reorderedItem)
        console.log(result.destination.index)
        items.splice(result.destination.index, 0, reorderedItem);

        const updatedVideos = items.map((item, index) => ({
            ...item,
            position: index + 1
        }));

        setPlaylist(prev => ({ ...prev, posts: updatedVideos }));

        setIsUpdatingOrder(true);
        try {
            await API.post("profile/api/Playlist/updatePositions", {
                playlistId: playlistId,
                postId: reorderedItem.id,
                destination: result.destination.index + 1
            });
        } catch (error) {
            console.error("Error updating video positions:", error);
            fetchPlaylistData();
        } finally {
            setIsUpdatingOrder(false);
        }
    }, [playlist.posts, playlistId, fetchPlaylistData]);

    const handleOpenModal = useCallback(async () => {
        await fetchAvailableVideos();
        setShowAddVideoModal(true);
    }, [fetchAvailableVideos]);

    if (!JwtTokenService.isAuth()) {
        return <div className="auth-warning">Вы не авторизованы</div>;
    }

    return (
        <div className="page-container">
            <SideBar />
            <div className="content-container">
                <div className="playlist-header">
                    <div className="cover-container">
                        <img
                            src={playlist.thumbnailUrl || 'default-cover.jpg'}
                            alt="Обложка плейлиста"
                            className="playlist-cover"
                        />
                        <label className="edit-cover-btn">
                            <input
                                type="file"
                                accept="image/*"
                                onChange={handleCoverChange}
                                hidden
                            />
                            ✏️
                        </label>
                    </div>
                    <div className="playlist-info">
                        {isEditingTitle ? (
                            <div className="title-edit">
                                <input
                                    type="text"
                                    value={playlist.title}
                                    onChange={handleTitleChange}
                                    className="title-input"
                                />
                                <div className="edit-actions">
                                    <button
                                        className="btn btnPrimary"
                                        onClick={saveTitleChanges}
                                    >
                                        Сохранить
                                    </button>
                                    <button
                                        className="btn btnSecondary"
                                        onClick={() => setIsEditingTitle(false)}
                                    >
                                        Отмена
                                    </button>
                                </div>
                            </div>
                        ) : (
                            <div className="title-display">
                                <h1>{playlist.title}</h1>
                                <button
                                    className="btn btnPrimary"
                                    onClick={() => setIsEditingTitle(true)}
                                >
                                    Редактировать
                                </button>
                            </div>
                        )}
                        <button
                            className="btn btnPrimary add-video-btn"
                            onClick={handleOpenModal}
                        >
                            Добавить видео
                        </button>
                    </div>
                </div>

                <div className="playlist-content">
                    {isUpdatingOrder && <div className="updating-order">Обновление порядка...</div>}
                    {playlist.posts.length > 0 ? (
                        <DragDropContext onDragEnd={onDragEnd} >
                            <Droppable droppableId="playlist-videos" >
                                {(provided) => (
                                    <div
                                        {...provided.droppableProps}
                                        ref={provided.innerRef}
                                        className="videos-list"
                                    >
                                        {playlist.posts.map((video, index) => (
                                            <PlaylistItem
                                                key={video.id}
                                                video={video}
                                                index={index}
                                                onRemove={removeVideo}
                                                isDragDisabled={false}
                                            />
                                        ))}
                                        {provided.placeholder}
                                    </div>
                                )}
                            </Droppable>
                        </DragDropContext>
                    ) : (
                        <div className="empty-playlist">
                            <p>В плейлисте пока нет видео</p>
                            <button
                                className="btn btnPrimary"
                                onClick={handleOpenModal}
                            >
                                Добавить видео
                            </button>
                        </div>
                    )}
                </div>

                <AddVideoModal
                    show={showAddVideoModal}
                    onClose={() => setShowAddVideoModal(false)}
                    availableVideos={availableVideos}
                    selectedVideos={selectedVideos}
                    onToggleSelection={toggleVideoSelection}
                    onAddVideos={addVideos}
                />
            </div>
        </div>
    );
};

export default PlaylistPage;