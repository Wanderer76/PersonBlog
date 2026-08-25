import { memo } from 'react';
import { Button } from '@/shared/ui/Button/Button';
import { getLocalDateTime } from '@/shared/lib/date';
import DefaultProfileIcon from '@/shared/assets/defaultProfilePic.png';
import '@/entities/profile/ui/ProfileHeader/ProfileHeader.css';
import { BlogModel } from '@/shared/api/generated/models';

type ProfileViewModel = BlogModel & { totalPostsCount: number };

interface ProfileHeaderProps {
  profile: ProfileViewModel;
  hasBlog: boolean;
  onLogout: () => void;
  onCreateBlog: () => void;
  onEditBlog: () => void;
}

export const ProfileHeader = memo(({ profile, hasBlog, onLogout, onCreateBlog, onEditBlog }: ProfileHeaderProps) => (
  <header className="profileHeader">
    <div className="avatarSection">
      <div className="avatarWrapper">
        <img
          src={profile.photoUrl ?? DefaultProfileIcon}
          alt="Аватар пользователя"
          className="profileAvatar"
        />
      </div>

      <div className="profileInfo">
        <h1 className="blogTitle">{profile.name || 'Без имени'}</h1>
        {profile.description && <p className="profileDescription">{profile.description}</p>}
        <div className="profileMeta">
          <span className="profileStat">
            <strong>{profile.totalPostsCount}</strong>
            <span>публикаций</span>
          </span>
          <span className="profileStat">
            <strong>{profile.subscribersCount ?? 0}</strong>
            <span>подписчиков</span>
          </span>
          {profile.createdAt && (
            <time className="registration-date" dateTime={profile.createdAt}>
              На сайте с {getLocalDateTime(profile.createdAt)}
            </time>
          )}
        </div>
      </div>
    </div>

    <div className="profileHeaderActions">
      {!hasBlog && <Button onClick={onCreateBlog}>Создать блог</Button>}
      {hasBlog && <Button onClick={onEditBlog}>Редактировать блог</Button>}
      <Button className="profileLogoutButton" variant="secondary" onClick={onLogout}>Выйти</Button>
    </div>
  </header>
));

ProfileHeader.displayName = 'ProfileHeader';
