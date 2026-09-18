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
  onCreateBlog: () => void;
  onEditBlog: () => void;
}

export const ProfileHeader = memo(({ profile, hasBlog, onCreateBlog, onEditBlog }: ProfileHeaderProps) => (
  <header className="profileHeader">
    <div className="avatarSection">
      <div className="avatarWrapper">
        <img
          src={profile.photoUrl ?? DefaultProfileIcon}
          alt="Аватар блога"
          className="profileAvatar"
        />
      </div>

      <div className="profileInfo">
        <span className="studio-eyebrow">Управление блогом</span>
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
              Блог создан {getLocalDateTime(profile.createdAt)}
            </time>
          )}
        </div>
      </div>
    </div>

    <div className="profileHeaderActions">
      {!hasBlog && <Button onClick={onCreateBlog}>Создать блог</Button>}
      {hasBlog && <Button onClick={onEditBlog}>Редактировать блог</Button>}
    </div>
  </header>
));

ProfileHeader.displayName = 'ProfileHeader';
