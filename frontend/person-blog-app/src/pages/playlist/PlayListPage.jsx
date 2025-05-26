import { useState, useEffect } from "react";
import { JwtTokenService } from '../../scripts/TokenStrorage';
import API from "../../scripts/apiMethod";
import { useNavigate, useParams } from "react-router-dom";
import SideBar from "../../components/sidebar/SideBar";
import './PlaylistPage.css';

const PlaylistPage = () => {

    const [playlist, setPlaylist] = useState({
        title: 'Мой плейлист',
        thumbnailUrl: 'default-cover.jpg',
        posts: []
    });

    const { playlistId } = useParams();
    const [isEditingTitle, setIsEditingTitle] = useState(false);
    const [showAddVideoModal, setShowAddVideoModal] = useState(false);
    const [availableVideos, setAvailableVideos] = useState([]);
    const navigate = useNavigate();
    const [selectedVideoIds, setSelectedVideoIds] = useState([]);

    useEffect(() => {
        fetchPlaylistData();
    }, [playlistId]);

    const fetchPlaylistData = async () => {
        const response = await API.get(`profile/api/Playlist/item/${playlistId}`);
        if (response.status === 200) {
            setPlaylist(response.data);
        }
    };

    const fetchAvailableVideos = async () => {
        const response = await API.get(`profile/api/Playlist/availableVideos?playlistId=${playlistId}`);
        if (response.status === 200) {
            setAvailableVideos(response.data);
        }
    };

    const handleTitleChange = (e) => {
        setPlaylist({ ...playlist, title: e.target.value });
    };

    const handleChangeCheckbox = (e) => {
        const id = e.target.value;
        if (e.target.checked) {
            setSelectedVideoIds([...selectedVideoIds, id]);
        } else {
            setSelectedVideoIds(selectedVideoIds.filter((vid) => vid !== id));
        }
    };
    const handleCoverChange = async (e) => {
        const file = e.target.files[0];
        const formData = new FormData();
        formData.append('cover', file);

        const response = await API.post("playlist/api/update-cover", formData, true);
        if (response.status === 200) {
            setPlaylist({ ...playlist, thumbnailUrl: URL.createObjectURL(file) });
        }
    };

    const saveTitleChanges = async () => {
        await API.put("playlist/api/update-title", { title: playlist.title });
        setIsEditingTitle(false);
    };

    const removeVideo = async (videoId) => {
        await API.post(`profile/api/Playlist/removeVideo`, {
            playListId: playlistId,
            postId: videoId
        });
        setPlaylist({ ...playlist, posts: playlist.posts.filter(v => v.id !== videoId) });
    };

    const addVideos = async (selectedIds) => {
        const response = await API.post("profile/api/Playlist/addVideo", { playlistId: playlistId, items: selectedIds.map(x => { return { postId: x, position: null } }) });
        if (response.status === 200) {
            setPlaylist(response.data);
        }
        setShowAddVideoModal(false);
    };

    if (!JwtTokenService.isAuth()) {
        return <div className="auth-warning">Вы не авторизованы</div>;
    }

    const PlaylistItem = ({ video, onRemove }) => {
        console.log(video)
        return (
            <div className="playlist-item" onClick={() => navigate(`/video/${video.id}`)}>
                <img src={video.previewUrl} alt="Превью" className="thumbnail" />
                <div className="details">
                    <h3 className="title">{video.title}</h3>
                    <div className="meta">
                        {/* <div className="author">{video.blogName}</div> */}
                        <div className="stats">
                            <span>{video.viewCount} просмотров</span>
                            <span>•</span>
                            <span>{video.videoData.length}</span>
                        </div>
                    </div>
                </div>
                <div className="postMeta">
                    <div className="postActions">
                        <button className="btn btnSecondary" onClick={(e) => {
                            e.stopPropagation();
                            onRemove(video.id);
                        }}>Удалить</button>
                    </div>
                </div>

            </div>
        );
    };

    return (
        <div className="page-container">
            <SideBar />
            <div className="content-container">
                <div className="playlist-header">
                    <div className="cover-container">
                        <img
                            src={playlist.thumbnailUrl}
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
                                />
                                <button onClick={saveTitleChanges}>Сохранить</button>
                                <button onClick={() => setIsEditingTitle(false)}>Отмена</button>
                            </div>
                        ) : (
                            <h1 className="playlist-title">
                                {playlist.title}
                                <button
                                    className="btn btnPrimary"
                                    onClick={() => setIsEditingTitle(true)}
                                >
                                    Редактировать
                                </button>
                            </h1>
                        )}
                        <button
                            className="btn btnPrimary"
                            onClick={async () => {
                                await fetchAvailableVideos()
                                setShowAddVideoModal(true)
                            }}>
                            Добавить видео
                        </button>
                    </div>
                </div>

                <div className="playlist-content">
                    {playlist.posts.length > 0 ? (
                        playlist.posts.map(video => (
                            <PlaylistItem
                                key={video.id}
                                video={video}
                                onRemove={removeVideo}
                            />
                        ))
                    ) : (
                        <div className="empty-playlist">
                            <p>В плейлисте пока нет видео</p>
                            <button
                                className=" btn btnPrimary"
                                onClick={async () => {
                                    await fetchAvailableVideos();
                                    setShowAddVideoModal(true)
                                }}
                            >
                                Добавить видео
                            </button>
                        </div>
                    )}
                </div>

                {showAddVideoModal && (
                    <div className="modal-overlay">
                        <div className="add-video-modal">
                            <h2>Выберите видео для добавления</h2>
                            <div className="video-list">
                                {availableVideos.map(video => (
                                    <div key={video.id} className="video-option">
                                        <label>
                                            <input
                                                type="checkbox"
                                                value={video.id}
                                                onChange={handleChangeCheckbox}
                                                checked={selectedVideoIds.includes(video.id)}
                                            />
                                            <img src={video.previewUrl} alt="Превью" />
                                            <span>{video.title}</span>
                                        </label>
                                    </div>
                                ))}
                            </div>
                            <div className="modal-actions">
                                <button onClick={() => {
                                    setShowAddVideoModal(false);
                                    setSelectedVideoIds([]); // Сброс состояния при закрытии
                                }}>
                                    Отмена
                                </button>
                                <button onClick={() => {
                                    addVideos(selectedVideoIds);
                                    setSelectedVideoIds([]); // Сброс состояния после добавления
                                }}>
                                    Добавить выбранные
                                </button>
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

export default PlaylistPage;