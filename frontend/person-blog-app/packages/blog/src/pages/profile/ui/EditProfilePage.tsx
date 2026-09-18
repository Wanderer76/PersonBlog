import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useProfilePreview } from '@/entities/profile/model/profilePreview';
import styles from './PersonalProfile.module.css';

export default function EditProfilePage() {
    const { person, updatePerson } = useProfilePreview();
    const [name, setName] = useState(person.name);
    const [photoUrl, setPhotoUrl] = useState(person.photoUrl);
    const [error, setError] = useState('');
    const readerRef = useRef<FileReader | null>(null);
    const navigate = useNavigate();
    useEffect(() => () => readerRef.current?.abort(), []);
    function save(event: FormEvent) {
        event.preventDefault();
        if (!name.trim()) { setError('Введите имя.'); return; }
        updatePerson({ ...person, name: name.trim(), photoUrl });
        navigate('/profile');
    }
    return <main className={styles.page}>
        <p className={styles.breadcrumb}><Link to="/profile">Личный профиль</Link><span>/</span>Редактирование</p>
        <form className={styles.card} onSubmit={save}>
            <p className={styles.eyebrow}>Личный профиль</p><h1>Редактирование профиля</h1>
            <p className={styles.muted}>Выберите аватар и имя, которое увидят другие пользователи.</p>
            <div className={styles.formGrid}>
                <div><div className={styles.avatar}>{photoUrl ? <img src={photoUrl} alt="Предпросмотр аватара" /> : name.trim().slice(0, 1)}</div>
                    <label className={styles.upload}>Выбрать фотографию<input type="file" accept="image/png,image/jpeg,image/webp" onChange={event => {
                        const file = event.target.files?.[0];
                        if (!file) return;
                        if (!['image/png', 'image/jpeg', 'image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024) { setError('Выберите JPG, PNG или WebP размером до 5 МБ.'); return; }
                        readerRef.current?.abort();
                        const reader = new FileReader();
                        readerRef.current = reader;
                        reader.onload = () => { setPhotoUrl(String(reader.result)); setError(''); };
                        reader.onerror = () => setError('Не удалось прочитать фотографию.');
                        reader.readAsDataURL(file);
                    }} /></label><p className={styles.muted}>JPG, PNG или WebP · до 5 МБ</p>
                </div>
                <div><label className={styles.field}>Имя<input value={name} onChange={event => setName(event.target.value)} maxLength={100} required /></label>{person.username && <label className={styles.field}>Логин<input value={`@${person.username}`} readOnly /></label>}<p className={styles.muted}>Редактирование интересов появится позже.</p></div>
            </div>
            {error && <p role="alert">{error}</p>}
            <p className={styles.previewNote}>Макет: имя и фотография изменятся только в текущем предпросмотре. На сервер данные не отправляются.</p>
            <div className={styles.actions}><Link className={styles.secondary} to="/profile">Отмена</Link><button className={styles.primary} type="submit">Сохранить в предпросмотре</button></div>
        </form>
    </main>;
}
