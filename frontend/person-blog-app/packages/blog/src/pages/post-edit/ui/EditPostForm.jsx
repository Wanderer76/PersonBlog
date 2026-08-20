import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { getProfilePostV2 } from '@/shared/api/generated/profile-post-v2/profile-post-v2';
import {
  DescriptionTextarea,
  PrivacySelect,
  ThumbnailUpload,
  TitleInput
} from '@/features/post-editor';
import styles from '@/pages/post-create/ui/CreatePostForm.module.css';

const MAX_THUMBNAIL_SIZE = 5 * 1024 * 1024;
const profilePostApi = getProfilePostV2();

const EditPostForm = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    title: '',
    description: '',
    visibility: 1,
    thumbnailUrl: '',
    thumbnailFile: null
  });
  const [createModel, setCreateModel] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    const controller = new AbortController();

    const fetchData = async () => {
      if (!id) {
        setError('Не указан идентификатор публикации');
        setIsLoading(false);
        return;
      }

      try {
        const [formConfig, postData] = await Promise.all([
          profilePostApi.getApiProfilePostV2Create(),
          profilePostApi.getApiProfilePostV2EditPostId(id)
        ]);

        if (controller.signal.aborted) return;
        setCreateModel(formConfig.data);
        setFormData({
          title: postData.data.title || '',
          description: postData.data.description || '',
          visibility: postData.data.visibility ?? 1,
          thumbnailUrl: postData.data.previewUrl || '',
          thumbnailFile: null
        });
      } catch (requestError) {
        if (!controller.signal.aborted) {
          console.error('Ошибка загрузки данных:', requestError);
          setError('Не удалось загрузить данные публикации');
        }
      } finally {
        if (!controller.signal.aborted) setIsLoading(false);
      }
    };

    void fetchData();
    return () => controller.abort();
  }, [id]);

  const handleInputChange = (event) => {
    const { name, value } = event.target;
    setFormData(previous => ({ ...previous, [name]: value }));
  };

  const handleThumbnailChange = (event) => {
    const file = event.target.files?.[0];
    if (!file) return;

    if (file.size > MAX_THUMBNAIL_SIZE) {
      setError('Размер изображения не должен превышать 5 МБ');
      event.target.value = '';
      return;
    }

    if (!file.type.startsWith('image/')) {
      setError('Выберите файл изображения');
      event.target.value = '';
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      setError(null);
      setFormData(previous => ({
        ...previous,
        thumbnailUrl: typeof reader.result === 'string' ? reader.result : previous.thumbnailUrl,
        thumbnailFile: file
      }));
    };
    reader.readAsDataURL(file);
  };

  const handleUpdatePost = async () => {
    if (!id || isSubmitting) return;
    if (!formData.title.trim()) {
      setError('Добавьте название видео');
      return;
    }

    setError(null);
    setIsSubmitting(true);

    try {
      await profilePostApi.postApiProfilePostV2Edit({
        Id: id,
        Title: formData.title.trim(),
        Description: formData.description,
        Visibility: Number(formData.visibility),
        Preview: formData.thumbnailFile ?? undefined
      });
      navigate('/profile');
    } catch (requestError) {
      console.error('Ошибка обновления:', requestError);
      setError('Не удалось сохранить изменения. Попробуйте ещё раз.');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isLoading) {
    return (
      <main className={styles.pageShell}>
        <section className={`${styles.formCard} ${styles.formState}`} aria-live="polite">
          <span className={styles.loadingSpinner} aria-hidden="true" />
          <p>Загружаем публикацию…</p>
        </section>
      </main>
    );
  }

  if (error && !createModel) {
    return (
      <main className={styles.pageShell}>
        <section className={`${styles.formCard} ${styles.formState}`} role="alert">
          <h1>Не удалось открыть публикацию</h1>
          <p>{error}</p>
          <button className={`${styles.btn} ${styles.btnPrimary}`} type="button" onClick={() => navigate('/profile')}>
            Вернуться в профиль
          </button>
        </section>
      </main>
    );
  }

  return (
    <main className={styles.pageShell}>
      <section className={styles.formCard} aria-labelledby="edit-video-title">
        <header className={styles.formHeader}>
          <div>
            <span className={styles.eyebrow}>Настройки публикации</span>
            <h1 id="edit-video-title">Редактирование видео</h1>
            <p>Обновите информацию, обложку и параметры доступа.</p>
          </div>
          <button className={styles.closeButton} type="button" onClick={() => navigate('/profile')}
            aria-label="Закрыть форму">×</button>
        </header>

        {error && <div className={styles.errorBanner} role="alert">{error}</div>}

        <div className={styles.formBody}>
          <section className={styles.mediaColumn} aria-label="Обложка видео">
            <div className={styles.sectionHeading}>
              <span className={styles.stepNumber}>1</span>
              <div><h2>Обложка видео</h2><p>Изображение до 5 МБ</p></div>
            </div>
            <div className={styles.editThumbnail}>
              <ThumbnailUpload thumbnail={formData.thumbnailUrl} onChange={handleThumbnailChange} />
            </div>
            <p className={styles.editMediaHint}>Используйте изображение формата 16:9 — оно лучше выглядит в ленте и на странице видео.</p>
          </section>

          <section className={styles.detailsColumn} aria-label="Информация о публикации">
            <div className={styles.sectionHeading}>
              <span className={styles.stepNumber}>2</span>
              <div><h2>О публикации</h2><p>Название, описание и параметры доступа</p></div>
            </div>
            <TitleInput value={formData.title} onChange={handleInputChange}
              placeholder="Название видео" />
            <DescriptionTextarea value={formData.description} onChange={handleInputChange}
              placeholder="Расскажите, о чём это видео" />
            <PrivacySelect options={createModel?.visibility ?? []} value={formData.visibility}
              onChange={handleInputChange} />
          </section>
        </div>

        <footer className={styles.actionBar}>
          <p>Изменения будут видны зрителям после сохранения.</p>
          <div className={styles.actionButtons}>
            <button className={`${styles.btn} ${styles.btnSecondary}`} type="button"
              onClick={() => navigate('/profile')} disabled={isSubmitting}>Отмена</button>
            <button className={`${styles.btn} ${styles.btnPrimary}`} type="button"
              onClick={() => void handleUpdatePost()} disabled={isSubmitting}>
              {isSubmitting ? 'Сохраняем…' : 'Сохранить изменения'}
            </button>
          </div>
        </footer>
      </section>
    </main>
  );
};

export default EditPostForm;
