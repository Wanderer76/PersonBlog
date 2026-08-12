import { useState, useEffect } from 'react';
import type { ChangeEvent, FormEvent } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Button } from '@/shared/ui/Button/Button';
import { PrivacySelect, TitleInput } from './CommonComponents';
import { PostVisibility } from '@/lib/api/generated/models';
import type { PostVisibilitySelectItem, TextPostMediaViewModel } from '@/lib/api/generated/models';
import { getTextPost } from '@/lib/api/generated/text-post/text-post';
import { MediaUploader } from '@/features/post-management/components/MediaUploader/MediaUploader';
import { RichTextEditor } from '@/features/post-management/components/RichTextEditor/RichTextEditor';
import { hasErrors, validateForm } from './CreateTextPostForm.validation';
import type { FormErrors, TextPostFormData } from './CreateTextPostForm.validation';
import './CreateTextPostForm.css';

const textPostApi = getTextPost();

const CreateTextPostForm = () => {
    const navigate = useNavigate();
    const { id: postId } = useParams<{ id: string }>();
    const isEditing = Boolean(postId);
    const [formData, setFormData] = useState<TextPostFormData>({
        Title: '',
        Text: '',
        Media: [],
        Visibility: PostVisibility.NUMBER_0,
    });

    const [errors, setErrors] = useState<FormErrors>({});
    const [visibilities, setVisibilities] = useState<PostVisibilitySelectItem[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [hasLoadError, setHasLoadError] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [existingMedia, setExistingMedia] = useState<TextPostMediaViewModel[]>([]);
    const [removedMediaIds, setRemovedMediaIds] = useState<string[]>([]);

    const handleChange = <K extends keyof TextPostFormData>(
        field: K,
        value: TextPostFormData[K]
    ) => {
        setFormData((prev) => ({ ...prev, [field]: value }));
        if (errors[field as keyof FormErrors]) {
            setErrors((prev) => ({ ...prev, [field]: undefined }));
        }
    };

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();

        const validationErrors = validateForm(formData, existingMedia.length > 0);
        setErrors(validationErrors);

        if (hasErrors(validationErrors)) {
            return;
        }

        setIsSubmitting(true);
        try {
            const result = isEditing && postId
                ? await textPostApi.postApiTextPostEdit({
                    Id: postId,
                    Title: formData.Title,
                    Text: formData.Text,
                    Visibility: formData.Visibility ?? PostVisibility.NUMBER_0,
                    Media: formData.Media,
                    RemovedMediaIds: removedMediaIds
                })
                : await textPostApi.postApiTextPostCreateTextPost(formData);
            if (result.status === 200) navigate('/profile');
        } catch {
            setErrors(previous => ({
                ...previous,
                general: isEditing
                    ? 'Не удалось сохранить публикацию. Попробуйте ещё раз.'
                    : 'Не удалось создать публикацию. Попробуйте ещё раз.'
            }));
        } finally {
            setIsSubmitting(false);
        }
    };

    useEffect(() => {
        let isActive = true;
        const controller = new AbortController();
        const loadCreateModel = async () => {
            try {
                const [createResponse, editResponse] = await Promise.all([
                    textPostApi.getApiTextPostCreate(),
                    postId
                        ? textPostApi.getApiTextPostEditPostId(postId, { signal: controller.signal })
                        : Promise.resolve(null)
                ]);
                if (!isActive) return;
                setVisibilities(createResponse.data.visibility ?? []);
                if (editResponse) {
                    setFormData({
                        Title: editResponse.data.title,
                        Text: editResponse.data.text ?? '',
                        Media: [],
                        Visibility: editResponse.data.visibility
                    });
                    setExistingMedia(editResponse.data.media ?? []);
                }
            } catch {
                if (isActive && !controller.signal.aborted) {
                    setHasLoadError(true);
                    setErrors({ general: isEditing ? 'Не удалось загрузить публикацию.' : 'Не удалось загрузить настройки публикации.' });
                }
            } finally {
                if (isActive) setIsLoading(false);
            }
        };
        void loadCreateModel();
        return () => {
            isActive = false;
            controller.abort();
        };
    }, [isEditing, postId]);

    if (isLoading) {
        return <main className="create-text-post-page"><div className="create-text-post-loading">Загрузка формы…</div></main>;
    }

    if (hasLoadError) {
        return (
            <main className="create-text-post-page">
                <div className="create-text-post-loading" role="alert">
                    <p>{errors.general}</p>
                    <Button type="button" variant="secondary" onClick={() => navigate(-1)}>Вернуться назад</Button>
                </div>
            </main>
        );
    }

    return (
        <main className="create-text-post-page">
            <form className="create-text-post-form" onSubmit={handleSubmit} noValidate>
                <header className="create-text-post-form__header">
                    <div>
                        <span className="create-text-post-form__eyebrow">{isEditing ? 'Редактирование' : 'Новая публикация'}</span>
                        <h1>{isEditing ? 'Редактировать текстовый пост' : 'Создать текстовый пост'}</h1>
                        <p>{isEditing
                            ? 'Измените заголовок, текст, медиафайлы или настройки доступа.'
                            : 'Поделитесь текстом и при необходимости прикрепите изображения, видео или аудио.'}</p>
                    </div>
                    <button type="button" className="create-text-post-form__close" aria-label="Закрыть" onClick={() => navigate(-1)}>×</button>
                </header>

                {errors.general && <p className="create-text-post-form__error create-text-post-form__error--general" role="alert">{errors.general}</p>}

                <div className="create-text-post-form__body">
                    <section className="create-text-post-form__section" aria-labelledby="text-post-content-heading">
                        <div className="create-text-post-form__section-heading">
                            <span>1</span>
                            <div><h2 id="text-post-content-heading">Содержание</h2><p>Придумайте заголовок и напишите основной текст.</p></div>
                        </div>
                        <TitleInput
                            value={formData.Title}
                            onChange={(event: ChangeEvent<HTMLInputElement>) => handleChange('Title', event.target.value)}
                            placeholder="Например, итоги недели"
                        />
                        {errors.title && <p className="create-text-post-form__error" role="alert">{errors.title}</p>}

                        <div className="create-text-post-form__field">
                            <label>Текст</label>
                            <RichTextEditor value={formData.Text ?? ''} onChange={(value) => handleChange('Text', value)} placeholder="Напишите что-нибудь…" />
                            {errors.text && <p className="create-text-post-form__error" role="alert">{errors.text}</p>}
                        </div>
                    </section>

                    <section className="create-text-post-form__section" aria-labelledby="text-post-media-heading">
                        <div className="create-text-post-form__section-heading">
                            <span>2</span>
                            <div><h2 id="text-post-media-heading">Медиа</h2><p>Необязательно · до 20 МБ на один файл.</p></div>
                        </div>
                        <MediaUploader
                            files={formData.Media ?? []}
                            onChange={(files) => handleChange('Media', files)}
                            existingFiles={existingMedia}
                            onRemoveExisting={(id) => {
                                setExistingMedia((current) => current.filter((file) => file.id !== id));
                                setRemovedMediaIds((current) => current.includes(id) ? current : [...current, id]);
                            }}
                            error={errors.media}
                        />
                    </section>

                    <section className="create-text-post-form__section create-text-post-form__access" aria-labelledby="text-post-access-heading">
                        <div className="create-text-post-form__section-heading">
                            <span>3</span>
                            <div><h2 id="text-post-access-heading">Доступ</h2><p>Выберите, кому будет видна публикация.</p></div>
                        </div>
                        <PrivacySelect
                            options={visibilities}
                            value={formData.Visibility}
                            onChange={(event: ChangeEvent<HTMLSelectElement>) => handleChange('Visibility', Number(event.target.value) as PostVisibility)}
                        />
                    </section>
                </div>

                <footer className="create-text-post-form__actions">
                    <p>{isEditing ? 'Изменения станут видны сразу после сохранения.' : 'Публикацию можно будет удалить из профиля.'}</p>
                    <div>
                        <Button type="button" variant="secondary" disabled={isSubmitting} onClick={() => navigate(-1)}>Отмена</Button>
                        <Button type="submit" disabled={isSubmitting}>
                            {isSubmitting ? (isEditing ? 'Сохраняем…' : 'Создаём…') : (isEditing ? 'Сохранить' : 'Создать пост')}
                        </Button>
                    </div>
                </footer>
            </form>
        </main>
    );
};
export default CreateTextPostForm;


