import React, { useEffect, useRef, useState } from "react";
import './CreatePostForm.css';
import API, { BaseApUrl } from "../../../scripts/apiMethod";
import { useNavigate, useParams } from "react-router-dom";
import VideoPlayer from "../../VideoPlayer/VideoPlayer";

const EditPostForm = () => {
    const queryParam  = useParams(); // Получаем ID поста из URL
    const [postForm, setPostForm] = useState({
        id: queryParam.id,
        type: 1,
        title: "",
        description: "",
        visibility: 1,
        videoUrl: "",
        thumbnailUrl: "",
        vidoeObjectName: "",
        thumbnailFile: null // Новое поле для загрузки обложки
    });
    const [createModel, setCreateModel] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState(null);
    const navigate = useNavigate();

    // Загрузка начальных данных
    useEffect(() => {
        const fetchData = async () => {
            try {
                const [createFormRes, postRes] = await Promise.all([
                    API.get("/profile/api/Post/create"),
                    API.get(`/profile/api/Post/edit/${queryParam.id}`)
                ]);

                const postData = postRes.data;

                let previewData = { data: {} };
                if (postData) {
                    previewData = await API.get(`/profile/api/Post/manifest/${queryParam.id}`);
                }

                setCreateModel(createFormRes.data);

                setPostForm((prev) => ({
                    ...prev,
                    id: postData.id,
                    type: postData.type || 1,
                    title: postData.title || "",
                    description: postData.description || "",
                    visibility: postData.visibility !== undefined ? postData.visibility : 1,
                    videoUrl: postData.videoUrl || "",
                    thumbnailUrl: previewData.data?.previewUrl || "",
                    vidoeObjectName: previewData.data?.objectName || ""
                }));

            } catch (err) {
                console.error("Ошибка при загрузке данных:", err);
                setError("Не удалось загрузить данные поста.");
            } finally {
                setIsLoading(false);
            }
        };

        fetchData();
    }, [queryParam.id]);

    const updateForm = (event) => {
        const key = event.target.name;
        const value = event.target.value;
        setPostForm((prev) => ({
            ...prev,
            [key]: value
        }));
    };

    const handleThumbnailChange = (e) => {
        const file = e.target.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = (e) => {
                setPostForm(prev => ({
                    ...prev,
                    thumbnailFile: file,
                    thumbnailUrl: e.target.result // Предпросмотр
                }));
            };
            reader.readAsDataURL(file);
        }
    };

    const handleUpdatePost = async () => {
        const url = `/profile/api/Post/edit`;
        const formData = new FormData();

        // Добавляем текстовые поля
        Object.keys(postForm).forEach((key) => {
            if (
                key !== "videoUrl" &&
                key !== "thumbnailUrl" &&
                key !== "vidoeObjectName" &&
                key !== "thumbnailFile"
            ) {
                formData.append(key, postForm[key]);
            }
        });

        // Если есть новый файл обложки — добавляем его
        if (postForm.thumbnailFile) {
            formData.append("previewId", postForm.thumbnailFile);
        }

        formData.append('id',queryParam.id);

        try {
            const response = await API.post(url, formData, {
                headers: {
                    'Content-Type': 'multipart/form-data'
                }
            });

            if (response.status === 200) {
                navigate('/profile');
            }
        } catch (error) {
            console.error("Ошибка при обновлении поста:", error);
            alert("Произошла ошибка при сохранении изменений.");
        }
    };

    if (isLoading) {
        return (
            <div className="modal">
                <div className="loading">⏳ Загрузка данных...</div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="modal">
                <div className="error">
                    <h2>Ошибка</h2>
                    <p>{error}</p>
                    <button onClick={() => navigate(-1)} className="btn btnPrimary">Назад</button>
                </div>
            </div>
        );
    }

    return (
        <div className="modal">
            <div className="createPostForm">
                <h1>Редактировать видео-пост</h1>

                {/* Название */}
                <div className="formGroup">
                    <label>Название</label>
                    <input
                        className="modalContent"
                        type="text"
                        placeholder="Добавьте название вашего видео"
                        name="title"
                        value={postForm.title}
                        onChange={updateForm}
                    />
                </div>

                {/* Заставка с возможностью редактирования */}
                <div className="formGroup">
                    <label>Заставка видео</label>
                    <div className="thumbnailContainer">
                        {postForm.thumbnailUrl ? (
                            <img
                                src={postForm.thumbnailUrl}
                                alt="Заставка видео"
                                className="thumbnailImage"
                            />
                        ) : (
                            <div className="thumbnailPlaceholder">
                                <span>🖼️</span>
                                <p>Заставка не доступна</p>
                            </div>
                        )}
                    </div>

                    {/* Кнопка выбора файла */}
                    <div style={{ marginTop: '10px' }}>
                        <input
                            type="file"
                            accept="image/*"
                            hidden
                            id="thumbnailInput"
                            onChange={handleThumbnailChange}
                        />
                        <label htmlFor="thumbnailInput" className="btn btnSecondary">
                            Выбрать новую заставку
                        </label>
                    </div>
                </div>

                {/* Видео через VideoPlayer */}
                <div className="formGroup">
                    <label>Видео</label>
                    <div className="videoContainer">
                        {postForm.vidoeObjectName && postForm.id ? (
                            <VideoPlayer
                                key={postForm.id}
                                path={{
                                    label: '',
                                    
                                    postId: postForm.id,
                                    autoplay: false,
                                    objectName: postForm.vidoeObjectName
                                }}
                            />
                        ) : (
                            <div className="videoPlaceholder">
                                <span>🎥</span>
                                <p>Видео не доступно</p>
                            </div>
                        )}
                    </div>
                </div>

                <div className="notice">
                    Видео нельзя изменить после загрузки
                </div>

                {/* Описание */}
                <div className="formGroup">
                    <label>Описание</label>
                    <textarea
                        className="modalContent description"
                        rows="4"
                        placeholder="Добавьте описание к вашему видео"
                        name="description"
                        value={postForm.description}
                        onChange={updateForm}
                    />
                </div>

                {/* Приватность */}
                <div className="formGroup">
                    <label>Настройки приватности</label>
                    <div className="privacySettings">
                        <select
                            name="visibility"
                            value={postForm.visibility}
                            onChange={updateForm}
                        >
                            {createModel?.visibility?.map((v) => (
                                <option key={v.value} value={v.value}>
                                    {v.text}
                                </option>
                            ))}
                        </select>
                        <span>🔒</span>
                    </div>
                </div>

                {/* Кнопки */}
                <div className="actionButtons">
                    <button className="btn btnSecondary" onClick={() => navigate('/profile')}>
                        Отменить
                    </button>
                    <button className="btn btnPrimary" onClick={handleUpdatePost}>
                        Сохранить изменения
                    </button>
                </div>
            </div>
        </div>
    );
};

export default EditPostForm;