import { memo, useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Post } from '../../../../entities/post/types';
import { useServiceWorkerMessage } from '../../../../shared/hooks/useServiceWorkerMessage';
import './PostCard.css';
import { secondsToHumanReadable } from '@/shared/LocalDate';
import { Button } from '@/shared/ui/Button/Button';

interface PostCardProps {
  post: Post;
  isLast?: boolean;
  onRemove: (id: string) => void;
  observeRef?: (node: HTMLElement | null) => void;
}

export const PostCard = memo(({ post, isLast, onRemove, observeRef }: PostCardProps) => {
  const navigate = useNavigate();
  const [uploadProgress, setUploadProgress] = useState(0);

  // Хук для подписки на сообщения от Service Worker
  useServiceWorkerMessage((event) => {
    if (event.data.type === 'CHUNK_UPLOADED' && event.data.payload.postId === post.id) {
      const { chunkNumber, totalChunks } = event.data.payload;
      setUploadProgress(Math.round((chunkNumber / totalChunks) * 100));
    }
  }, [post.id]);

  const isClickable = post.videoInfo.processState === 1;
  const statusText =
    post.videoInfo.processState === 1 ? 'Опубликовано' :
      post.videoInfo.state === 0 ? 'В обработке' :
        post.videoInfo.state === 2 ? `Загрузка ${uploadProgress}%` :
          post.errorMessage || '';

  const handleClick = (e: React.MouseEvent) => {
    e.preventDefault();
    if (isClickable) navigate(`/video/${post.id}`);
  };

  return (
    <article
      className="postCard"
      ref={isLast && observeRef ? observeRef : undefined}
    >
      <div
        className="postThumbnail"
        onClick={handleClick}
        role={isClickable ? 'button' : undefined}
        tabIndex={isClickable ? 0 : -1}
      >
        <img
          src={post.videoInfo.previewUrl}
          alt={post.title}
          loading="lazy"
        />
        <time className="videoDuration">
          {secondsToHumanReadable(post.videoInfo.videoMetadata?.duration ?? 0)}
        </time>
        {post.type === 1 && (
          <span className={`postStatus status-${post.videoInfo.processState}`}>
            {statusText}
          </span>
        )}
      </div>

      <div className="postContent">
        <h3
          className="postTitle"
          onClick={handleClick}
          role={isClickable ? 'button' : undefined}
          tabIndex={isClickable ? 0 : -1}
        >
          {post.title}
        </h3>

        <div className="postMeta">
          <div className="postStats">
            <span aria-label={`Просмотры: ${post.viewCount}`}>👁 {post.viewCount}</span>
            <time dateTime={post.createdAt}>
              📅 {new Date(post.createdAt).toLocaleDateString()}
            </time>
          </div>
          <div className="postActions">
            <Button
              variant={'primary'}
              onClick={(e) => { e.stopPropagation(); navigate(`post/edit/${post.id}`); }}>
              Редактировать
            </Button >
            <Button variant={'danger'} onClick={(e) => { e.stopPropagation(); onRemove(post.id); }}>
              Удалить
            </Button>

          </div>
        </div>
      </div>
    </article>
  );
});

PostCard.displayName = 'PostCard';