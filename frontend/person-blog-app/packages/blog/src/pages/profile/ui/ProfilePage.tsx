import { Link } from 'react-router-dom';
import { useProfilePreview } from '@/entities/profile/model/profilePreview';
import DefaultProfileIcon from '@/shared/assets/defaultProfilePic.png';
import styles from './PersonalProfile.module.css';

export default function ProfilePage() {
    const { person, profileStatus, reloadProfile, features, blog, status, reloadBlog } = useProfilePreview();
    return <main className={styles.page}>
        <p className={styles.breadcrumb}><Link to="/">Главная</Link><span>/</span>Личный профиль</p>
        <nav className={styles.navigation} aria-label="Личные разделы">
            <Link to="/profile" aria-current="page">Мой профиль</Link>
            {features.friendsEnabled && <Link to="/friends">Друзья</Link>}
            {features.messagesEnabled && <Link to="/messages">Сообщения</Link>}
        </nav>
        {profileStatus === 'loading' ? <section className={styles.card}><p className={styles.muted} role="status">Загрузка профиля…</p></section> : profileStatus === 'error' ? <section className={styles.card} role="alert"><h1>Не удалось загрузить профиль</h1><p className={styles.muted}>Попробуйте ещё раз.</p><button className={styles.secondary} onClick={reloadProfile}>Повторить</button></section> : <><section className={styles.hero}>
            <div className={styles.identity}>
                <div className={styles.avatar}>{person.photoUrl ? <img src={person.photoUrl} alt="" /> : person.name.trim().split(/\s+/).slice(0, 2).map(word => word[0]).join('')}</div>
                <div><p className={styles.eyebrow}>Личный профиль</p><h1>{person.name}</h1>{person.username && <p className={styles.handle}>@{person.username}</p>}</div>
            </div>
            <Link className={styles.secondary} to="/profile/edit">Редактировать профиль</Link>
        </section>
        <div className={features.friendsEnabled ? styles.columns : undefined}>
            <section className={styles.card}>
                <h2>Обо мне</h2>
                <dl className={styles.details}>
                    <div><dt>Интересы</dt><dd>{person.interests.length ? person.interests.join(', ') : 'Не указаны'}</dd></div>
                    <div><dt>На сайте с</dt><dd>{person.createdAt ? new Date(person.createdAt).toLocaleDateString('ru-RU', { month: 'long', year: 'numeric' }) : 'Не указано'}</dd></div>
                </dl>
            </section>
            {features.friendsEnabled && <section className={styles.card}><h2>Друзья</h2><p className={styles.muted}>Раздел появится позже.</p></section>}
        </div></>}
        <section className={`${styles.card} ${styles.blogCard}`} aria-label="Мой блог" aria-busy={status === 'loading'}>
            {status === 'loading' ? <p className={styles.muted}>Загрузка блога…</p> : status === 'error' ? <div role="alert"><h2>Не удалось загрузить блог</h2><p className={styles.muted}>Попробуйте ещё раз.</p><button className={styles.secondary} onClick={reloadBlog}>Повторить</button></div> : blog ? <>
                <div className={styles.identity}>
                    <img className={styles.blogAvatar} src={blog.photoUrl || DefaultProfileIcon} alt="Аватар блога" />
                    <div><p className={styles.eyebrow}>Мой блог</p><h2>{blog.name}</h2>{blog.description && <p className={styles.muted}>{blog.description}</p>}<p className={styles.muted}>{blog.subscribersCount ?? 0} подписчиков</p></div>
                </div>
                <Link className={styles.secondary} to="/studio">Управление блогом →</Link>
            </> : <>
                <div><p className={styles.eyebrow}>Мой блог</p><h2>У вас пока нет блога</h2><p className={styles.muted}>Создайте блог, чтобы публиковать видео и посты.</p></div>
                <Link className={styles.primary} to="/profile/blog/create">Создать блог</Link>
            </>}
        </section>
    </main>;
}
