import { useRef } from 'react';
import styles from './CreatePostForm.module.css';

// Общий компонент для ввода названия
export const TitleInput = ({ value, onChange, placeholder }) => (
    <div className={styles.formGroup}>
        <label>Название</label>
        <input
            className={styles.modalContent}
            type="text"
            placeholder={placeholder}
            name="title"
            value={value}
            onChange={onChange}
        />
    </div>
);

// Общий компонент для миниатюры (создание)
// export const ThumbnailUpload = ({ thumbnail, onChange }) => {
//     const fileInputRef = useRef(null);

//     const handleClick = () => {
//         fileInputRef.current?.click();
//     };

//     return (
//         <div className={styles.formGroup}>
//             <label>Превью (миниатюра)</label>
//             <div className={styles.uploadThumbnail} onClick={handleClick}>
//                 {thumbnail ? (
//                     <img
//                         src={typeof thumbnail === 'string' ? thumbnail : URL.createObjectURL(thumbnail)}
//                         alt="Превью"
//                         className={styles.thumbnailPreview}
//                     />
//                 ) : (
//                     <>
//                         <span>📷</span>
//                         <p>Выберите изображение</p>
//                     </>
//                 )}
//             </div>
//             <input
//                 ref={fileInputRef}
//                 name="thumbnail"
//                 type="file"
//                 className={`${styles.thumbnailInput} ${styles.fileInput}`}
//                 accept="image/*"
//                 hidden
//                 onChange={onChange}
//             />
//         </div>
//     );
// };

// Общий компонент для миниатюры (редактирование)
export const ThumbnailEdit = ({ thumbnailUrl, onChange }) => (
    <div className={styles.formGroup}>
        <label>Заставка видео</label>
        <div className={styles.thumbnailContainer}>
            {thumbnailUrl ? (
                <img
                    src={thumbnailUrl}
                    alt="Заставка видео"
                    className={styles.thumbnailImage}
                />
            ) : (
                <div className={styles.thumbnailPlaceholder}>
                    <span>🖼️</span>
                    <p>Заставка не доступна</p>
                </div>
            )}
        </div>
        <div style={{ marginTop: '10px' }}>
            <input
                type="file"
                accept="image/*"
                hidden
                id="thumbnailInput"
                onChange={onChange}
            />
            <label htmlFor="thumbnailInput" className={`${styles.btn} ${styles.btnSecondary}`}>
                Выбрать новую заставку
            </label>
        </div>
    </div>
);

export const ThumbnailUpload = ({ thumbnail, onChange }) => {
    const fileInputRef = useRef(null);

    const handleClick = () => {
        fileInputRef.current?.click();
    };

    // Определяем источник изображения
    const thumbnailSrc = typeof thumbnail === 'string'
        ? thumbnail
        : thumbnail instanceof File
            ? URL.createObjectURL(thumbnail)
            : null;

    return (
        <div className={styles.formGroup}>
            <label>{thumbnailSrc ? 'Заставка видео' : 'Превью (миниатюра)'}</label>

            <div
                className={`${styles.uploadThumbnail} ${thumbnailSrc ? styles.hasThumbnail : ''}`}
                onClick={handleClick}
            >
                {thumbnailSrc ? (
                    <>
                        <img
                            src={thumbnailSrc}
                            alt="Превью"
                            className={styles.thumbnailPreview}
                        />
                        <div className={styles.thumbnailOverlay}>
                            <span>✏️</span>
                            <p>Изменить изображение</p>
                        </div>
                    </>
                ) : (
                    <>
                        <span>📷</span>
                        <p>Выберите изображение</p>
                    </>
                )}
            </div>

            <input
                ref={fileInputRef}
                name="thumbnail"
                type="file"
                accept="image/*"
                hidden
                onChange={onChange}
            />
        </div>
    );
};

// Общий компонент для описания
export const DescriptionTextarea = ({ value, onChange, placeholder }) => (
    <div className={styles.formGroup}>
        <label>Описание</label>
        <textarea
            className={`${styles.modalContent} ${styles.description}`}
            rows="4"
            placeholder={placeholder}
            name="description"
            value={value}
            onChange={onChange}
        />
    </div>
);

// Общий компонент для настроек приватности
export const PrivacySelect = ({ options, value, onChange }) => (
    <div className={styles.formGroup}>
        <label>Настройки приватности</label>
        <div className={styles.privacySettings}>
            <select name="visibility" value={value} onChange={onChange}>
                {options?.map((v) => (
                    <option key={v.value} value={v.value}>
                        {v.text}
                    </option>
                ))}
            </select>
            <span>🔒</span>
        </div>
    </div>
);

// Общий компонент для кнопок действий
export const ActionButtons = ({
    onCancel,
    onSubmit,
    cancelText,
    submitText,
    isSubmitting = false
}) => (
    <div className={styles.actionButtons}>
        <button
            className={`${styles.btn} ${styles.btnSecondary}`}
            onClick={onCancel}
            disabled={isSubmitting}
        >
            {cancelText}
        </button>
        <button
            className={`${styles.btn} ${styles.btnPrimary}`}
            onClick={onSubmit}
            disabled={isSubmitting}
        >
            {isSubmitting ? 'Загрузка...' : submitText}
        </button>
    </div>
);