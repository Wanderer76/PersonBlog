import React, { useEffect, useState } from "react";
import styles from './CreatePostForm.module.css';
import API from "../../../scripts/apiMethod";
import { useNavigate, useParams } from "react-router-dom";
import VideoPlayer from "../../VideoPlayer/VideoPlayer";
import {
  TitleInput,
  ThumbnailEdit,
  DescriptionTextarea,
  PrivacySelect,
  ActionButtons,
  ThumbnailUpload
} from "./CommonComponents";

const EditPostForm = () => {
  const { id } = useParams();
  const navigate = useNavigate();

  const [formData, setFormData] = useState({
    title: "",
    description: "",
    visibility: 1,
    thumbnailUrl: ""
  });

  const [videoInfo, setVideoInfo] = useState({
    objectName: ""
  });

  const [createModel, setCreateModel] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setIsLoading(true);
        const [formConfig, postData] = await Promise.all([
          API.get("/profile/api/Post/create"),
          API.get(`/profile/api/Post/edit/${id}`)
        ]);

        const previewData = await API.get(`/profile/api/Post/manifest/${id}`);

        setCreateModel(formConfig.data);
        setFormData({
          title: postData.data.title || "",
          description: postData.data.description || "",
          visibility: postData.data.visibility ?? 1,
          thumbnailUrl: previewData.data?.previewUrl || ""
        });

        setVideoInfo({
          objectName: previewData.data?.objectName || ""
        });

      } catch (err) {
        console.error("Ошибка загрузки данных:", err);
        setError("Не удалось загрузить данные поста");
      } finally {
        setIsLoading(false);
      }
    };

    fetchData();
  }, [id]);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleThumbnailChange = (e) => {
    const file = e.target.files[0];
    if (!file) return;

    // Проверка размера файла (макс. 5MB)
    if (file.size > 5 * 1024 * 1024) {
      alert("Размер изображения должен быть меньше 5MB");
      return;
    }

    const reader = new FileReader();
    reader.onload = (e) => {
      setFormData(prev => ({
        ...prev,
        thumbnailUrl: e.target.result,
        thumbnailFile: file
      }));
    };
    reader.readAsDataURL(file);
  };

  const handleUpdatePost = async () => {
    if (!formData.title.trim()) {
      alert("Пожалуйста, добавьте название видео");
      return;
    }

    if (isSubmitting) return;
    setIsSubmitting(true);

    try {
      const payload = new FormData();
      payload.append('id', id);
      payload.append('title', formData.title);
      payload.append('description', formData.description);
      payload.append('visibility', formData.visibility);

      if (formData.thumbnailFile) {
        payload.append("previewId", formData.thumbnailFile);
      }

      const response = await API.post("/profile/api/Post/edit", payload, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });

      if (response.status === 200) {
        navigate('/profile');
      }
    } catch (error) {
      console.error("Ошибка обновления:", error);
      alert("Произошла ошибка при сохранении изменений");
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isLoading) {
    return (
      <div className="modal loading-modal">
        <div className="loading-spinner"></div>
        <p>Загрузка данных...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="modal error-modal">
        <div className="error-content">
          <h2>Ошибка</h2>
          <p>{error}</p>
          <button
            onClick={() => navigate(-1)}
            className="btn btnPrimary"
          >
            Назад
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.modal}>
      <div className={styles.createPostForm}>
        <h1>Редактировать видео-пост</h1>

        <TitleInput
          value={formData.title}
          onChange={handleInputChange}
          placeholder="Добавьте название вашего видео"
        />

        <ThumbnailUpload
          thumbnail={formData.thumbnailUrl}
          onChange={handleThumbnailChange}
        />

        <div className={styles.formGroup}>
          <label>Видео</label>
          <div className={styles.videoPreview}>
            {videoInfo.objectName ? (
              <VideoPlayer
                key={id}
                path={{
                  postId: id,
                  autoplay: false,
                  objectName: videoInfo.objectName
                }}
              />
            ) : (
              <div className={styles.cameraIcon}>
                <span>🎥</span>
                <p>Видео не доступно</p>
              </div>
            )}
          </div>
        </div>

        <DescriptionTextarea
          value={formData.description}
          onChange={handleInputChange}
          placeholder="Добавьте описание к вашему видео"
        />

        <PrivacySelect
          options={createModel?.visibility}
          value={formData.visibility}
          onChange={handleInputChange}
        />

        <ActionButtons
          onCancel={() => navigate('/profile')}
          onSubmit={handleUpdatePost}
          cancelText="Отменить"
          submitText="Сохранить изменения"
          isSubmitting={isSubmitting}
        />
      </div>
    </div>
  );
};

export default EditPostForm;