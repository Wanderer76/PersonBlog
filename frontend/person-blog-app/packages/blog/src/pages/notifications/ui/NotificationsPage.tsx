import { useCallback, useEffect, useState } from 'react';
import { NOTIFICATION_CREATED_EVENT, useNotifications } from '@/app/providers/notificationContext';
import { notificationApi, type NotificationDelivery, type NotificationItem } from '@/shared/api/notifications';
import { PageShell } from '@/widgets/page-shell';
import styles from './NotificationsPage.module.css';

const copy: Record<string, { title: string; description: (item: NotificationItem) => string }> = {
  PostPublished: {
    title: 'Новая публикация',
    description: (item) => item.data.title ? `Опубликовано: «${item.data.title}»` : 'В подписанном блоге вышла новая публикация.',
  },
  VideoProcessingCompleted: {
    title: 'Видео готово',
    description: () => 'Обработка видео завершена. Оно готово к просмотру.',
  },
  VideoProcessingFailed: {
    title: 'Не удалось обработать видео',
    description: () => 'Попробуйте загрузить видео повторно или выбрать другой файл.',
  },
  CommentReply: {
    title: 'Новый ответ',
    description: () => 'Пользователь ответил на ваш комментарий.',
  },
  ConferenceInvitation: {
    title: 'Приглашение в конференцию',
    description: () => 'Вас пригласили присоединиться к конференции.',
  },
};

const notificationKinds = [
  'VideoProcessingCompleted',
  'VideoProcessingFailed',
  'PostPublished',
  'CommentReply',
  'ConferenceInvitation',
] as const;

const targetTypes = ['Post', 'Comment', 'ConferenceInvitation'] as const;

const enumName = (value: string | number, names: readonly string[]) =>
  typeof value === 'number' ? names[value] || String(value) : value;

const fromDelivery = (delivery: NotificationDelivery): NotificationItem => ({
  id: delivery.notificationId,
  kind: enumName(delivery.kind, notificationKinds),
  businessId: delivery.businessId,
  actorUserId: delivery.actorUserId,
  target: {
    ...delivery.target,
    type: enumName(delivery.target.type, targetTypes),
  },
  templateKey: delivery.templateKey,
  templateVersion: delivery.templateVersion,
  data: delivery.data,
  createdAt: delivery.createdAt,
  readAt: null,
  expiresAt: delivery.expiresAt,
});

const formatDate = (value: string) => new Intl.DateTimeFormat('ru-RU', {
  day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit',
}).format(new Date(value));

const NotificationsPage = () => {
  const [items, setItems] = useState<NotificationItem[]>([]);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [snapshot, setSnapshot] = useState<string | null>(null);
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { decrementUnreadCount, refreshUnreadCount } = useNotifications();

  const load = useCallback(async (cursor?: string | null) => {
    if (cursor) setLoadingMore(true);
    else setLoading(true);
    setError(null);
    try {
      const page = await notificationApi.list(cursor, unreadOnly);
      setItems((current) => cursor ? [...current, ...page.items] : page.items);
      setNextCursor(page.nextCursor || null);
      if (!cursor) setSnapshot(page.snapshot);
    } catch {
      setError('Не удалось загрузить уведомления. Попробуйте ещё раз.');
    } finally {
      setLoading(false);
      setLoadingMore(false);
    }
  }, [unreadOnly]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    const receive = (event: Event) => {
      const notification = (event as CustomEvent<NotificationDelivery>).detail;
      if (!notification) return;
      setItems((current) => current.some((item) => item.id === notification.notificationId)
        ? current
        : [fromDelivery(notification), ...current]);
    };
    window.addEventListener(NOTIFICATION_CREATED_EVENT, receive);
    return () => window.removeEventListener(NOTIFICATION_CREATED_EVENT, receive);
  }, [unreadOnly]);

  const markRead = async (item: NotificationItem) => {
    if (item.readAt) return;
    try {
      await notificationApi.markRead(item.id);
      setItems((current) => current
        .map((entry) => entry.id === item.id ? { ...entry, readAt: new Date().toISOString() } : entry)
        .filter((entry) => !unreadOnly || !entry.readAt));
      decrementUnreadCount();
    } catch {
      setError('Не удалось отметить уведомление прочитанным.');
    }
  };

  const markAllRead = async () => {
    if (!snapshot) return;
    try {
      await notificationApi.markAllRead(snapshot);
      setItems((current) => unreadOnly ? [] : current.map((item) => ({
        ...item, readAt: item.readAt || new Date().toISOString(),
      })));
      await refreshUnreadCount();
    } catch {
      setError('Не удалось отметить уведомления прочитанными.');
    }
  };

  return (
    <PageShell contentClassName={styles.page}>
      <section className={styles.panel} aria-labelledby="notifications-title">
        <div className={styles.headingRow}>
          <div>
            <h1 id="notifications-title">Уведомления</h1>
            <p>Новости ваших подписок, ответы и состояние обработки видео.</p>
          </div>
          <button type="button" className={styles.markAllButton} onClick={markAllRead}
            disabled={!snapshot || !items.some((item) => !item.readAt)}>
            Прочитать все
          </button>
        </div>

        <label className={styles.filter}>
          <input type="checkbox" checked={unreadOnly} onChange={(event) => setUnreadOnly(event.target.checked)} />
          Только непрочитанные
        </label>

        {error && <div className={styles.error} role="alert">{error}</div>}
        {loading ? (
          <div className={styles.state}>Загрузка уведомлений...</div>
        ) : items.length === 0 ? (
          <div className={styles.empty}>
            <span aria-hidden="true">✓</span>
            <h2>{unreadOnly ? 'Всё прочитано' : 'Уведомлений пока нет'}</h2>
            <p>{unreadOnly ? 'Новые уведомления появятся здесь.' : 'Мы сообщим о новых публикациях, ответах и приглашениях.'}</p>
          </div>
        ) : (
          <div className={styles.list}>
            {items.map((item) => {
              const text = copy[item.kind] || { title: 'Уведомление', description: () => 'У вас новое событие.' };
              return (
                <article key={item.id} className={`${styles.item} ${!item.readAt ? styles.unread : ''}`}>
                  <span className={styles.kindIcon} aria-hidden="true">{iconFor(item.kind)}</span>
                  <div className={styles.itemBody}>
                    <div className={styles.itemHeader}>
                      <h2>{text.title}</h2>
                      <time dateTime={item.createdAt}>{formatDate(item.createdAt)}</time>
                    </div>
                    <p>{text.description(item)}</p>
                  </div>
                  {!item.readAt && (
                    <button type="button" className={styles.readButton} onClick={() => void markRead(item)}>
                      Прочитано
                    </button>
                  )}
                </article>
              );
            })}
          </div>
        )}

        {nextCursor && (
          <button type="button" className={styles.moreButton} disabled={loadingMore}
            onClick={() => void load(nextCursor)}>
            {loadingMore ? 'Загрузка...' : 'Показать ещё'}
          </button>
        )}
      </section>
    </PageShell>
  );
};

const iconFor = (kind: string) => ({
  PostPublished: '▤', VideoProcessingCompleted: '▶', VideoProcessingFailed: '!',
  CommentReply: '↩', ConferenceInvitation: '⌁',
}[kind] || '•');

export default NotificationsPage;
