import { memo, type Ref } from 'react';
import { Link } from 'react-router-dom';
import type { Post } from '@/entities/post/types';
import { secondsToHumanReadable } from '@/shared/lib/date';
import styles from './VideoPostCard.module.css';

interface VideoPostCardProps {
  post: Post;
  observeRef?: Ref<HTMLElement>;
}

const getStatusText = (post: Post) => {
  if (post.state === 0) return 'В обработке';
  if (post.state === 2) return 'Загрузка';
  if (post.state === 3) return post.errorMessage || 'Ошибка обработки';
  return null;
};

export const VideoPostCard = memo(({ post, observeRef }: VideoPostCardProps) => {
  const statusText = getStatusText(post);
  const duration = post.videoInfo.videoMetadata?.duration;
  const hasMetadata = post.viewCount !== undefined || Boolean(post.createdAt);
  const content = (
    <>
      <div className={styles.thumbnail}>
        {post.videoInfo.previewUrl ? (
          <img src={post.videoInfo.previewUrl} alt={`Превью видео «${post.title}»`} loading="lazy" />
        ) : (
          <div className={styles.thumbnailFallback}>
            <span aria-hidden="true">▶</span>
            <small>Превью недоступно</small>
          </div>
        )}
        {duration !== undefined && (
          <time className={styles.duration}>{secondsToHumanReadable(duration)}</time>
        )}
        {statusText && <span className={styles.status}>{statusText}</span>}
      </div>
      <div className={styles.info}>
        <h3>{post.title || 'Без названия'}</h3>
        {hasMetadata && (
          <div className={styles.meta}>
            {post.viewCount !== undefined && <span>{post.viewCount} просмотров</span>}
            {post.viewCount !== undefined && post.createdAt && <span aria-hidden="true">•</span>}
            {post.createdAt && (
              <time dateTime={post.createdAt}>{new Date(post.createdAt).toLocaleDateString()}</time>
            )}
          </div>
        )}
        {post.description && <p>{post.description}</p>}
      </div>
    </>
  );

  return (
    <article ref={observeRef} className={styles.card}>
      {post.state === 1 ? (
        <Link className={styles.link} to={`/videoPage/${post.id}`} aria-label={`Открыть видео «${post.title}»`}>
          {content}
        </Link>
      ) : content}
    </article>
  );
});

VideoPostCard.displayName = 'VideoPostCard';
