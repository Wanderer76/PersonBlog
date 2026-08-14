import { useRef, useState } from 'react';
import styles from '../post/CreatePostForm.module.css';

const BlogCoverUpload = ({ file, imagePreview, disabled, onSelect }) => {
    const fileInputRef = useRef(null);
    const [isDragging, setIsDragging] = useState(false);

    const openFilePicker = () => fileInputRef.current?.click();

    const handleKeyDown = (event) => {
        if (event.key !== 'Enter' && event.key !== ' ') return;
        event.preventDefault();
        openFilePicker();
    };

    const handleDrop = (event) => {
        event.preventDefault();
        setIsDragging(false);
        onSelect(event.dataTransfer.files?.[0]);
    };

    return (
        <section className={styles.mediaColumn} aria-label="Обложка блога">
            <div className={styles.sectionHeading}>
                <span className={styles.stepNumber}>1</span>
                <div>
                    <h2>Обложка</h2>
                    <p>Изображение в формате JPG, PNG или WebP</p>
                </div>
            </div>

            {!imagePreview ? (
                <div
                    className={styles.uploadArea}
                    role="button"
                    tabIndex={0}
                    onClick={openFilePicker}
                    onKeyDown={handleKeyDown}
                    onDragEnter={(event) => {
                        event.preventDefault();
                        setIsDragging(true);
                    }}
                    onDragOver={(event) => event.preventDefault()}
                    onDragLeave={() => setIsDragging(false)}
                    onDrop={handleDrop}
                    style={isDragging ? { borderColor: 'var(--create-accent)', background: '#fff8f3' } : undefined}
                >
                    <span className={styles.uploadIcon} aria-hidden="true">+</span>
                    <strong>Выберите изображение</strong>
                    <span>или перетащите его в эту область</span>
                </div>
            ) : (
                <div className={styles.previewContainer}>
                    <img
                        src={imagePreview}
                        alt="Предпросмотр обложки блога"
                        className={styles.videoPreview}
                        style={{ objectFit: 'cover' }}
                    />
                    <div className={styles.fileDetails}>
                        <div>
                            <strong title={file?.name}>{file?.name}</strong>
                            <span>Обложка блога</span>
                        </div>
                        <button type="button" onClick={openFilePicker} disabled={disabled}>
                            Заменить
                        </button>
                    </div>
                </div>
            )}

            <input
                ref={fileInputRef}
                name="photoUrl"
                type="file"
                className={styles.fileInput}
                accept="image/jpeg,image/png,image/webp"
                hidden
                onChange={(event) => onSelect(event.currentTarget.files?.[0])}
            />
        </section>
    );
};

export default BlogCoverUpload;
