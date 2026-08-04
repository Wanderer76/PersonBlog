import { KeyboardEvent, memo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { UserPostInfoModel } from '@/lib/api/generated/models';
import { useServiceWorkerMessage } from '../../../../shared/hooks/useServiceWorkerMessage';
import './PostCard.css';
import { secondsToHumanReadable } from '@/shared/LocalDate';
import { Button } from '@/shared/ui/Button/Button';

interface PostCardProps {
  post: UserPostInfoModel;
  isLast?: boolean;
  onRemove: (id: string) => void;
  observeRef?: (node: HTMLElement | null) => void;
}

export const PostCard = memo(({ post, isLast, onRemove, observeRef }: PostCardProps) => {
  const navigate = useNavigate();
  const [uploadProgress, setUploadProgress] = useState(0);

  useServiceWorkerMessage((event) => {
    if (event.data.type === 'CHUNK_UPLOADED' && event.data.payload.postId === post.id) {
      const { chunkNumber, totalChunks } = event.data.payload;
      setUploadProgress(Math.round((chunkNumber / totalChunks) * 100));
    }
  }, [post.id]);

  const processState = post.videoInfo?.processState;
  const isClickable = processState === 1 && Boolean(post.id);
  const statusText = processState === 1
    ? 'Опубликовано'
    : processState === 2
      ? `Загрузка ${uploadProgress}%`
      : processState === 3
        ? 'Ошибка обработки'
        : 'Черновик';

  const openPost = () => {
    if (isClickable) navigate(`/videoPage/${post.id}`);
  };

  const handleKeyDown = (event: KeyboardEvent<HTMLElement>) => {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      openPost();
    }
  };

  return (
    <article className="postCard" ref={isLast && observeRef ? observeRef : undefined}>
      <div className="postThumbnail" onClick={openPost} onKeyDown={handleKeyDown}
        role={isClickable ? 'button' : undefined} tabIndex={isClickable ? 0 : -1}>
        <img src={post.videoInfo?.previewUrl || ''} alt={post.title || 'Превью публикации'} loading="lazy" />
        <time className="videoDuration">
          {secondsToHumanReadable(post.videoInfo?.videoMetadata?.duration ?? 0)}
        </time>
        <span className={`postStatus status-${processState}`}>{statusText}</span>
      </div>

      <div className="postContent">
        <h3 className="postTitle" onClick={openPost} onKeyDown={handleKeyDown}
          role={isClickable ? 'button' : undefined} tabIndex={isClickable ? 0 : -1}>
          {post.title || 'Без названия'}
        </h3>
        <div className="postMeta">
          <div className="postStats">
            <span aria-label={`Просмотры: ${post.viewCount ?? 0}`}>👁 {post.viewCount ?? 0}</span>
            <time dateTime={post.createdAt || ''}>
              📅 {post.createdAt ? new Date(post.createdAt).toLocaleDateString() : '—'}
            </time>
          </div>
          <div className="postActions">
            <Button variant="primary" onClick={(event) => {
              event.stopPropagation();
              if (post.id) navigate(`post/edit/${post.id}`);
            }}>Редактировать</Button>
            <Button variant="danger" onClick={(event) => {
              event.stopPropagation();
              if (post.id) onRemove(post.id);
            }}>Удалить</Button>
          </div>
        </div>
      </div>
    </article>
  );
});

PostCard.displayName = 'PostCard';
