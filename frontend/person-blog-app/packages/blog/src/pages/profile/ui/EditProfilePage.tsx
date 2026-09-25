import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useProfilePreview } from '@/entities/profile/model/profilePreview';
import { getProfile } from '@/shared/api/generated/profile/profile';
import styles from './PersonalProfile.module.css';

const profileApi = getProfile();

export default function EditProfilePage() {
    const { person, updatePerson } = useProfilePreview();
    const [name, setName] = useState(person.name);
    const [photoUrl, setPhotoUrl] = useState(person.photoUrl);
    const [profilePicture, setProfilePicture] = useState<File | null>(null);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState('');
    const readerRef = useRef<FileReader | null>(null);
    const navigate = useNavigate();
    useEffect(() => () => readerRef.current?.abort(), []);
    async function save(event: FormEvent) {
        event.preventDefault();
        if (!name.trim()) { setError('Введите имя.'); return; }
        setIsSaving(true);
        setError('');
        try {
            const { data } = await profileApi.postApiProfileEdit({
                Name: name.trim(),
                ProfilePicture: profilePicture ?? undefined
            });
            updatePerson({
                ...person,
                name: data.name ?? name.trim(),
                photoUrl: data.photoUrl ?? '',
                createdAt: data.createdAt,
                interests: data.interests ?? []
            });
            navigate('/profile');
        } catch {
            setError('Не удалось сохранить профиль. Попробуйте ещё раз.');
        } finally {
            setIsSaving(false);
        }
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
                        setProfilePicture(file);
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
            <div className={styles.actions}><Link className={styles.secondary} to="/profile">Отмена</Link><button className={styles.primary} type="submit" disabled={isSaving}>{isSaving ? 'Сохраняем…' : 'Сохранить'}</button></div>
        </form>
    </main>;
}
