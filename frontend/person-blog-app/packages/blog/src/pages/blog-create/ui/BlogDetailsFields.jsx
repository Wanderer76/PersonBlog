import styles from '@/pages/post-create/ui/CreatePostForm.module.css';

const BlogDetailsFields = ({ title, description, onChange }) => (
    <section className={styles.detailsColumn} aria-label="Информация о блоге">
        <div className={styles.sectionHeading}>
            <span className={styles.stepNumber}>2</span>
            <div>
                <h2>О блоге</h2>
                <p>Название и краткое описание</p>
            </div>
        </div>

        <div className={styles.formGroup}>
            <label htmlFor="blog-title">Название</label>
            <input
                id="blog-title"
                className={styles.modalContent}
                type="text"
                placeholder="Например, Путешествия по Уралу"
                name="title"
                value={title}
                onChange={onChange}
                autoComplete="off"
                required
            />
        </div>

        <div className={styles.formGroup}>
            <label htmlFor="blog-description">Описание</label>
            <textarea
                id="blog-description"
                className={`${styles.modalContent} ${styles.description}`}
                rows={6}
                placeholder="Расскажите, какие публикации здесь появятся"
                name="description"
                value={description}
                onChange={onChange}
            />
        </div>
    </section>
);

export default BlogDetailsFields;
