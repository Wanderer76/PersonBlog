import React, { useEffect, useRef, useState } from "react";
import styles from './CreatePostForm.module.css';
import API from "../../../lib/api/client";
import { useNavigate } from "react-router-dom";
import { DirectFileUploader, type InitiateUploadResponse } from "../../../service/DirectFileUploader";
import {
  TitleInput,
  ThumbnailUpload,
  DescriptionTextarea,
  PrivacySelect,
  ActionButtons,
  CategoryMultiSelect
} from "./CommonComponents";

interface GetPostCreate {
  subscriptionLevels: any[];
  visibility: any[];
  categoryList: any[];
}

interface VideoPostData {
  description: string | null;
  categories: number[];
  thumbnail: File | null;
}

interface PostForm {
  type: number;
  title: string;
  videoPostData: VideoPostData;
  visibility: number;
  video: File | null;
}

const CreatePostForm = () => {
  const [postForm, setPostForm] = useState<PostForm>({
    type: 1,
    title: "",
    videoPostData: {
      description: null,
      categories: [],
      thumbnail: null
    },
    visibility: 0,
    video: null
  });

  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [createModel, setCreateModel] = useState<GetPostCreate>({});
  const videoRef = useRef<HTMLVideoElement>(null);
  const navigate = useNavigate();
  const uploaderRef = useRef<DirectFileUploader | null>(null);

  // Автоматическая загрузка видео при выборе
  useEffect(() => {
    if (postForm.video && videoRef.current) {
      const videoURL = URL.createObjectURL(postForm.video);
      videoRef.current.src = videoURL;
      videoRef.current.load();

      return () => URL.revokeObjectURL(videoURL);
    }
  }, [postForm.video]);

  // Загрузка данных для формы
  useEffect(() => {
    API.get("/profile/api/ProfilePostV2/create")
      .then(response => setCreateModel(response.data))
      .catch(error => console.error("Ошибка загрузки данных:", error));
  }, []);

  // Обновление формы
  const updateForm = (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value, files } = event.target;

    if (name === 'video' && files?.[0]) {
      const file = files[0];

      // Валидация видео
      if (file.size > 2 * 1024 * 1024 * 1024) {
        alert("Файл слишком большой. Максимальный размер 2GB");
        return;
      }

      if (!file.type.startsWith('video/')) {
        alert("Пожалуйста, выберите видео файл");
        return;
      }

      setPostForm(prev => ({ ...prev, video: file }));
    } else {
      setPostForm(prev => ({ ...prev, [name]: value }));
    }
  };

  // Обновление описания
  const updateDescription = (value: string) => {
    setPostForm(prev => ({
      ...prev,
      videoPostData: { ...prev.videoPostData, description: value }
    }));
  };

  // Обновление категорий
  const updateCategories = (categories: number[]) => {
    setPostForm(prev => ({
      ...prev,
      videoPostData: { ...prev.videoPostData, categories }
    }));
  };

  // Отправка формы
  const sendForm = async () => {
  if (!postForm.title.trim()) {
    alert("Пожалуйста, добавьте название видео");
    return;
  }

  if (isSubmitting) return;
  setIsSubmitting(true);

  try {
    const formData = new FormData();
    
    // Основные поля
    formData.append('type', postForm.type.toString());
    formData.append('title', postForm.title.trim());
    formData.append('visibility', postForm.visibility.toString());

    // Поля videoPostData
    // Описание: преобразуем null в пустую строку для корректной отправки
    const descriptionValue = postForm.videoPostData.description ?? '';
    formData.append('videoPostData[description]', descriptionValue.trim());

    // Категории: отправляем каждый ID отдельно с именем массива
    postForm.videoPostData.categories.forEach(categoryId => {
      formData.append('videoPostData[categories][]', categoryId.toString());
    });

    // Превью: добавляем файл только если это экземпляр File
    if (postForm.videoPostData.thumbnail instanceof File) {
      formData.append(
        'videoPostData[thumbnail]',
        postForm.videoPostData.thumbnail,
        postForm.videoPostData.thumbnail.name
      );
    }

    // ВАЖНО: Не устанавливаем заголовок 'Content-Type' вручную!
    // Браузер автоматически установит правильный boundary для multipart/form-data
    const response = await API.post("/profile/api/ProfilePostV2/create", formData);

    if (response.status === 200 && response.data?.id) {
      const postId = response.data.id;

      // Загружаем видео отдельно, если оно выбрано
      if (postForm.video) {
        await uploadFile(postId, postForm.video);
      }
    }

    navigate('/profile');
  } catch (error) {
    console.error("Ошибка создания поста:", error);
    alert("Произошла ошибка при создании поста. Проверьте заполнение полей и попробуйте снова.");
  } finally {
    setIsSubmitting(false);
  }
};

  // Загрузка видео файла
  const uploadFile = async (postId: string, file: File) => {
    try {
      const uploader = new DirectFileUploader(5 * 1024 * 1024);
      uploaderRef.current = uploader;

      uploader.setProgressCallback(setUploadProgress);
      const session: InitiateUploadResponse = await uploader.initiateUpload(
        postId,
        videoRef.current!.duration!,
        file
      );

      console.log('Upload initiated:', session.uploadId);
      await uploader.uploadFile(file);
      console.log('Upload completed successfully!');

    } catch (error) {
      console.error('Upload failed:', error);

      if (uploaderRef.current) {
        await uploaderRef.current.abortUpload().catch(console.error);
      }
      throw error;
    }
  };

  // Отмена создания
  const handleCancel = () => {
    const isUploading = uploadProgress > 0 && uploadProgress < 100;

    if (isUploading && !confirm('Загрузка еще не завершена. Отменить?')) {
      return;
    }

    if (uploaderRef.current && isUploading) {
      uploaderRef.current.abortUpload().catch(console.error);
    }

    navigate('/profile');
  };

  // Выбор файла через клик
  const triggerFileInput = () => {
    fileInputRef.current?.click();
  };

  return (
    <div className={styles.modal}>
      <div className={styles.createPostForm}>
        <h1>Создать видео-пост</h1>

        <TitleInput
          value={postForm.title}
          onChange={updateForm}
          placeholder="Добавьте название вашего видео"
        />

        <ThumbnailUpload
          thumbnail={postForm.videoPostData.thumbnail}
          onChange={updateForm}
        />

        <div className={styles.formGroup}>
          <label>Видео</label>
          <div className={styles.uploadArea} onClick={triggerFileInput}>
            <div className={styles.cameraIcon}>🎥</div>
            <h3>Выберите файл для загрузки</h3>
            <p>или перетащите видео файл</p>
            <input
              ref={fileInputRef}
              name="video"
              type="file"
              className={`${styles.videoInput} ${styles.fileInput}`}
              accept="video/*"
              hidden
              onChange={updateForm}
            />
          </div>
        </div>

        {postForm.video && (
          <div className={styles.previewContainer}>
            <video
              className={styles.videoPreview}
              ref={videoRef}
              controls
              preload="metadata"
            />

            {uploadProgress > 0 && (
              <div className={styles.uploadStatus}>
                <div className={styles.progressBar}>
                  <div
                    className={styles.progressFill}
                    style={{ width: `${uploadProgress}%` }}
                  />
                </div>
                <span className={styles.progressText}>
                  {uploadProgress}% загружено
                </span>
              </div>
            )}
          </div>
        )}

        <DescriptionTextarea
          value={postForm.videoPostData.description}
          onChange={(e) => updateDescription(e.target.value)}
          placeholder="Добавьте описание к вашему видео"
        />

        <CategoryMultiSelect
          options={createModel.categoryList || []}
          value={postForm.videoPostData.categories}
          onChange={(e) => updateCategories(e.target.value)}
        />

        <PrivacySelect
          options={createModel.visibility || []}
          value={postForm.visibility}
          onChange={updateForm}
        />

        <ActionButtons
          onCancel={handleCancel}
          onSubmit={sendForm}
          cancelText="Закрыть"
          submitText={uploadProgress > 0 && uploadProgress < 100 ? "Загрузка..." : "Создать"}
          isSubmitting={isSubmitting || (uploadProgress > 0 && uploadProgress < 100)}
        />
      </div>
    </div>
  );
};

export default CreatePostForm;