import axios from 'axios';
import {
  useEffect,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent as ReactKeyboardEvent,
  type MouseEvent as ReactMouseEvent,
  type ReactNode,
} from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { getTextPostInlineMediaIds, sanitizeTextPostHtml } from '@/entities/post';
import { JwtTokenService } from '@/shared/auth';
import type { TextPostDetailResponse, TextPostMedia } from '@/shared/api/generated/models';
import { getPost } from '@/shared/api/generated/post/post';
import { getSubscriber } from '@/shared/api/generated/subscriber/subscriber';
import { getTextPost } from '@/shared/api/generated/text-post/text-post';
import defaultProfilePic from '@/shared/assets/defaultProfilePic.png';
import { Button } from '@/shared/ui/Button/Button';
import { PageShell } from '@/widgets/page-shell';
import styles from './TextPostPage.module.css';

type Reaction = true | false;
type PreviewImage = { src: string; alt: string };

const postApi = getPost();
const subscriberApi = getSubscriber();
const textPostApi = getTextPost();
const dateFormatter = new Intl.DateTimeFormat('ru-RU', {
  day: 'numeric',
  month: 'long',
  year: 'numeric',
});
const numberFormatter = new Intl.NumberFormat('ru-RU');
const compactNumberFormatter = new Intl.NumberFormat('ru-RU', {
  notation: 'compact',
  maximumFractionDigits: 1,
});

