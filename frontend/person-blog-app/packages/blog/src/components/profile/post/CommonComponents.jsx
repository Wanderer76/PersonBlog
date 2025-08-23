import { useRef } from 'react';
import styles from './CreatePostForm.module.css';
import { useState } from 'react';

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
export const CategoryMultiSelect = ({ options = [], value = [], onChange }) => {
  const [isOpen, setIsOpen] = useState(false);

  // Приводим value к массиву id (ожидаем, что value — массив id или объектов)
  const selectedIds = Array.isArray(value)
    ? value.map(v => (typeof v === 'object' && v !== null ? v.id : v))
    : [];

  const selectedOptions = Array.isArray(options)
    ? options.filter(opt => opt && selectedIds.includes(opt.id))
    : [];

  const toggleOption = (option) => {
    const isSelected = selectedIds.includes(option.id);

    // Обновляем массив ID
    const newSelectedIds = isSelected
      ? selectedIds.filter(id => id !== option.id) // удаляем
      : [...selectedIds, option.id]; // добавляем

    // Передаём только массив ID
    onChange({ target: { name: 'categories', value: newSelectedIds } });
  };

  const toggleDropdown = () => setIsOpen(prev => !prev);

  const handleBlur = (e) => {
    if (!e.relatedTarget || !e.currentTarget.contains(e.relatedTarget)) {
      setIsOpen(false);
    }
  };

  return (
    <div className={styles.formGroup}>
      <label>Категории</label>
      <div
        className={styles.multiselectContainer}
        onClick={toggleDropdown}
        tabIndex="0"
        onBlur={handleBlur}
      >
        {/* Отображение выбранных тегов */}
        <div className={styles.multiselectSelectedTags}>
          {selectedOptions.length === 0 ? (
            <span className={styles.placeholder}>Выберите категории</span>
          ) : (
            selectedOptions.map(opt => (
              <span key={opt.id} className={styles.tag}>
                {opt.title}
              </span>
            ))
          )}
        </div>

        {/* Стрелочка */}
        <div className={styles.dropdownTrigger}>
          <span className={styles.dropdownArrow}>▼</span>
        </div>

        {/* Выпадающий список */}
        {isOpen && (
          <ul className={styles.multiselectList}>
            {options.map((option) => (
              <li
                key={option.id}
                onClick={(e) => {
                  e.stopPropagation();
                  toggleOption(option); // ← передаём объект, но в onChange уйдёт только id
                }}
                className={selectedIds.includes(option.id) ? styles.selected : ''}
              >
                {option.title}
                {selectedIds.includes(option.id) && <span className={styles.checkmark}>✓</span>}
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
};

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