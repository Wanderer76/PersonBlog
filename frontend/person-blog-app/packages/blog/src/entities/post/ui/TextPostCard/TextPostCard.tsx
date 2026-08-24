import { memo, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { sanitizeTextPostHtml } from '../../lib/sanitizeTextPostHtml';
import { UserPostInfoModel } from '@/shared/api/generated/models';
import { Button } from '@/shared/ui/Button/Button';
import styles from '@/entities/post/ui/TextPostCard/TextPostCard.module.css';

interface TextPostCardProps {
  post: UserPostInfoModel;
  isLast: boolean;
  observeRef: (node: HTMLElement | null) => void;
  onRemove: (id: string) => Promise<void>;
}

export const TextPostCard = memo(({ post, isLast, observeRef, onRemove }: TextPostCardProps) => {
  const navigate = useNavigate();
  const [isRemoving, setIsRemoving] = useState(false);
  const sanitizedHtml = useMemo(
    () => sanitizeTextPostHtml(post.textInfo?.text ?? ''),
    [post.textInfo?.text]
  );

  const handleRemove = async () => {
    if (!post.id || isRemoving || !window.confirm('Удалить публикацию? Это действие нельзя отменить.')) return;
    setIsRemoving(true);
    try {
      await onRemove(post.id);
    } finally {
      setIsRemoving(false);
    }
  };

  return (
    <article className={styles.card} ref={isLast ? observeRef : undefined}>
      <header className={styles.header}>
        <h3>{post.title || 'Без названия'}</h3>
        <details className={styles.menu}>
          <summary aria-label="Действия с публикацией" title="Действия с публикацией">
            <span aria-hidden="true">•••</span>
          </summary>
          <div className={styles.menuPopover}>
            <Button
              type="button"
              variant="secondary"
              disabled={!post.id || isRemoving}
              onClick={() => post.id && navigate(`/profile/textPost/edit/${post.id}`)}
            >
              Редактировать
            </Button>
            <Button type="button" variant="danger" disabled={isRemoving} onClick={() => void handleRemove()}>
              {isRemoving ? 'Удаление…' : 'Удалить'}
            </Button>
          </div>
        </details>
      </header>
      <div className={styles.content} dangerouslySetInnerHTML={{ __html: sanitizedHtml }} />
      {post.createdAt && (
        <footer className={styles.footer}>
          <time dateTime={post.createdAt}>{new Date(post.createdAt).toLocaleDateString()}</time>
        </footer>
      )}
    </article>
  );
});

TextPostCard.displayName = 'TextPostCard';
