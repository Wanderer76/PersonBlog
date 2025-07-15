import './CreatePostForm.css';

// Общий компонент для ввода названия
export const TitleInput = ({ value, onChange, placeholder }) => (
    <div className='formGroup'>
        <label>Название</label>
        <input
            className="modalContent"
            type="text"
            placeholder={placeholder}
            name="title"
            value={value}
            onChange={onChange}
        />
    </div>
);

// Общий компонент для миниатюры (создание)
export const ThumbnailUpload = ({ thumbnail, onChange }) => (
    <div className="formGroup">
        <label>Превью (миниатюра)</label>
        <div className="uploadThumbnail" onClick={() => document.querySelector('.thumbnailInput').click()}>
            {thumbnail ? (
                <img
                    src={typeof thumbnail === 'string' ? thumbnail : URL.createObjectURL(thumbnail)}
                    alt="Превью"
                    className="thumbnailPreview"
                />
            ) : (
                <>
                    <span>📷</span>
                    <p>Выберите изображение</p>
                </>
            )}
        </div>
        <input
            name="thumbnail"
            type="file"
            className="thumbnailInput fileInput"
            accept="image/*"
            hidden
            onChange={onChange}
        />
    </div>
);

// Общий компонент для миниатюры (редактирование)
export const ThumbnailEdit = ({ thumbnailUrl, onChange }) => (
    <div className="formGroup">
        <label>Заставка видео</label>
        <div className="thumbnailContainer">
            {thumbnailUrl ? (
                <img
                    src={thumbnailUrl}
                    alt="Заставка видео"
                    className="thumbnailImage"
                />
            ) : (
                <div className="thumbnailPlaceholder">
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
            <label htmlFor="thumbnailInput" className="btn btnSecondary">
                Выбрать новую заставку
            </label>
        </div>
    </div>
);

// Общий компонент для описания
export const DescriptionTextarea = ({ value, onChange, placeholder }) => (
    <div className="formGroup">
        <label>Описание</label>
        <textarea
            className="modalContent description"
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
    <div className="formGroup">
        <label>Настройки приватности</label>
        <div className="privacySettings">
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
    <div className="actionButtons">
        <button
            className="btn btnSecondary"
            onClick={onCancel}
            disabled={isSubmitting}
        >
            {cancelText}
        </button>
        <button
            className="btn btnPrimary"
            onClick={onSubmit}
            disabled={isSubmitting}
        >
            {isSubmitting ? 'Загрузка...' : submitText}
        </button>
    </div>
);