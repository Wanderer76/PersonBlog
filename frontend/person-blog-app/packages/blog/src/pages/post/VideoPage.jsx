import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { useEffect, useRef, useState } from 'react';
import VideoPlayer from '../../components/VideoPlayer/VideoPlayer';
import './VideoPage.css';
import { JwtTokenService } from '../../shared/TokenStrorage.js';
import SmallVideoCard from '../../components/VideoCards/SmallVideoCard';
import CommentsList from '../../components/comment/Comment';
import { getVideo } from '@/lib/api/generated/video/video';
import { getRecommendation } from '@/lib/api/generated/recommendation/recommendation';
import { getComments } from '@/lib/api/generated/comments/comments';
import { getSubscriber } from '@/lib/api/generated/subscriber/subscriber';
import { getConferenceRoom } from '@/lib/api/generated/conference-room/conference-room';
import {
    ChannelSummary,
    VideoActionButton,
    VideoDescription,
    VideoMetadata,
    VideoPageState,
    VideoPlayerFrame,
    VideoWatchLayout,
} from '../../components/VideoWatch/VideoWatch';

const videoApi = getVideo();
const recommendationApi = getRecommendation();
const commentsApi = getComments();
const subscriberApi = getSubscriber();
const conferenceRoomApi = getConferenceRoom();
const recommendationsLimit = 40;

const emptyPost = {
    id: null,
    previewUrl: null,
    createdAt: null,
    viewCount: 0,
    likeCount: 0,
    dislikeCount: 0,
    description: '',
    title: '',
    type: 1,
    videoData: {
        id: null,
        length: 0,
        contentType: null,
        objectName: null,
    },
    isProcessed: true,
};

const emptyUserView = {
    isViewed: false,
    isLike: null,
    hasSubscription: false,
};

