import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import styles from '../post/CreatePostForm.module.css';
import { getBlog } from '@/lib/api/generated/blog/blog';
import BlogCoverUpload from './BlogCoverUpload';
import BlogDetailsFields from './BlogDetailsFields';

const blogApi = getBlog();

const CreateBlogForm = () => {
    const navigate = useNavigate();
    const [blogForm, setBlogForm] = useState({
        title: '',
        description: '',
        photoUrl: null
    });
    const [imagePreview, setImagePreview] = useState(null);
    const [isCreating, setIsCreating] = useState(false);
    const [errorMessage, setErrorMessage] = useState(null);

    useEffect(() => {
        return () => {
            if (imagePreview) URL.revokeObjectURL(imagePreview);
        };
    }, [imagePreview]);

    const selectImage = (file) => {
        if (!file) return;
        if (!file.type.startsWith('image/')) {
            setErrorMessage('Выберите файл изображения');
            return;
        }

        setErrorMessage(null);
        setBlogForm((previous) => ({ ...previous, photoUrl: file }));
        setImagePreview(URL.createObjectURL(file));
    };

    const updateTextField = (event) => {
        const { name, value } = event.currentTarget;
        setBlogForm((previous) => ({ ...previous, [name]: value }));
        if (name === 'title' && value.trim()) setErrorMessage(null);
    };

    const sendForm = async () => {
        if (isCreating) return;
        if (!blogForm.title.trim()) {
            setErrorMessage('Введите название блога');
            return;
        }

        setIsCreating(true);
        setErrorMessage(null);

        try {
            await blogApi.postApiBlogCreate({
                Title: blogForm.title.trim(),
                Description: blogForm.description.trim(),
                PhotoUrl: blogForm.photoUrl ?? undefined
            });
            navigate('/profile');
        } catch (error) {
            console.error('Error creating blog:', error);
            setErrorMessage('Не удалось создать блог. Попробуйте ещё раз.');
        } finally {
            setIsCreating(false);
        }
    };

    return (
        <main className={styles.pageShell}>
            <section className={styles.formCard} aria-labelledby="create-blog-title">
                <header className={styles.formHeader}>
                    <div>
                        <span className={styles.eyebrow}>Новый блог</span>
                        <h1 id="create-blog-title">Создание блога</h1>
                        <p>Добавьте обложку и расскажите читателям, о чём будет ваш блог.</p>
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

                <div className={styles.formBody}>
                    <BlogCoverUpload
                        file={blogForm.photoUrl}
                        imagePreview={imagePreview}
                        disabled={isCreating}
                        onSelect={selectImage}
                    />
                    <BlogDetailsFields
                        title={blogForm.title}
                        description={blogForm.description}
                        onChange={updateTextField}
                    />
                </div>

                <footer className={styles.actionBar}>
                    <p>Название можно будет изменить позже в настройках блога.</p>
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
                            disabled={isCreating || !blogForm.title.trim()}
                            onClick={() => void sendForm()}
                        >
                            {isCreating ? 'Создаём…' : 'Создать блог'}
                        </button>
                    </div>
                </footer>
            </section>
        </main>
    );
};

export default CreateBlogForm;