const TextPostPage = () => {
  const { postId } = useParams<{ postId: string }>();
  const navigate = useNavigate();
  const [post, setPost] = useState<TextPostDetailResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState('');
  const [actionMessage, setActionMessage] = useState('');
  const [reactionPending, setReactionPending] = useState(false);
  const [subscriptionPending, setSubscriptionPending] = useState(false);
  const [reloadKey, setReloadKey] = useState(0);
  const [previewImage, setPreviewImage] = useState<PreviewImage | null>(null);
  const lightboxCloseRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!previewImage) return;

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    lightboxCloseRef.current?.focus();

    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setPreviewImage(null);
    };
    document.addEventListener('keydown', closeOnEscape);
    return () => {
      document.body.style.overflow = previousOverflow;
      document.removeEventListener('keydown', closeOnEscape);
    };
  }, [previewImage]);

  useEffect(() => {
    const controller = new AbortController();
    let isActive = true;

    setIsLoading(true);
    setLoadError('');
    setActionMessage('');
    setPost(null);

    if (!postId) {
      setLoadError('Некорректный идентификатор публикации.');
      setIsLoading(false);
      return () => controller.abort();
    }

    const loadPost = async () => {
      try {
        const response = await textPostApi.getApiTextPostPostId(
          postId,
          { signal: controller.signal },
        );
        if (!isActive) return;

        if (!response.data.id || !response.data.author?.blogId || !response.data.viewer) {
          throw new Error('Gateway returned an incomplete text post response.');
        }

        setPost(response.data);
        if (!response.data.viewer?.isViewed) {
          void textPostApi.postApiTextPostPostIdView(
            postId,
            { signal: controller.signal },
          )
            .then(() => {
              if (!isActive) return;
              setPost((current) => current ? {
                ...current,
                viewCount: (current.viewCount ?? 0) + 1,
                viewer: { ...current.viewer, isViewed: true },
              } : current);
            })
            .catch((error: unknown) => {
              if (!controller.signal.aborted) {
                console.warn('Не удалось зарегистрировать просмотр текстового поста:', error);
              }
            });
        }
      } catch (error: unknown) {
        if (!isActive || controller.signal.aborted) return;
        const status = axios.isAxiosError(error) ? error.response?.status : undefined;
        setLoadError(status === 404
          ? 'Публикация не найдена или была удалена.'
          : status === 403
            ? 'У вас нет доступа к этой публикации.'
            : 'Не удалось загрузить публикацию. Проверьте соединение и попробуйте ещё раз.');
      } finally {
        if (isActive) setIsLoading(false);
      }
    };

    void loadPost();
    return () => {
      isActive = false;
      controller.abort();
    };
  }, [postId, reloadKey]);

  const sanitizedHtml = useMemo(
    () => sanitizeTextPostHtml(post?.text ?? '', post?.media ?? []),
    [post?.text, post?.media],
  );
  const inlineMediaIds = useMemo(
    () => getTextPostInlineMediaIds(post?.text ?? ''),
    [post?.text],
  );

  const handleBack = () => {
    if (window.history.length > 1) {
      navigate(-1);
      return;
    }
    navigate('/');
  };

  const openImageFromTarget = (target: EventTarget | null) => {
    if (!(target instanceof HTMLImageElement) || !target.currentSrc) return;
    setPreviewImage({ src: target.currentSrc, alt: target.alt });
  };

  const handleArticleImageClick = (event: ReactMouseEvent<HTMLElement>) => {
    openImageFromTarget(event.target);
  };

  const handleArticleImageKeyDown = (event: ReactKeyboardEvent<HTMLElement>) => {
    if (event.key !== 'Enter' && event.key !== ' ') return;
    if (!(event.target instanceof HTMLImageElement)) return;
    event.preventDefault();
    openImageFromTarget(event.target);
  };

  const handleReaction = async (reaction: Reaction) => {
    if (!post?.id || reactionPending) return;
    setReactionPending(true);
    setActionMessage('');

    try {
      await postApi.postApiPostSetReactionPostId(post.id, { isLike: reaction });
      setPost((current) => {
        if (!current) return current;
        const previousReaction = current.viewer?.isLike;
        const nextReaction = previousReaction === reaction ? null : reaction;
        return {
          ...current,
          likeCount: Math.max(0, (current.likeCount ?? 0)
            - (previousReaction === true ? 1 : 0)
            + (nextReaction === true ? 1 : 0)),
          dislikeCount: Math.max(0, (current.dislikeCount ?? 0)
            - (previousReaction === false ? 1 : 0)
            + (nextReaction === false ? 1 : 0)),
          viewer: { ...current.viewer, isLike: nextReaction },
        };
      });
    } catch (error) {
      console.error('Не удалось сохранить реакцию:', error);
      setActionMessage('Не удалось сохранить реакцию. Попробуйте ещё раз.');
    } finally {
      setReactionPending(false);
    }
  };

  const handleSubscribe = async () => {
    if (!post?.author?.blogId || subscriptionPending) return;
    if (!JwtTokenService.isAuth()) {
      await JwtTokenService.redirectToAuth(window.location.href);
      return;
    }

    const wasSubscribed = post.viewer?.isSubscribed ?? false;
    setSubscriptionPending(true);
    setActionMessage('');
    try {
      if (wasSubscribed) {
        await subscriberApi.postApiSubscriberUnsubscribeBlogId(post.author.blogId);
      } else {
        await subscriberApi.postApiSubscriberSubscribeBlogId(post.author.blogId);
      }
      setPost((current) => current ? {
        ...current,
        author: {
          ...current.author,
          subscribersCount: Math.max(0, (current.author?.subscribersCount ?? 0) + (wasSubscribed ? -1 : 1)),
        },
        viewer: { ...current.viewer, isSubscribed: !wasSubscribed },
      } : current);
    } catch (error) {
      console.error('Не удалось изменить подписку:', error);
      setActionMessage('Не удалось изменить подписку. Попробуйте ещё раз.');
    } finally {
      setSubscriptionPending(false);
    }
  };

  const handleShare = async () => {
    if (!post) return;
    try {
      if (navigator.share) {
        await navigator.share({ title: post.title ?? 'Публикация', url: window.location.href });
      } else {
        await navigator.clipboard.writeText(window.location.href);
        setActionMessage('Ссылка скопирована в буфер обмена.');
      }
    } catch (error) {
      if (!(error instanceof DOMException && error.name === 'AbortError')) {
        setActionMessage('Не удалось поделиться ссылкой.');
      }
    }
  };

  if (isLoading) {
    return <TextPostState>Загружаем публикацию…</TextPostState>;
  }

  if (loadError || !post) {
    return (
      <TextPostState title="Не удалось открыть публикацию">
        <p>{loadError || 'Публикация недоступна.'}</p>
        <div className={styles.stateActions}>
          <Button type="button" onClick={() => setReloadKey((value) => value + 1)}>Повторить</Button>
          <Button type="button" variant="secondary" onClick={handleBack}>Назад</Button>
        </div>
      </TextPostState>
    );
  }

  const mediaItems = (post.media ?? []).filter((media) =>
    Boolean(media.url) && !inlineMediaIds.has(media.id?.toLowerCase() ?? ''));
  const heroMedia = mediaItems.find((media) => media.contentType?.startsWith('image/'));
  const remainingMedia = heroMedia
    ? mediaItems.filter((media) => media.id !== heroMedia.id)
    : mediaItems;
  const categoryLabel = (post.categories ?? [])
    .map((category) => category.title)
    .filter(Boolean)
    .join(' · ');

  return (
    <PageShell className={styles.shell} contentClassName={styles.page}>
      <div className={styles.layout}>
        <article
          className={styles.article}
          onClick={handleArticleImageClick}
          onKeyDown={handleArticleImageKeyDown}
        >
          <header className={styles.articleHeader}>
            <button className={styles.backButton} type="button" onClick={handleBack}>
              <ArrowLeftIcon />
              Назад
            </button>

            {categoryLabel && <span className={styles.category}>{categoryLabel}</span>}
            <h1>{post.title || 'Без названия'}</h1>
            {post.lead && <p className={styles.lead}>{post.lead}</p>}
            <div className={styles.meta}>
              {post.createdAt && (
                <time dateTime={post.createdAt}>{dateFormatter.format(new Date(post.createdAt))}</time>
              )}
              <span aria-hidden="true" />
              <span>{Math.max(1, post.estimatedReadingTimeMinutes ?? 1)} мин чтения</span>
              <span aria-hidden="true" />
              <span>{numberFormatter.format(post.viewCount ?? 0)} просмотров</span>
            </div>
          </header>

          {heroMedia && (
            <div className={styles.hero}>
              <img
                src={heroMedia.url!}
                alt={heroMedia.name ?? ''}
                role="button"
                tabIndex={0}
                aria-label={`Открыть изображение ${heroMedia.name ?? ''}`.trim()}
              />
            </div>
          )}

          {sanitizedHtml && (
            <div className={styles.content} dangerouslySetInnerHTML={{ __html: sanitizedHtml }} />
          )}

          {remainingMedia.length > 0 && (
            <section className={styles.mediaList} aria-label="Медиа публикации">
              {remainingMedia.map((media, index) => (
                <PostMedia key={media.id ?? media.url ?? index} media={media} />
              ))}
            </section>
          )}

          <footer className={styles.articleFooter}>
            <div className={styles.reactions} aria-label="Оценка статьи">
              <button
                className={`${styles.reactionButton} ${post.viewer?.isLike === true ? styles.reactionActive : ''}`}
                type="button"
                disabled={reactionPending}
                aria-pressed={post.viewer?.isLike === true}
                onClick={() => void handleReaction(true)}
              >
                <LikeIcon />
                <span>Нравится</span>
                <strong>{numberFormatter.format(post.likeCount ?? 0)}</strong>
              </button>
              <button
                className={`${styles.reactionButton} ${styles.dislikeButton} ${post.viewer?.isLike === false ? styles.dislikeActive : ''}`}
                type="button"
                disabled={reactionPending}
                aria-pressed={post.viewer?.isLike === false}
                onClick={() => void handleReaction(false)}
              >
                <DislikeIcon />
                <span>Не нравится</span>
                <strong>{numberFormatter.format(post.dislikeCount ?? 0)}</strong>
              </button>
            </div>
            <button className={styles.shareButton} type="button" onClick={() => void handleShare()}>
              <ShareIcon />
              Поделиться
            </button>
            {actionMessage && <p className={styles.actionMessage} role="status">{actionMessage}</p>}
          </footer>
        </article>

        <aside className={styles.creator} aria-labelledby="creator-name">
          <p className={styles.creatorEyebrow}>Создатель публикации</p>
          <button
            className={styles.creatorLink}
            type="button"
            onClick={() => navigate(`/channel/${post.author?.blogId}`)}
          >
            <span className={styles.creatorMain}>
              <img src={post.author?.photoUrl || defaultProfilePic} alt="" />
              <span>
                <span id="creator-name">{post.author?.name || 'Автор'}</span>
                <span>{compactNumberFormatter.format(post.author?.subscribersCount ?? 0)} подписчиков</span>
              </span>
            </span>
          </button>
          {post.author?.description && (
            <p className={styles.creatorDescription}>{post.author.description}</p>
          )}
          <Button
            variant={post.viewer?.isSubscribed ? 'secondary' : 'primary'}
            fullWidth
            loading={subscriptionPending}
            aria-pressed={post.viewer?.isSubscribed}
            onClick={() => void handleSubscribe()}
          >
            {post.viewer?.isSubscribed ? 'Вы подписаны' : 'Подписаться'}
          </Button>
          <div className={styles.creatorStats}>
            <div><strong>{numberFormatter.format(post.author?.postsCount ?? 0)}</strong><span>публикаций</span></div>
            <div><strong>{compactNumberFormatter.format(post.author?.totalViewsCount ?? 0)}</strong><span>просмотров</span></div>
          </div>
        </aside>
      </div>
      {previewImage && (
        <div
          className={styles.lightbox}
          role="dialog"
          aria-modal="true"
          aria-label="Полноразмерное изображение"
          onClick={(event) => {
            if (event.target === event.currentTarget) setPreviewImage(null);
          }}
        >
          <button
            ref={lightboxCloseRef}
            className={styles.lightboxClose}
            type="button"
            aria-label="Закрыть изображение"
            onClick={() => setPreviewImage(null)}
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
              <path d="M6 6l12 12M18 6 6 18" />
            </svg>
          </button>
          <figure className={styles.lightboxContent}>
            <img src={previewImage.src} alt={previewImage.alt} />
            {previewImage.alt && <figcaption>{previewImage.alt}</figcaption>}
          </figure>
        </div>
      )}
    </PageShell>
  );
};