const VideoPage = function () {
    const { postId } = useParams();
    const location = useLocation();
    const navigate = useNavigate();
    const [post, setPostData] = useState(emptyPost);
    const [blog, setBlog] = useState({});
    const [userView, setUserView] = useState(emptyUserView);
    const [time, setTime] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const [loadError, setLoadError] = useState('');
    const [actionMessage, setActionMessage] = useState('');
    const [reloadKey, setReloadKey] = useState(0);
    const [recommendations, setRecommendations] = useState([]);
    const [comments, setComments] = useState([]);
    const [commentCount, setCommentCount] = useState(0);
    const [commentsError, setCommentsError] = useState(false);
    const [newCommentText, setNewCommentText] = useState('');
    const [reactionPending, setReactionPending] = useState(false);
    const [subscriptionPending, setSubscriptionPending] = useState(false);
    const [conferencePending, setConferencePending] = useState(false);
    const watchedTimeRef = useRef(0);
    const nextThresholdRef = useRef(30);
    const lastCallTimeRef = useRef(0);

    useEffect(() => {
        let isActive = true;
        const urlTimeValue = new URLSearchParams(location.search).get('time');
        const parsedUrlTime = urlTimeValue === null ? null : Number(urlTimeValue);
        const urlTime = Number.isFinite(parsedUrlTime) && parsedUrlTime >= 0 ? parsedUrlTime : null;

        setIsLoading(true);
        setLoadError('');
        setActionMessage('');
        setPostData(emptyPost);
        setBlog({});
        setUserView(emptyUserView);
        setRecommendations([]);
        setComments([]);
        setCommentCount(0);
        setCommentsError(false);
        setNewCommentText('');
        setTime(urlTime);
        watchedTimeRef.current = 0;
        nextThresholdRef.current = 30;
        lastCallTimeRef.current = 0;

        videoApi.getVideoVideoPostId(postId)
            .then((response) => {
                if (!isActive) return;

                if (!response.data?.post || !response.data?.blog) {
                    throw new Error('Видео не найдено');
                }

                const savedTime = Number(response.data.userPostInfo?.watchedTime);
                const resumeTime = urlTime ?? (Number.isFinite(savedTime) && savedTime > 0 ? savedTime : null);

                setPostData(response.data.post);
                setBlog(response.data.blog);
                setUserView(response.data.userPostInfo ?? emptyUserView);
                setTime(resumeTime);
            })
            .catch((error) => {
                if (!isActive) return;
                console.error('Ошибка при загрузке видео:', error);
                setLoadError(error?.response?.status === 404
                    ? 'Видео не найдено или было удалено.'
                    : 'Не удалось загрузить видео. Проверьте соединение и попробуйте ещё раз.');
            })
            .finally(() => {
                if (isActive) setIsLoading(false);
            });

        recommendationApi.getApiV1Feed({
            limit: recommendationsLimit,
            currentPostId: postId,
        })
            .then((response) => {
                if (isActive) setRecommendations(response.data?.items ?? []);
            })
            .catch((error) => {
                if (isActive) console.warn('Не удалось загрузить рекомендации:', error);
            });

        commentsApi.getApiCommentsList({ postId })
            .then((response) => {
                if (!isActive) return;
                setComments(response.data?.comments ?? []);
                setCommentCount(response.data?.count ?? 0);
            })
            .catch((error) => {
                if (isActive) setCommentsError(true);
            });

        return () => {
            isActive = false;
        };
    }, [location.search, postId, reloadKey]);

    const redirectToSignIn = async () => {
        await JwtTokenService.redirectToAuth(window.location.href);
    };

    const handleAddComment = async () => {
        if (!newCommentText.trim()) return;

        try {
            const response = await commentsApi.postApiCommentsCreate({
                postId,
                replyTo: null,
                text: newCommentText.trim(),
            });

            if (response.status === 200) {
                const commentsResponse = await commentsApi.getApiCommentsList({ postId });
                setComments(commentsResponse.data?.comments ?? []);
                setCommentCount(commentsResponse.data?.count ?? 0);
                setNewCommentText('');
            }
        } catch (error) {
            console.error('Ошибка при добавлении комментария:', error);
            setActionMessage('Не удалось отправить комментарий. Попробуйте ещё раз.');
        }
    };

    const setReaction = async (isLike) => {
        if (!JwtTokenService.isAuth()) {
            await redirectToSignIn();
            return;
        }

        if (reactionPending) return;
        setReactionPending(true);
        setActionMessage('');

        try {
            await videoApi.postVideoSetReactionPostId(post.id, { isLike });

            setPostData((previousPost) => {
                const previousReaction = userView.isLike;
                const removingReaction = previousReaction === isLike;

                return {
                    ...previousPost,
                    likeCount: previousPost.likeCount
                        + (isLike && !removingReaction ? 1 : 0)
                        - (previousReaction === true ? 1 : 0),
                    dislikeCount: previousPost.dislikeCount
                        + (!isLike && !removingReaction ? 1 : 0)
                        - (previousReaction === false ? 1 : 0),
                };
            });
            setUserView((previous) => ({
                ...previous,
                isLike: previous.isLike === isLike ? null : isLike,
            }));
        } catch (error) {
            console.error('Ошибка при сохранении реакции:', error);
            setActionMessage('Не удалось сохранить реакцию.');
        } finally {
            setReactionPending(false);
        }
    };

    const saveView = async (player, isComplete = false) => {
        if (!JwtTokenService.isAuth()) return;

        const duration = player.duration();
        const currentTime = player.currentTime();
        if (!Number.isFinite(duration) || duration <= 0 || !Number.isFinite(currentTime)) return;

        await videoApi.postVideoSetView({
            postId: post.id,
            time: currentTime,
            isComplete: isComplete || currentTime >= duration * 0.85,
        });
    };

    const setView = async (player) => {
        if (!JwtTokenService.isAuth()) return;

        const duration = player.duration();
        if (!Number.isFinite(duration) || duration <= 0) return;

        const currentTime = player.currentTime();
        const currentPercent = (currentTime / duration) * 100;
        if (currentPercent < nextThresholdRef.current) return;

        const now = Date.now();
        const minInterval = duration <= 60 ? Math.max(2, duration * 0.1) : 5;
        if ((now - lastCallTimeRef.current) / 1000 < minInterval) return;

        lastCallTimeRef.current = now;
        nextThresholdRef.current = currentPercent >= 90
            ? Infinity
            : Math.max(40, Math.floor(currentPercent / 10) * 10 + 10);

        try {
            await saveView(player);
        } catch (error) {
            console.warn('Не удалось сохранить прогресс просмотра:', error);
        }
    };

    const setViewEnd = async (player) => {
        try {
            await saveView(player, true);
        } catch (error) {
            console.warn('Не удалось завершить просмотр:', error);
        }
    };

    const onPaused = async (player) => {
        if (!JwtTokenService.isAuth()) return;

        const currentTime = player.currentTime();
        if (watchedTimeRef.current === currentTime) return;
        watchedTimeRef.current = currentTime;

        try {
            await saveView(player);
        } catch (error) {
            console.warn('Не удалось сохранить позицию просмотра:', error);
        }
    };

    const handleSubscribe = async () => {
        if (!JwtTokenService.isAuth()) {
            await redirectToSignIn();
            return;
        }

        if (subscriptionPending) return;
        const current = userView.hasSubscription;
        setSubscriptionPending(true);
        setActionMessage('');

        try {
            if (current) {
                await subscriberApi.postApiSubscriberUnsubscribeBlogId(blog.id);
            } else {
                await subscriberApi.postApiSubscriberSubscribeBlogId(blog.id);
            }
            setUserView((previous) => ({ ...previous, hasSubscription: !current }));
            setBlog((previous) => ({
                ...previous,
                subscribersCount: Math.max(0, (previous.subscribersCount ?? 0) + (current ? -1 : 1)),
            }));
        } catch (error) {
            console.error('Ошибка при изменении подписки:', error);
            setActionMessage('Не удалось изменить подписку.');
        } finally {
            setSubscriptionPending(false);
        }
    };

    const handleShare = async () => {
        const shareData = { title: post.title, url: window.location.href };

        try {
            if (navigator.share) {
                await navigator.share(shareData);
            } else {
                await navigator.clipboard.writeText(shareData.url);
                setActionMessage('Ссылка скопирована в буфер обмена.');
            }
        } catch (error) {
            if (error?.name !== 'AbortError') setActionMessage('Не удалось поделиться ссылкой.');
        }
    };

    const handleCreateConference = async () => {
        if (!JwtTokenService.isAuth()) {
            await redirectToSignIn();
            return;
        }

        if (conferencePending) return;
        setConferencePending(true);
        setActionMessage('');

        try {
            const response = await conferenceRoomApi.postApiConferenceRoomCreateConferenceToPost({ postId: post.id });
            if (response.status === 200) navigate(`/conference/${response.data.id}`);
        } catch (error) {
            console.error('Ошибка при создании комнаты:', error);
            setActionMessage('Не удалось создать совместный просмотр.');
        } finally {
            setConferencePending(false);
        }
    };

    if (isLoading) {
        return (
            <VideoPageState loading>Загружаем видео…</VideoPageState>
        );
    }

    if (loadError) {
        return (
            <VideoPageState
                title="Не удалось открыть видео"
                action={(
                    <button type="button" className="primary-button" onClick={() => setReloadKey((value) => value + 1)}>
                        Повторить
                    </button>
                )}
            >
                {loadError}
            </VideoPageState>
        );
    }

    return (
        <VideoWatchLayout>
                <div className="video-container">
                    <article className="main-content">
                        <VideoPlayerFrame>
                            <VideoPlayer
                                key={post.id}
                                className="video-page-player"
                                thumbnail={post.previewUrl}
                                path={{
                                    postId: post.id,
                                    blogId: blog.id,
                                    autoplay: false,
                                    objectName: post.videoData.objectName,
                                }}
                                currentTime={time}
                                onTimeupdate={setView}
                                onEnded={setViewEnd}
                                onPause={onPaused}
                            />
                        </VideoPlayerFrame>

                        <VideoMetadata post={post}>
                                    <VideoActionButton
                                        className={userView.isLike === true ? 'is-active' : ''}
                                        onClick={() => setReaction(true)}
                                        disabled={reactionPending}
                                        aria-pressed={userView.isLike === true}
                                    >
                                        <span aria-hidden="true">👍</span><span>{post.likeCount}</span>
                                    </VideoActionButton>
                                    <VideoActionButton
                                        className={userView.isLike === false ? 'is-active' : ''}
                                        onClick={() => setReaction(false)}
                                        disabled={reactionPending}
                                        aria-pressed={userView.isLike === false}
                                    >
                                        <span aria-hidden="true">👎</span><span>{post.dislikeCount}</span>
                                    </VideoActionButton>
                                    <VideoActionButton onClick={handleShare}>
                                        <span aria-hidden="true">↗</span><span>Поделиться</span>
                                    </VideoActionButton>
                                    <VideoActionButton
                                        className="conference-button"
                                        onClick={handleCreateConference}
                                        disabled={conferencePending}
                                    >
                                        <span aria-hidden="true">◉</span><span>Смотреть вместе</span>
                                    </VideoActionButton>
                        </VideoMetadata>

                        <ChannelSummary
                            blog={blog}
                            onClick={() => navigate(`/channel/${blog.id}`)}
                            action={<button
                                type="button"
                                className={`subscribe-button ${userView.hasSubscription ? 'subscribe-button-active' : ''}`}
                                onClick={handleSubscribe}
                                disabled={subscriptionPending}
                            >
                                {userView.hasSubscription ? 'Вы подписаны' : 'Подписаться'}
                            </button>}
                        />

                        {actionMessage && <p className="video-action-message" role="status">{actionMessage}</p>}

                        <VideoDescription description={post.description} />

                        <section className="comments-section">
                            <h2>{commentCount} комментариев</h2>
                            {JwtTokenService.isAuth() && (
                                <div className="add-comment">
                                    <div className="comment-avatar-placeholder" aria-hidden="true">Вы</div>
                                    <div className="comment-input-container">
                                        <input
                                            type="text"
                                            value={newCommentText}
                                            onChange={(event) => setNewCommentText(event.target.value)}
                                            onKeyDown={(event) => {
                                                if (event.key === 'Enter' && !event.shiftKey) handleAddComment();
                                            }}
                                            placeholder="Добавьте комментарий…"
                                            className="comment-input"
                                            aria-label="Текст комментария"
                                        />
                                        <button
                                            type="button"
                                            onClick={handleAddComment}
                                            className="primary-button"
                                            disabled={!newCommentText.trim()}
                                        >
                                            Отправить
                                        </button>
                                    </div>
                                </div>
                            )}
                            {commentsError
                                ? <div className="comments-unavailable" role="status">Комментарии временно недоступны.</div>
                                : <CommentsList comments={comments} postId={postId} />}
                        </section>
                    </article>

                    <aside className="recommendation-sidebar" aria-label="Рекомендованные видео">
                        <h2>Следующее</h2>
                        <div className="recommendation-list">
                            {recommendations.map((video) => (
                                <SmallVideoCard videoCardModel={video} navigate={navigate} key={video.postId} />
                            ))}
                        </div>
                    </aside>
                </div>
        </VideoWatchLayout>
    );
};

export default VideoPage;
