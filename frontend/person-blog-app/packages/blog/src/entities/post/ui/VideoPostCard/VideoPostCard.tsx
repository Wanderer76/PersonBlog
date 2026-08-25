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
        <time className={styles.duration}>{secondsToHumanReadable(post.videoInfo.videoMetadata?.duration ?? 0)}</time>
        {statusText && <span className={styles.status}>{statusText}</span>}
      </div>
      <div className={styles.info}>
        <h3>{post.title || 'Без названия'}</h3>
        <div className={styles.meta}>
          <span>{post.viewCount} просмотров</span>
          <span aria-hidden="true">•</span>
          <time dateTime={post.createdAt}>{new Date(post.createdAt).toLocaleDateString()}</time>
        </div>
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