const TextPostState = ({ title, children }: { title?: string; children: ReactNode }) => (
  <PageShell className={styles.shell} contentClassName={styles.page}>
    <section className={styles.state} aria-live="polite">
      {title && <h1>{title}</h1>}
      {children}
    </section>
  </PageShell>
);

const PostMedia = ({ media }: { media: TextPostMedia }) => {
  if (!media.url) return null;
  if (media.contentType?.startsWith('image/')) {
    return (
      <figure>
        <img
          src={media.url}
          alt={media.name ?? ''}
          role="button"
          tabIndex={0}
          aria-label={`Открыть изображение ${media.name ?? ''}`.trim()}
        />
        {media.name && <figcaption>{media.name}</figcaption>}
      </figure>
    );
  }
  if (media.contentType?.startsWith('video/')) {
    return <video controls preload="metadata" src={media.url}>Ваш браузер не поддерживает видео.</video>;
  }
  if (media.contentType?.startsWith('audio/')) {
    return <audio controls preload="metadata" src={media.url}>Ваш браузер не поддерживает аудио.</audio>;
  }
  return <a className={styles.mediaFile} href={media.url} target="_blank" rel="noreferrer">Скачать {media.name || 'файл'}</a>;
};

const ArrowLeftIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
    <path d="m15 18-6-6 6-6" />
  </svg>
);

const LikeIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
    <path d="M7 10v11M3 10h4l3-7a2 2 0 0 1 2 2v5h6a2 2 0 0 1 2 2l-2 7a2 2 0 0 1-2 2H7" />
  </svg>
);

const DislikeIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
    <path d="M17 14V3M21 14h-4l-3 7a2 2 0 0 1-2-2v-5H6a2 2 0 0 1-2-2l2-7a2 2 0 0 1 2-2h9" />
  </svg>
);

const ShareIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
    <circle cx="18" cy="5" r="3" /><circle cx="6" cy="12" r="3" /><circle cx="18" cy="19" r="3" />
    <path d="m8.6 10.5 6.8-4M8.6 13.5l6.8 4" />
  </svg>
);

export default TextPostPage;
