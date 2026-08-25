import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import styles from '@/pages/post-create/ui/CreatePostForm.module.css';
import { getBlog } from '@/shared/api/generated/blog/blog';
import BlogCoverUpload from '@/pages/blog-create/ui/BlogCoverUpload';
import BlogDetailsFields from '@/pages/blog-create/ui/BlogDetailsFields';

const blogApi = getBlog();

const EditBlogForm = () => {
    const navigate = useNavigate();
    const [blogId, setBlogId] = useState(null);
    const [blogForm, setBlogForm] = useState({
        title: '',
        description: '',
        photoUrl: null
    });
    const [imagePreview, setImagePreview] = useState(null);
    const [selectedImagePreview, setSelectedImagePreview] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [errorMessage, setErrorMessage] = useState(null);

    useEffect(() => {
        let isActive = true;

        const loadBlog = async () => {
            try {
                const { data } = await blogApi.getApiBlogDetail();
                if (!isActive) return;
                if (!data.id) {
                    setErrorMessage('Не удалось определить блог для редактирования.');
                    return;
                }

                setBlogId(data.id);
                setBlogForm({
                    title: data.name ?? '',
                    description: data.description ?? '',
                    photoUrl: null
                });
                setImagePreview(data.photoUrl ?? null);
            } catch (error) {
                console.error('Error loading blog:', error);
                if (isActive) setErrorMessage('Не удалось загрузить данные блога. Попробуйте ещё раз.');
            } finally {
                if (isActive) setIsLoading(false);
            }
        };

        void loadBlog();
        return () => {
            isActive = false;
        };
    }, []);

    useEffect(() => () => {
        if (selectedImagePreview) URL.revokeObjectURL(selectedImagePreview);
    }, [selectedImagePreview]);

    const selectImage = (file) => {
        if (!file) return;
        if (!file.type.startsWith('image/')) {
            setErrorMessage('Выберите файл изображения');
            return;
        }

        const preview = URL.createObjectURL(file);
        setErrorMessage(null);
        setBlogForm((previous) => ({ ...previous, photoUrl: file }));
        setSelectedImagePreview(preview);
        setImagePreview(preview);
    };

    const updateTextField = (event) => {
        const { name, value } = event.currentTarget;
        setBlogForm((previous) => ({ ...previous, [name]: value }));
        if (name === 'title' && value.trim()) setErrorMessage(null);
    };

    const sendForm = async () => {
        if (isSaving || !blogId) return;
        if (!blogForm.title.trim()) {
            setErrorMessage('Введите название блога');
            return;
        }

        setIsSaving(true);
        setErrorMessage(null);

        try {
            await blogApi.putApiBlogBlogId(blogId, {
                Title: blogForm.title.trim(),
                Description: blogForm.description.trim(),
                PhotoUrl: blogForm.photoUrl ?? undefined
            });
            navigate('/profile');
        } catch (error) {
            console.error('Error updating blog:', error);
            setErrorMessage('Не удалось сохранить изменения. Попробуйте ещё раз.');
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <main className={styles.pageShell}>
            <section className={styles.formCard} aria-labelledby="edit-blog-title">
                <header className={styles.formHeader}>
                    <div>
                        <span className={styles.eyebrow}>Настройки блога</span>
                        <h1 id="edit-blog-title">Редактирование блога</h1>
                        <p>Обновите обложку, название или описание вашего блога.</p>
                    </div>
                    <button
                        className={styles.closeButton}
                        type="button"
                        onClick={() => navigate('/profile')}
                        aria-label="Закрыть форму"
                    >
                        ×
                    </button>
                </header>

                {errorMessage && (
                    <div className={styles.errorBanner} role="alert">{errorMessage}</div>
                )}

                {isLoading ? (
                    <div className={styles.formBody}>Загрузка данных блога...</div>
                ) : (
                    <div className={styles.formBody}>
                        <BlogCoverUpload
                            file={blogForm.photoUrl}
                            imagePreview={imagePreview}
                            disabled={isSaving}
                            onSelect={selectImage}
                        />
                        <BlogDetailsFields
                            title={blogForm.title}
                            description={blogForm.description}
                            onChange={updateTextField}
                        />
                    </div>
                )}

                <footer className={styles.actionBar}>
                    <p>Изменения появятся в профиле после сохранения.</p>
                    <div className={styles.actionButtons}>
                        <button
                            className={`${styles.btn} ${styles.btnSecondary}`}
                            type="button"
                            onClick={() => navigate('/profile')}
                        >
                            Отмена
                        </button>
                        <button
                            className={`${styles.btn} ${styles.btnPrimary}`}
                            type="button"
                            disabled={isLoading || isSaving || !blogId || !blogForm.title.trim()}
                            onClick={() => void sendForm()}
                        >
                            {isSaving ? 'Сохраняем…' : 'Сохранить изменения'}
                        </button>
                    </div>
                </footer>
            </section>
        </main>
    );
};

export default EditBlogForm;
