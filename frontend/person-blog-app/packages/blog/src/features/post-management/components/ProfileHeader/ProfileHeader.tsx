import { memo } from 'react';
import { useNavigate } from 'react-router-dom';
import { ProfileData } from '../../../../entities/profile/types';
import { Button } from '../../../../shared/ui/Button/Button';
import { getLocalDateTime } from '@/shared/LocalDate';
import DefaultProfileIcon from '@/defaultProfilePic.png';
import './ProfileHeader.css';

interface ProfileHeaderProps {
  profile: ProfileData;
  hasBlog: boolean;
  onLogout: () => void;
  onEditBlog: () => void;
}

export const ProfileHeader = memo(({ 
  profile, 
  hasBlog, 
  onLogout, 
  onEditBlog 
}: ProfileHeaderProps) => {
  const navigate = useNavigate();

  return (
    <header className="profileHeader">
      <div className="avatarSection">
        <Button 
          variant="secondary"
          onClick={onEditBlog}
        >
          {hasBlog ? 'Редактировать профиль' : 'Создать блог'}
        </Button>

        <div className="avatarWrapper">
          <img
            src={profile.photoUrl ?? DefaultProfileIcon}
            alt="Аватар пользователя"
            className="profileAvatar"
          />
          <button 
            className="avatarEditBtn"
            aria-label="Изменить аватар"
            // onClick={handleAvatarChange} // реализовать при необходимости
          >
            ✏️
          </button>
        </div>

        <div className="profileInfo">
          <h1 className="blogTitle">{profile.name || 'Без имени'}</h1>
          <div className="profileMeta">
            {profile.createdAt && (
              <time className="registration-date" dateTime={profile.createdAt}>
                📅 Зарегистрирован: {getLocalDateTime(profile.createdAt)}
              </time>
            )}
            <span className="posts-count">
              📝 Постов: {profile.totalPostsCount}
            </span>
          </div>
        </div>
      </div>

      <Button variant="danger" onClick={onLogout}>
        Выход
      </Button>
    </header>
  );
});

ProfileHeader.displayName = 'ProfileHeader';