import { useEffect, useRef, useState } from 'react';
import type { ChangeEvent, ComponentType } from 'react';
import { useNavigate } from 'react-router-dom';
import styles from './CreatePostForm.module.css';
import API from '../../../lib/api/client';
import { DirectFileUploader } from '../../../shared/DirectFileUploader';
import type { CategoryModel, CreatePostModelViewModel } from '@/lib/api/generated/models';
import {
  CategoryMultiSelect,
  DescriptionTextarea,
  PrivacySelect,
  ThumbnailUpload,
  TitleInput
} from './CommonComponents';

const TypedCategoryMultiSelect = CategoryMultiSelect as ComponentType<{
  options: CategoryModel[];
  value: number[];
  onChange: (event: { target: { value: number[] } }) => void;
}>;

interface PostForm {
  title: string;
  visibility: number;
  video: File | null;
  videoPostData: {
    description: string;
    categories: number[];
    thumbnail: File | null;
  };
}

const emptyCreateModel: CreatePostModelViewModel = {
  subscriptionLevels: [],
  visibility: [],
  categoryList: []
};

const CreatePostForm = () => {
  const navigate = useNavigate();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const videoRef = useRef<HTMLVideoElement>(null);
  const uploaderRef = useRef<DirectFileUploader | null>(null);
  const createdPostIdRef = useRef<string | null>(null);

  const [postForm, setPostForm] = useState<PostForm>({
    title: '',
    visibility: 0,
    video: null,
    videoPostData: { description: '', categories: [], thumbnail: null }
  });
  const [createModel, setCreateModel] = useState<CreatePostModelViewModel>(emptyCreateModel);
  const [videoDuration, setVideoDuration] = useState(0);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!postForm.video || !videoRef.current) return;
    const videoUrl = URL.createObjectURL(postForm.video);
    videoRef.current.src = videoUrl;
    videoRef.current.load();
    return () => URL.revokeObjectURL(videoUrl);
  }, [postForm.video]);

  useEffect(() => {
    let isActive = true;
    API.get<CreatePostModelViewModel>('/profile/api/ProfilePostV2/create')
      .then(({ data }) => { if (isActive) setCreateModel(data); })
      .catch(() => { if (isActive) setErrorMessage('Не удалось загрузить параметры формы'); });
    return () => { isActive = false; };
  }, []);

  const updateForm = (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const target = event.currentTarget;
    const file = target instanceof HTMLInputElement ? target.files?.[0] : undefined;

    if (target.name === 'video' && file) {
      if (file.size > 2 * 1024 * 1024 * 1024) {
        setErrorMessage('Максимальный размер видео — 2 ГБ');
        return;
      }
      if (!file.type.startsWith('video/')) {
        setErrorMessage('Выберите видеофайл');
        return;
      }
      setVideoDuration(0);
      setUploadProgress(0);
      setPostForm(previous => ({ ...previous, video: file }));
      return;
    }

    if (target.name === 'thumbnail' && file) {
      setPostForm(previous => ({
        ...previous,
        videoPostData: { ...previous.videoPostData, thumbnail: file }
      }));
      return;
    }

    setPostForm(previous => ({
      ...previous,
      [target.name]: target.name === 'visibility' ? Number(target.value) : target.value
    }));
  };

  const createPost = async () => {
    const formData = new FormData();
    formData.append('Title', postForm.title.trim());
    formData.append('Visibility', postForm.visibility.toString());
    formData.append('VideoPostData.Description', postForm.videoPostData.description.trim());
    postForm.videoPostData.categories.forEach(categoryId => {
      formData.append('VideoPostData.Categories', categoryId.toString());
    });
    if (postForm.videoPostData.thumbnail) {
      formData.append('VideoPostData.Thumbnail', postForm.videoPostData.thumbnail);
    }

    const { data } = await API.post<{ id?: string }>('/profile/api/ProfilePostV2/create', formData);
    if (!data.id) throw new Error('Сервис не вернул идентификатор созданного поста');
    createdPostIdRef.current = data.id;
    return data.id;
  };

  const uploadVideo = async (postId: string, file: File) => {
    const uploader = new DirectFileUploader();
    uploaderRef.current = uploader;
    uploader.setProgressCallback(setUploadProgress);
    await uploader.initiateUpload(postId, videoDuration, file);
    await uploader.uploadFile(file);
  };

  const sendForm = async () => {
    if (isSubmitting) return;
    setErrorMessage(null);

    if (!postForm.title.trim()) return setErrorMessage('Введите заголовок видео');
    if (!postForm.video) return setErrorMessage('Выберите видеофайл');
    if (!Number.isFinite(videoDuration) || videoDuration <= 0) {
      return setErrorMessage('Не удалось определить длительность видео');
    }

    setIsSubmitting(true);
    try {
      const postId = createdPostIdRef.current ?? await createPost();
      await uploadVideo(postId, postForm.video);
      navigate('/profile');
    } catch (error: unknown) {
      if (uploaderRef.current) await uploaderRef.current.abortUpload().catch(() => undefined);
      setErrorMessage(error instanceof Error ? error.message : 'Не удалось создать публикацию');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCancel = async () => {
    const isUploading = isSubmitting && uploadProgress < 100;
    if (isUploading && !window.confirm('Загрузка не завершена. Отменить её?')) return;
    if (uploaderRef.current) await uploaderRef.current.abortUpload().catch(() => undefined);
    if (createdPostIdRef.current) {
      await API.post(`/profile/api/ProfilePostV2/remove/${createdPostIdRef.current}`).catch(() => undefined);
    }
    navigate('/profile');
  };

  return (
    <div className={styles.modal}>
      <div className={styles.createPostForm}>
        <h1>Создать видео</h1>
        {errorMessage && <p role="alert">{errorMessage}</p>}

        <TitleInput value={postForm.title} onChange={updateForm} placeholder="Название видео" />
        <ThumbnailUpload thumbnail={postForm.videoPostData.thumbnail} onChange={updateForm} />

        <div className={styles.formGroup}>
          <label>Видео</label>
          <div className={styles.uploadArea} onClick={() => fileInputRef.current?.click()}>
            <div className={styles.cameraIcon}>🎥</div>
            <h3>Выберите видеофайл</h3>
            <input ref={fileInputRef} name="video" type="file"
              className={`${styles.videoInput} ${styles.fileInput}`} accept="video/*" hidden onChange={updateForm} />
          </div>
        </div>

        {postForm.video && (
          <div className={styles.previewContainer}>
            <video className={styles.videoPreview} ref={videoRef} controls preload="metadata"
              onLoadedMetadata={event => setVideoDuration(event.currentTarget.duration)} />
            {uploadProgress > 0 && (
              <div className={styles.uploadStatus}>
                <div className={styles.progressBar}>
                  <div className={styles.progressFill} style={{ width: `${uploadProgress}%` }} />
                </div>
                <span className={styles.progressText}>{uploadProgress}% загружено</span>
              </div>
            )}
          </div>
        )}

        <DescriptionTextarea value={postForm.videoPostData.description}
          onChange={(event: ChangeEvent<HTMLTextAreaElement>) => setPostForm(previous => ({
            ...previous,
            videoPostData: { ...previous.videoPostData, description: event.target.value }
          }))} placeholder="Описание видео" />

        <TypedCategoryMultiSelect options={createModel.categoryList ?? []} value={postForm.videoPostData.categories}
          onChange={(event: { target: { value: number[] } }) => setPostForm(previous => ({
            ...previous,
            videoPostData: { ...previous.videoPostData, categories: event.target.value }
          }))} />

        <PrivacySelect options={createModel.visibility ?? []} value={postForm.visibility} onChange={updateForm} />
        <div className={styles.actionButtons}>
          <button className={`${styles.btn} ${styles.btnSecondary}`} onClick={() => void handleCancel()}>
            Закрыть
          </button>
          <button className={`${styles.btn} ${styles.btnPrimary}`} onClick={() => void sendForm()} disabled={isSubmitting}>
            {isSubmitting ? 'Загрузка...' : createdPostIdRef.current ? 'Повторить загрузку' : 'Создать'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default CreatePostForm;
