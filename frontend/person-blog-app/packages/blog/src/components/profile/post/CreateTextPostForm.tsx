import { useState, useEffect } from 'react';
import type { ChangeEvent, FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/shared/ui/Button/Button';
import { PrivacySelect, TitleInput } from './CommonComponents';
import { PostVisibility } from '@/lib/api/generated/models';
import type { PostVisibilitySelectItem } from '@/lib/api/generated/models';
import { getTextPost } from '@/lib/api/generated/text-post/text-post';
import { MediaUploader } from '@/features/post-management/components/MediaUploader/MediaUploader';
import { RichTextEditor } from '@/features/post-management/components/RichTextEditor/RichTextEditor';
import { hasErrors, validateForm } from './CreateTextPostForm.validation';
import type { FormErrors, TextPostFormData } from './CreateTextPostForm.validation';
import './CreateTextPostForm.css';

const textPostApi = getTextPost();

const CreateTextPostForm = () => {
    const navigate = useNavigate();
    const [formData, setFormData] = useState<TextPostFormData>({
        Title: '',
        Text: '',
        Media: [],
        Visibility: PostVisibility.NUMBER_0,
    });

    const [errors, setErrors] = useState<FormErrors>({});
    const [visibilities, setVisibilities] = useState<PostVisibilitySelectItem[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [isSubmitting, setIsSubmitting] = useState(false);

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

        const validationErrors = validateForm(formData);
        setErrors(validationErrors);

        if (hasErrors(validationErrors)) {
            return;
        }

        setIsSubmitting(true);
        try {
            const result = await textPostApi.postApiTextPostCreateTextPost(formData);
            if (result.status === 200) navigate('/profile');
        } catch {
            setErrors(previous => ({ ...previous, general: 'Не удалось создать публикацию. Попробуйте ещё раз.' }));
        } finally {
            setIsSubmitting(false);
        }
    };

    useEffect(() => {
        let isActive = true;
        const loadCreateModel = async () => {
            try {
                const response = await textPostApi.getApiTextPostCreate();
                if (!isActive) return;
                setVisibilities(response.data.visibility ?? []);
            } catch {
                if (isActive) setErrors({ general: 'Не удалось загрузить настройки публикации.' });
            } finally {
                if (isActive) setIsLoading(false);
            }
        };
        void loadCreateModel();
        return () => { isActive = false; };
    }, []);

    if (isLoading) {
        return <main className="create-text-post-page"><div className="create-text-post-loading">Загрузка формы…</div></main>;
    }

    return (
        <main className="create-text-post-page">
            <form className="create-text-post-form" onSubmit={handleSubmit} noValidate>
                <header className="create-text-post-form__header">
                    <div>
                        <span className="create-text-post-form__eyebrow">Новая публикация</span>
                        <h1>Создать текстовый пост</h1>
                        <p>Поделитесь текстом и при необходимости прикрепите изображения, видео или аудио.</p>
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
                        <MediaUploader files={formData.Media ?? []} onChange={(files) => handleChange('Media', files)} error={errors.media} />
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
                    <p>Публикацию можно будет удалить из профиля.</p>
                    <div>
                        <Button type="button" variant="secondary" disabled={isSubmitting} onClick={() => navigate(-1)}>Отмена</Button>
                        <Button type="submit" disabled={isSubmitting}>{isSubmitting ? 'Создаём…' : 'Создать пост'}</Button>
                    </div>
                </footer>
            </form>
        </main>
    );
};
export default CreateTextPostForm;


