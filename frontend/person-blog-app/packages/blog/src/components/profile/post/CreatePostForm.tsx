import React, { useEffect, useRef, useState } from "react";
import styles from './CreatePostForm.module.css';
import API from "../../../scripts/apiMethod";
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

interface VideoPostCreateForm {
  description: string | null;
  categories: Number[];
  thumbnail: File | null;
}

interface VideoPostCreateRequest {
  title: string;
  type: Number;
  visibility: Number;
  videoPostData: VideoPostCreateForm;
}

const CreatePostForm = function () {
  const [postForm, setPostForm] = useState({
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

  useEffect(() => {
    API.get("/profile/api/PostV2/create")
      .then(response => setCreateModel(response.data));
  }, []);

  function updateForm(event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement> | { target: { name: string; value: any } }) {
    const target = 'target' in event ? event.target : event;
    const key = target.name;

    let value: any;
    if ('files' in target && target.files) {
      value = target.files[0];
    } else {
      value = target.value;
    }

    if (key) {
      setPostForm(prev => ({ ...prev, [key]: value }));
    }
  }

  async function sendForm() {
    if (!postForm.title.trim()) {
      alert("Пожалуйста, добавьте название видео");
      return;
    }

    if (isSubmitting) return;
    setIsSubmitting(true);

    try {
      const url = "/profile/api/PostV2/create";

      // formData.append('type', postForm.type.toString());
      // formData.append('title', postForm.title);
      // if (postForm.videoPostData.description !== null)
      //   formData.append('description', postForm.videoPostData.description);

      // if (postForm.videoPostData.thumbnail) {
      //   formData.append('thumbnail', postForm.videoPostData.thumbnail);
      // }

      // postForm.videoPostData.categories.forEach(id => {
      //   formData.append('categories[]', id.toString());
      // });

      // formData.append('visibility', postForm.visibility.toString());

      const response = await API.post(url, {
        title: postForm.title,
        videoPostData: {
          categories: postForm.videoPostData.categories,
          description: postForm.videoPostData.description,
          thumbnail: postForm.videoPostData.thumbnail
        },
        type: 1,
        visibility: postForm.visibility

      });

      if (response.status === 200) {
        const postId = response.data.id;

        if (postForm.video) {
          await uploadFile(postId, postForm.video);
        }
      }

      navigate('/profile');
    } catch (error) {
      console.error("Ошибка создания поста:", error);
      alert("Произошла ошибка при создании поста");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function uploadFile(postId: string, file: File) {
    try {
      // Создаем загрузчик
      const uploader = new DirectFileUploader(5 * 1024 * 1024); // 5MB chunks
      uploaderRef.current = uploader;

      // Устанавливаем колбэк для прогресса
      uploader.setProgressCallback((progress) => {
        setUploadProgress(progress);
      });

      // Инициируем загрузку
      const session: InitiateUploadResponse = await uploader.initiateUpload(postId, videoRef.current!.duration!,  file);

      console.log('Upload initiated:', session.uploadId);

      // Загружаем файл напрямую в MinIO
      await uploader.uploadFile(file);

      console.log('Upload completed successfully!');

      // // Сохраняем информацию о файле в посте
      // await API.post(`/profile/api/PostV2/${postId}/attach-video`, {
      //   fileId: session.uploadId,
      //   fileName: file.name,
      //   fileSize: file.size,
      //   fileUrl: session.objectName
      // });

    } catch (error) {
      console.error('Upload failed:', error);

      // Отменяем загрузку при ошибке
      if (uploaderRef.current) {
        await uploaderRef.current.abortUpload().catch(console.error);
      }

      throw error;
    }
  }

  function handleFileSelect(input: React.ChangeEvent<HTMLInputElement>) {
    const file = input.target.files?.[0];
    if (!file) return;

    // Проверка размера файла (макс. 2GB для видео)
    if (file.size > 2 * 1024 * 1024 * 1024) {
      alert("Файл слишком большой. Максимальный размер 2GB");
      return;
    }

    // Проверка типа файла
    if (!file.type.startsWith('video/')) {
      alert("Пожалуйста, выберите видео файл");
      return;
    }

    const videoURL = URL.createObjectURL(file);
    if (videoRef.current) {
      videoRef.current.src = videoURL;
      videoRef.current.load();
    }
  }

  function handleCancel() {
    // Отменяем загрузку при закрытии
    if (uploaderRef.current && uploadProgress > 0 && uploadProgress < 100) {
      if (confirm('Загрузка еще не завершена. Отменить?')) {
        uploaderRef.current.abortUpload().catch(console.error);
        navigate('/profile');
      }
    } else {
      navigate('/profile');
    }
  }

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
          <div
            className={styles.uploadArea}
            onClick={() => fileInputRef.current?.click()}
          >
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
              onChange={(e) => {
                updateForm(e);
                handleFileSelect(e);
              }}
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
          onChange={(e) => {
            const target = 'target' in e ? e.target : e;
            setPostForm(prev => ({
              ...prev,
              videoPostData: {
                ...prev.videoPostData,
                description: target.value
              }
            }));

          }}
          placeholder="Добавьте описание к вашему видео"
        />

        <CategoryMultiSelect
          options={createModel?.categoryList || []}
          value={postForm.videoPostData.categories || []}
          onChange={(e) => {
            const target = 'target' in e ? e.target : e;

            console.log(target.value)

            setPostForm(prev => ({
              ...prev,
              videoPostData: {
                ...prev.videoPostData,
                categories: target.value
              }
            }));

          }}
        />

        <PrivacySelect
          options={createModel?.visibility || []}
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
}

export default CreatePostForm;