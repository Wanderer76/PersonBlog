import { memo } from 'react';
import { Link } from 'react-router-dom';
import { Playlist } from '@/entities/playlist/types';
import '@/entities/playlist/ui/PlaylistCard/PlaylistCard.css';
import { Button } from '@/shared/ui/Button/Button';

interface PlaylistCardProps {
  playlist: Playlist;
  onRemove?: (id: string) => void;
}

export const PlaylistCard = memo(({ playlist, onRemove }: PlaylistCardProps) => {
  return (
    <article className="playlistCard">
      <Link
        className="playlistCover"
        to={`/playlist/${playlist.id}`}
        aria-label={`Открыть плейлист «${playlist.title}»`}
      >
        <img 
          src={playlist.thumbnailUrl} 
          alt={`Обложка плейлиста ${playlist.title}`} 
          loading="lazy"
        />
        <span className="playlistBadge videoCount">
          {playlist.postCount} видео
        </span>
      </Link>

      <div className="playlistInfo">
        <h3 className="playlistTitle">{playlist.title}</h3>
        {onRemove && (
          <div className="playlistActions">
            <Button variant='primary'>Редактировать</Button>
            <Button
              variant='danger'
              onClick={() => onRemove(playlist.id)}
            >
              Удалить
            </Button>
          </div>
        )}
      </div>
    </article>
  );
});

PlaylistCard.displayName = 'PlaylistCard';
