import { memo } from 'react';
import { useNavigate } from 'react-router-dom';
import { Playlist } from '@/entities/playlist/types';
import '@/entities/playlist/ui/PlaylistCard/PlaylistCard.css';
import { Button } from '@/shared/ui/Button/Button';

interface PlaylistCardProps {
  playlist: Playlist;
  onRemove: (id: string) => void;
}

export const PlaylistCard = memo(({ playlist, onRemove }: PlaylistCardProps) => {
  const navigate = useNavigate();

  return (
    <article className="playlistCard">
      <div 
        className="playlistCover"
        onClick={(e) => { e.preventDefault(); navigate(`/playlist/${playlist.id}`); }}
        role="button"
        tabIndex={0}
      >
        <img 
          src={playlist.thumbnailUrl} 
          alt={`Обложка плейлиста ${playlist.title}`} 
          loading="lazy"
        />
        <span className="playlistBadge videoCount">
          {playlist.postCount} видео
        </span>
      </div>

      <div className="playlistInfo">
        <h3 className="playlistTitle">{playlist.title}</h3>
        <div className="playlistActions">
          <Button variant='primary'>Редактировать</Button>
          <Button 
            variant='danger'
            onClick={() => onRemove(playlist.id)}
          >
            Удалить
          </Button>
        </div>
      </div>
    </article>
  );
});

PlaylistCard.displayName = 'PlaylistCard';