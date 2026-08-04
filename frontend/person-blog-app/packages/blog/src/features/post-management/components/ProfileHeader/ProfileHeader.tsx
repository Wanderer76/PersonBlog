import { memo } from 'react';
import { Button } from '@/shared/ui/Button/Button';
import { getLocalDateTime } from '@/shared/LocalDate';
import DefaultProfileIcon from '@/defaultProfilePic.png';
import './ProfileHeader.css';
import { BlogModel } from '@/lib/api/generated/models';

type ProfileViewModel = BlogModel & { totalPostsCount: number };

interface ProfileHeaderProps {
  profile: ProfileViewModel;
  hasBlog: boolean;
  onLogout: () => void;
  onCreateBlog: () => void;
}

export const ProfileHeader = memo(({ profile, hasBlog, onLogout, onCreateBlog }: ProfileHeaderProps) => (
  <header className="profileHeader">
    <div className="avatarSection">
      {!hasBlog && (
        <Button variant="secondary" onClick={onCreateBlog}>
          Создать блог
        </Button>
      )}

      <div className="avatarWrapper">
        <img
          src={profile.photoUrl ?? DefaultProfileIcon}
          alt="Аватар пользователя"
          className="profileAvatar"
        />
      </div>

      <div className="profileInfo">
        <h1 className="blogTitle">{profile.name || 'Без имени'}</h1>
        <div className="profileMeta">
          {profile.createdAt && (
            <time className="registration-date" dateTime={profile.createdAt}>
              📅 Зарегистрирован: {getLocalDateTime(profile.createdAt)}
            </time>
          )}
          <span className="posts-count">📝 Публикаций: {profile.totalPostsCount}</span>
        </div>
      </div>
    </div>

    <Button variant="danger" onClick={onLogout}>Выход</Button>
  </header>
));

ProfileHeader.displayName = 'ProfileHeader';
