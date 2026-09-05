import { useEffect, useRef, useState } from 'react';
import type { ChangeEvent, ComponentType } from 'react';
import { useNavigate } from 'react-router-dom';
import styles from '@/pages/post-create/ui/CreatePostForm.module.css';
import { cancelBackgroundUpload, enqueueVideo } from '@/shared/lib/upload/backgroundUpload';
import { getProfilePostV2 } from '@/shared/api/generated/profile-post-v2/profile-post-v2';
import type { CategoryModel, CreatePostModelViewModel, PostVisibility } from '@/shared/api/generated/models';
import {
  CategoryMultiSelect,
  DescriptionTextarea,
  PrivacySelect,
  ThumbnailUpload,
  TitleInput
} from '@/features/post-editor';

const TypedCategoryMultiSelect = CategoryMultiSelect as ComponentType<{
  options: CategoryModel[];
  value: number[];
  onChange: (event: { target: { value: number[] } }) => void;
}>;

const profilePostApi = getProfilePostV2();

interface PostForm {
  title: string;
  visibility: PostVisibility;
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

const formatFileSize = (bytes: number) => {
  if (bytes < 1024 * 1024) return `${Math.ceil(bytes / 1024)} КБ`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} МБ`;
};

const formatDuration = (seconds: number) => {
  if (!Number.isFinite(seconds) || seconds <= 0) return 'Определяем длительность…';
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = Math.floor(seconds % 60);
  return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
};

const CreatePostForm = () => {
  const navigate = useNavigate();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const videoRef = useRef<HTMLVideoElement>(null);
  const submittingRef = useRef(false);
  const createdPostIdRef = useRef<string | null>(null);

  const [postForm, setPostForm] = useState<PostForm>({
    title: '',
    visibility: 0,
    video: null,
    videoPostData: { description: '', categories: [], thumbnail: null }
  });
  const [createModel, setCreateModel] = useState<CreatePostModelViewModel>(emptyCreateModel);
  const [videoDuration, setVideoDuration] = useState(0);
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
    profilePostApi.getApiProfilePostV2Create()
      .then(({ data }) => { if (isActive) setCreateModel(data); })
      .catch(() => { if (isActive) setErrorMessage('Не удалось загрузить параметры формы'); });
    return () => { isActive = false; };
  }, []);

  const updateForm = (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    if (submittingRef.current) return;
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
    const { data } = await profilePostApi.postApiProfilePostV2Create({
      Title: postForm.title.trim(),
      Visibility: postForm.visibility,
      'VideoPostData.Description': postForm.videoPostData.description.trim(),
      'VideoPostData.Categories': postForm.videoPostData.categories,
      'VideoPostData.Thumbnail': postForm.videoPostData.thumbnail ?? undefined
    });
    if (!data.id) throw new Error('Сервис не вернул идентификатор созданного поста');
    createdPostIdRef.current = data.id;
    return data.id;
  };

  const sendForm = async () => {
    if (submittingRef.current) return;
    setErrorMessage(null);

    if (!postForm.title.trim()) return setErrorMessage('Введите заголовок видео');
    if (!postForm.video) return setErrorMessage('Выберите видеофайл');
    if (!Number.isFinite(videoDuration) || videoDuration <= 0) {
      return setErrorMessage('Не удалось определить длительность видео');
    }

    submittingRef.current = true;
    setIsSubmitting(true);
    try {
      const postId = createdPostIdRef.current ?? await createPost();
      // Wait only for durable local storage, not for the network upload.
      await enqueueVideo(postId, postForm.video, videoDuration);
      navigate('/profile');
    } catch (error: unknown) {
      setErrorMessage(error instanceof Error ? error.message : 'Не удалось создать публикацию');
    } finally {
      submittingRef.current = false;
      setIsSubmitting(false);
    }
  };

  const handleCancel = async () => {
    if (submittingRef.current) return;
    submittingRef.current = true;
    setIsSubmitting(true);
    try {
      if (createdPostIdRef.current) {
        await cancelBackgroundUpload(createdPostIdRef.current);
        await profilePostApi.postApiProfilePostV2RemovePostId(createdPostIdRef.current);
      }
      navigate('/profile');
    } catch (error: unknown) {
      setErrorMessage(error instanceof Error ? error.message : 'Не удалось отменить создание');
    } finally {
      submittingRef.current = false;
      setIsSubmitting(false);
    }
  };

  const openFilePicker = () => fileInputRef.current?.click();

  return (
    <main className={styles.pageShell}>
      <section className={styles.formCard} aria-labelledby="create-video-title">
        <header className={styles.formHeader}>
          <div>
            <span className={styles.eyebrow}>Новая публикация</span>
            <h1 id="create-video-title">Создание видео</h1>
            <p>Добавьте файл и заполните информацию, которую увидят зрители.</p>
          </div>
          <button className={styles.closeButton} type="button" disabled={isSubmitting} onClick={() => void handleCancel()}
            aria-label="Закрыть форму">×</button>
        </header>

        {errorMessage && <div className={styles.errorBanner} role="alert">{errorMessage}</div>}

        <div className={styles.formBody}>
          <section className={styles.mediaColumn} aria-label="Видео">
            <div className={styles.sectionHeading}>
              <span className={styles.stepNumber}>1</span>
              <div><h2>Видеофайл</h2><p>До 2 ГБ, любой формат video/*</p></div>
            </div>

            {!postForm.video ? (
              <div className={styles.uploadArea} role="button" tabIndex={0} onClick={openFilePicker}
                onKeyDown={event => {
                  if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); openFilePicker(); }
                }}>
                <span className={styles.uploadIcon}>↑</span>
                <strong>Выберите видео</strong>
                <span>или перетащите файл в эту область</span>
              </div>
            ) : (
              <div className={styles.previewContainer}>
                <video className={styles.videoPreview} ref={videoRef} controls preload="metadata"
                  onLoadedMetadata={event => setVideoDuration(event.currentTarget.duration)} />
                <div className={styles.fileDetails}>
                  <div><strong title={postForm.video.name}>{postForm.video.name}</strong>
                    <span>{formatFileSize(postForm.video.size)} · {formatDuration(videoDuration)}</span></div>
                  <button type="button" onClick={openFilePicker} disabled={isSubmitting}>Заменить</button>
                </div>
              </div>
            )}
            <input ref={fileInputRef} name="video" type="file" className={styles.fileInput}
              accept="video/*" hidden onChange={updateForm} />
          </section>

          <section className={styles.detailsColumn} aria-label="Информация о публикации">
            <div className={styles.sectionHeading}>
              <span className={styles.stepNumber}>2</span>
              <div><h2>О публикации</h2><p>Название, обложка и параметры доступа</p></div>
            </div>

            <TitleInput value={postForm.title} onChange={updateForm} placeholder="Например, путешествие по Уралу" />
            <ThumbnailUpload thumbnail={postForm.videoPostData.thumbnail} onChange={updateForm} />
            <DescriptionTextarea value={postForm.videoPostData.description}
              onChange={(event: ChangeEvent<HTMLTextAreaElement>) => setPostForm(previous => ({
                ...previous, videoPostData: { ...previous.videoPostData, description: event.target.value }
              }))} placeholder="Расскажите, о чём это видео" />
            <TypedCategoryMultiSelect options={createModel.categoryList ?? []}
              value={postForm.videoPostData.categories}
              onChange={(event: { target: { value: number[] } }) => setPostForm(previous => ({
                ...previous, videoPostData: { ...previous.videoPostData, categories: event.target.value }
              }))} />
            <PrivacySelect options={createModel.visibility ?? []} value={postForm.visibility} onChange={updateForm} />
          </section>
        </div>

        <footer className={styles.actionBar}>
          <p>{postForm.video ? 'Видео загрузится в фоне. Прогресс будет доступен в профиле.' : 'Сначала выберите видеофайл.'}</p>
          <div className={styles.actionButtons}>
            <button className={`${styles.btn} ${styles.btnSecondary}`} type="button"
              onClick={() => void handleCancel()} disabled={isSubmitting}>Отмена</button>
            <button className={`${styles.btn} ${styles.btnPrimary}`} type="button"
              onClick={() => void sendForm()} disabled={isSubmitting || !postForm.video}>
              {isSubmitting ? 'Подготавливаем…' : createdPostIdRef.current ? 'Повторить отправку' : 'Создать видео'}
            </button>
          </div>
        </footer>
      </section>
    </main>
  );
};

export default CreatePostForm;
