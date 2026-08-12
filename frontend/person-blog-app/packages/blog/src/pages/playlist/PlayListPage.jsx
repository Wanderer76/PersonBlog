import { useState, useEffect, useCallback, memo } from "react";
import API from "../../lib/api/client";
import { useNavigate, useParams } from "react-router-dom";
import SideBar from "../../components/sidebar/SideBar";
import { DragDropContext, Droppable, Draggable } from 'react-beautiful-dnd';
import './PlaylistPage.css';
import { secondsToHumanReadable } from "../../shared/LocalDate";
import { getPlayList } from "@/lib/api/generated/play-list/play-list";

const playListApi = getPlayList();

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
                                item={video}
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

const VideoOption = memo(({ item, isSelected, onToggle }) => (
    <div
        className={`video-option ${isSelected ? 'selected' : ''}`}
        onClick={() => onToggle(item)}
    >
        <img src={item.previewObjectName} alt="Превью" />
        <div className="video-info">
            <h4>{item.title}</h4>
            <p>{item.viewCount} просмотров</p>
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
                        src={video.previewObjectName}
                        alt="Превью"
                        className="thumbnail"
                        onClick={() => navigate(`/videoPage/${video.id}`)}
                    />
                    <div
                        className="details"
                        onClick={() => navigate(`/videoPage/${video.id}`)}
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
                        {!isDragDisabled && <button
                            className="btn btnSecondary"
                            onClick={(e) => {
                                e.stopPropagation();
                                onRemove(video.id);
                            }}
                        >
                            Удалить
                        </button>
                        }
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
        canEdit: true,
        posts: []
    });
    const { playlistId } = useParams();
    const [isEditingTitle, setIsEditingTitle] = useState(false);
    const [showAddVideoModal, setShowAddVideoModal] = useState(false);
    const [availableVideos, setAvailableVideos] = useState([]);
    const [selectedVideos, setSelectedVideos] = useState([]);
    const [isUpdatingOrder, setIsUpdatingOrder] = useState(false);

    const fetchPlaylistData = useCallback(async () => {
        const { data } = await playListApi.getApiPlayListItemId(playlistId);
        const videosWithPositions = (data.postPage?.items ?? []).map((video, index) => ({
            ...video,
            position: index + 1
        }));
        setPlaylist({ ...data.playList, posts: videosWithPositions });
    }, [playlistId]);

    const fetchAvailableVideos = useCallback(async () => {
        const { data } = await playListApi.getApiPlayListAvailableVideos({ playListId: playlistId });
        setAvailableVideos(data ?? []);
        setSelectedVideos([]);
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

        const uploadResponse = await API.post("/profile/api/Playlist/loadThumbnail", formData);
        if (uploadResponse.status === 200) {
            const thumbnailId = uploadResponse.data?.thumbnailId ?? uploadResponse.data;
            await API.post("/profile/api/Playlist/update", { playListId: playlistId, thumbnailId });
            setIsEditingTitle(false);
            await fetchPlaylistData();
        }
    }, [fetchPlaylistData, playlistId]);


    const saveTitleChanges = useCallback(async () => {
        await API.post("/profile/api/Playlist/update", { playListId: playlistId, title: playlist.title });
        setIsEditingTitle(false);
        await fetchPlaylistData();
    }, [fetchPlaylistData, playlist.title, playlistId]);


    const removeVideo = useCallback(async (videoId) => {
        await playListApi.postApiPlayListRemoveVideo({
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
 
        const response = await playListApi.postApiPlayListAddVideo({
            playListId: playlistId,
            postsToAdd: selectedVideos.map(x => x.id)
        });

        if (response.status === 200) {
            await fetchPlaylistData();
            setShowAddVideoModal(false);
        }
    }, [fetchPlaylistData, playlistId, selectedVideos]);

    const onDragEnd = useCallback(async (result) => {
        if (!result.destination) return;

        const items = Array.from(playlist.posts);
        const [reorderedItem] = items.splice(result.source.index, 1);
        items.splice(result.destination.index, 0, reorderedItem);

        const updatedVideos = items.map((item, index) => ({
            ...item,
            position: index + 1
        }));

        setPlaylist(prev => ({ ...prev, posts: updatedVideos }));

        setIsUpdatingOrder(true);
        try {
            await playListApi.postApiPlayListUpdatePositions({
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
                        {playlist.canEdit &&
                            <label className="edit-cover-btn" >
                                <input
                                    type="file"
                                    accept="image/*"
                                    onChange={handleCoverChange}
                                    hidden
                                />
                                ✏️
                            </label>}
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
                                {playlist.canEdit &&
                                    <button
                                        className="btn btnPrimary"
                                        onClick={() => setIsEditingTitle(true)}
                                    >
                                        Редактировать
                                    </button>
                                }
                            </div>
                        )}
                        {playlist.canEdit &&
                            <button
                                className="btn btnPrimary add-video-btn"
                                onClick={handleOpenModal}
                            >
                                Добавить видео
                            </button>
                        }
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
                                                isDragDisabled={!playlist.canEdit}
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
