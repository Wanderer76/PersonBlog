import { KeyboardEvent, memo, useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { UserPostInfoModel } from '@/shared/api/generated/models';
import { useServiceWorkerMessage } from '@/shared/hooks/useServiceWorkerMessage';
import { secondsToHumanReadable } from '@/shared/lib/date';
import { VideoProcessingProgress } from '@/entities/profile/types';
import '@/entities/post/ui/PostCard/PostCard.css';

interface PostCardProps {
  post: UserPostInfoModel;
  isLast?: boolean;
  onRemove: (id: string) => void;
  observeRef?: (node: HTMLElement | null) => void;
  processingProgress?: VideoProcessingProgress;
}

export const PostCard = memo(({ post, isLast, onRemove, observeRef, processingProgress }: PostCardProps) => {
  const navigate = useNavigate();
  const [uploadProgress, setUploadProgress] = useState(0);
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useServiceWorkerMessage((message) => {
    if (message.type === 'CHUNK_UPLOADED' && message.payload.postId === post.id) {
      const { chunkNumber, totalChunks } = message.payload;
      setUploadProgress(Math.round((chunkNumber / totalChunks) * 100));
    }
  }, [post.id]);

  useEffect(() => {
    if (!isMenuOpen) return;

    const closeMenu = (event: MouseEvent) => {
      if (!menuRef.current?.contains(event.target as Node)) setIsMenuOpen(false);
    };
    const closeMenuOnEscape = (event: globalThis.KeyboardEvent) => {
      if (event.key === 'Escape') setIsMenuOpen(false);
    };

    document.addEventListener('mousedown', closeMenu);
    document.addEventListener('keydown', closeMenuOnEscape);
    return () => {
      document.removeEventListener('mousedown', closeMenu);
      document.removeEventListener('keydown', closeMenuOnEscape);
    };
  }, [isMenuOpen]);

  const processState = post.videoInfo?.processState;
  const displayedProgress = Math.round(processingProgress?.percent ?? uploadProgress);
  const isClickable = processState === 1 && Boolean(post.id);
  const statusText = processingProgress?.status === 'failed'
    ? 'Ошибка обработки'
    : processingProgress?.status === 'completed'
      ? 'Обработка завершена'
      : processingProgress?.status === 'processing'
        ? `Обработка ${displayedProgress}%`
        : processState === 1
          ? 'Опубликовано'
          : processState === 2
            ? `Загрузка ${displayedProgress}%`
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
    <article
      className={`postCard${isMenuOpen ? ' postCardMenuOpen' : ''}`}
      ref={isLast && observeRef ? observeRef : undefined}
    >
      <div className="postThumbnail" onClick={openPost} onKeyDown={handleKeyDown}
        role={isClickable ? 'button' : undefined} tabIndex={isClickable ? 0 : -1}>
        {post.videoInfo?.previewUrl ? (
          <img src={post.videoInfo.previewUrl} alt={post.title || 'Превью публикации'} loading="lazy" />
        ) : (
          <div className="postThumbnailFallback" aria-label="Превью пока недоступно">
            <span aria-hidden="true">▶</span>
            <small>Превью недоступно</small>
          </div>
        )}
        <time className="videoDuration">
          {secondsToHumanReadable(post.videoInfo?.videoMetadata?.duration ?? 0)}
        </time>
        <span className={`postStatus status-${processState}`}>{statusText}</span>
        {processState === 2 && (
          <div className="videoProcessingProgress" aria-label={`Прогресс обработки видео: ${displayedProgress}%`}>
            <div style={{ width: `${displayedProgress}%` }} />
          </div>
        )}
      </div>

      <div className="postContent">
        <div className="postTitleRow">
          <h3 className="postTitle" onClick={openPost} onKeyDown={handleKeyDown}
            role={isClickable ? 'button' : undefined} tabIndex={isClickable ? 0 : -1}>
            {post.title || 'Без названия'}
          </h3>
          <div className="postMenu" ref={menuRef}>
            <button
              className="postMenuTrigger"
              type="button"
              aria-label="Действия с публикацией"
              aria-expanded={isMenuOpen}
              aria-haspopup="menu"
              onClick={() => setIsMenuOpen(current => !current)}
            >
              <span aria-hidden="true">•••</span>
            </button>
            {isMenuOpen && (
              <div className="postMenuPopover" role="menu">
                <button type="button" role="menuitem" onClick={() => {
                  setIsMenuOpen(false);
                  if (post.id) navigate(`post/edit/${post.id}`);
                }}>
                  Редактировать
                </button>
                <button className="postMenuDanger" type="button" role="menuitem" onClick={() => {
                  setIsMenuOpen(false);
                  if (post.id && window.confirm('Удалить публикацию? Это действие нельзя отменить.')) {
                    onRemove(post.id);
                  }
                }}>
                  Удалить
                </button>
              </div>
            )}
          </div>
        </div>
        <div className="postMeta">
          <div className="postStats">
            <span aria-label={`Просмотры: ${post.viewCount ?? 0}`}>
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6Z" />
                <circle cx="12" cy="12" r="2.5" />
              </svg>
              {post.viewCount ?? 0}
            </span>
            <time dateTime={post.createdAt || ''}>
              {post.createdAt ? new Date(post.createdAt).toLocaleDateString() : '—'}
            </time>
          </div>
        </div>
      </div>
    </article>
  );
});

PostCard.displayName = 'PostCard';
