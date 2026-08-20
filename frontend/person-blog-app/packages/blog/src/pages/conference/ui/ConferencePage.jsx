import { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { HttpTransportType, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import VideoPlayer from '@/features/video-player';
import { BaseApUrl } from '@/shared/api/client';
import { getAccessToken } from '@/shared/auth/tokenStorage';
import { getConferenceRoom } from '@/shared/api/generated/conference-room/conference-room';
import { getConferenceChat } from '@/shared/api/generated/conference-chat/conference-chat';
import { getVideo } from '@/shared/api/generated/video/video';
import {
    ChannelSummary,
    VideoActionButton,
    VideoDescription,
    VideoMetadata,
    VideoPageState,
    VideoPlayerFrame,
    VideoWatchLayout,
} from '@/widgets/video-watch';
import './ConferencePage.css';

const messagePageSize = 20;
const conferenceRoomApi = getConferenceRoom();
const conferenceChatApi = getConferenceChat();
const videoApi = getVideo();

const ConferencePage = function () {
    const { id: conferenceId } = useParams();
    const navigate = useNavigate();
    const [post, setPost] = useState(null);
    const [blog, setBlog] = useState(null);
    const [messages, setMessages] = useState([]);
    const [isLoading, setIsLoading] = useState(true);
    const [loadError, setLoadError] = useState('');
    const [connectionStatus, setConnectionStatus] = useState('Подключение…');
    const [copyMessage, setCopyMessage] = useState('');
    const playerRef = useRef(null);
    const connectionRef = useRef(null);
    const isApplyingRemoteEventRef = useRef(false);
    const remoteEventTimerRef = useRef(null);
    const lastTimeSyncRef = useRef(0);

    useEffect(() => {
        let isActive = true;

        setIsLoading(true);
        setLoadError('');

        const loadConference = async () => {
            try {
                const roomResponse = await conferenceRoomApi.getApiConferenceRoomJoinLink({ roomId: conferenceId });
                const postId = roomResponse.data?.postId;
                if (!postId) throw new Error('Комната не найдена');

                const videoResponse = await videoApi.getVideoVideoPostId(postId);
                if (!videoResponse.data?.post || !videoResponse.data?.blog) {
                    throw new Error('Видео конференции не найдено');
                }

                if (isActive) {
                    setPost(videoResponse.data.post);
                    setBlog(videoResponse.data.blog);
                }
            } catch (error) {
                if (!isActive) return;
                console.error('Ошибка при загрузке конференции:', error);
                setLoadError('Не удалось открыть конференцию. Возможно, она завершена или недоступна.');
            } finally {
                if (isActive) setIsLoading(false);
            }
        };

        loadConference();

        return () => {
            isActive = false;
        };
    }, [conferenceId]);

    useEffect(() => {
        let isActive = true;

        const connection = new HubConnectionBuilder()
            .withUrl(`${BaseApUrl}/conference?conferenceId=${conferenceId}`, {
                accessTokenFactory: () => getAccessToken(),
                skipNegotiation: true,
                transport: HttpTransportType.WebSockets,
            })
            .configureLogging(LogLevel.Warning)
            .withAutomaticReconnect()
            .build();

        const releaseRemoteEvent = () => {
            if (remoteEventTimerRef.current) clearTimeout(remoteEventTimerRef.current);
            remoteEventTimerRef.current = setTimeout(() => {
                isApplyingRemoteEventRef.current = false;
            }, 400);
        };

        const beginRemoteEvent = () => {
            isApplyingRemoteEventRef.current = true;
            releaseRemoteEvent();
        };

        connection.on('OnMessageSend', (message) => {
            setMessages((previous) => [...previous, message]);
        });

        connection.on('OnPause', (time) => {
            const player = playerRef.current;
            if (!player) return;

            beginRemoteEvent();
            if (Number.isFinite(time) && Math.abs(player.currentTime() - time) > 0.5) player.currentTime(time);
            if (!player.paused()) player.pause();
        });

        connection.on('OnPlay', () => {
            const player = playerRef.current;
            if (!player || !player.paused()) return;

            beginRemoteEvent();
            player.play()?.catch(() => setConnectionStatus('Браузер заблокировал автоматическое воспроизведение'));
        });

        connection.on('OnTimeSeek', (time) => {
            const player = playerRef.current;
            if (!player || !Number.isFinite(time)) return;

            beginRemoteEvent();
            player.currentTime(time);
        });

        connection.onreconnecting(() => isActive && setConnectionStatus('Переподключение…'));
        connection.onreconnected(() => isActive && setConnectionStatus('Подключено'));
        connection.onclose(() => isActive && setConnectionStatus('Соединение потеряно'));

        connectionRef.current = connection;
        connection.start()
            .then(() => isActive && setConnectionStatus('Подключено'))
            .catch((error) => {
                console.error('Ошибка подключения к конференции:', error);
                if (isActive) setConnectionStatus('Не удалось подключиться');
            });

        return () => {
            isActive = false;
            if (remoteEventTimerRef.current) clearTimeout(remoteEventTimerRef.current);
            connectionRef.current = null;
            connection.stop().catch(() => undefined);
        };
    }, [conferenceId]);

    const invokeConference = useCallback(async (method, ...args) => {
        const connection = connectionRef.current;
        if (!connection || connection.state !== HubConnectionState.Connected) return;

        try {
            await connection.invoke(method, ...args);
        } catch (error) {
            console.warn(`Не удалось выполнить ${method}:`, error);
        }
    }, []);

    const handleTimeUpdate = useCallback((player) => {
        if (isApplyingRemoteEventRef.current) return;
        const now = Date.now();
        if (now - lastTimeSyncRef.current < 1000) return;
        lastTimeSyncRef.current = now;
        invokeConference('SetCurrentTime', player.currentTime());
    }, [invokeConference]);

    const handleExit = () => {
        const currentTime = playerRef.current?.currentTime?.() ?? 0;
        navigate(`/videoPage/${post.id}?time=${currentTime}`);
    };

    const handleCopyLink = async () => {
        try {
            await navigator.clipboard.writeText(window.location.href);
            setCopyMessage('Ссылка скопирована');
        } catch {
            setCopyMessage('Не удалось скопировать ссылку');
        }
    };

    if (isLoading) return <VideoPageState loading>Подключаемся к конференции…</VideoPageState>;

    if (loadError || !post || !blog) {
        return (
            <VideoPageState
                title="Конференция недоступна"
                action={<button type="button" className="conference-primary-button" onClick={() => navigate('/')}>На главную</button>}
            >
                {loadError}
            </VideoPageState>
        );
    }

    return (
        <VideoWatchLayout className="conference-page-content">
            <div className="conference-layout">
                <article className="conference-main">
                    <VideoPlayerFrame>
                        <VideoPlayer
                            key={post.id}
                            thumbnail={post.previewUrl}
                            path={{
                                blogId: blog.id,
                                postId: post.id,
                                autoplay: false,
                                objectName: post.videoData.objectName,
                            }}
                            onTimeupdate={handleTimeUpdate}
                            setPlayerRef={(player) => { playerRef.current = player; }}
                            onUserSeek={(time) => {
                                if (!isApplyingRemoteEventRef.current) invokeConference('Seek', time);
                            }}
                            onPause={(player) => {
                                if (!isApplyingRemoteEventRef.current) invokeConference('PauseVideo', player.currentTime());
                            }}
                            onPlay={() => {
                                if (!isApplyingRemoteEventRef.current) invokeConference('ResumeVideo');
                            }}
                        />
                    </VideoPlayerFrame>

                    <VideoMetadata post={post}>
                        <VideoActionButton onClick={handleExit}>
                            <span aria-hidden="true">←</span><span>Выйти из просмотра</span>
                        </VideoActionButton>
                        <VideoActionButton onClick={handleCopyLink}>
                            <span aria-hidden="true">⧉</span><span>{copyMessage || 'Скопировать ссылку'}</span>
                        </VideoActionButton>
                    </VideoMetadata>

                    <ChannelSummary blog={blog} onClick={() => navigate(`/channel/${blog.id}`)} />
                    <VideoDescription description={post.description} />
                </article>

                <ConferenceChat
                    messages={messages}
                    setMessages={setMessages}
                    conferenceId={conferenceId}
                    connectionStatus={connectionStatus}
                />
            </div>
        </VideoWatchLayout>
    );
};

const ConferenceChat = function ({ messages, setMessages, conferenceId, connectionStatus }) {
    const [messageInput, setMessageInput] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [isSending, setIsSending] = useState(false);
    const [hasMore, setHasMore] = useState(true);
    const [errorMessage, setErrorMessage] = useState('');
    const messagesEndRef = useRef(null);
    const containerRef = useRef(null);
    const offsetRef = useRef(0);
    const loadingRef = useRef(false);
    const autoScrollRef = useRef(true);
    const previousMessagesLengthRef = useRef(messages.length);

    const loadMessages = useCallback(async () => {
        if (!hasMore || loadingRef.current) return;

        loadingRef.current = true;
        setIsLoading(true);
        setErrorMessage('');

        try {
            const response = await conferenceChatApi.getApiConferenceChatMessagesConferenceId(conferenceId, {
                offset: offsetRef.current,
                count: messagePageSize,
            });
            const loadedMessages = response.data ?? [];

            if (loadedMessages.length > 0) {
                setMessages((previous) => [...loadedMessages, ...previous]);
                offsetRef.current += loadedMessages.length;
            }
            if (loadedMessages.length < messagePageSize) setHasMore(false);
        } catch (error) {
            console.error('Ошибка загрузки сообщений:', error);
            setErrorMessage('Не удалось загрузить сообщения.');
        } finally {
            loadingRef.current = false;
            setIsLoading(false);
        }
    }, [conferenceId, hasMore, setMessages]);

    useEffect(() => {
        loadMessages();
    }, [loadMessages]);

    useEffect(() => {
        if (autoScrollRef.current && messages.length > previousMessagesLengthRef.current) {
            messagesEndRef.current?.scrollIntoView({ behavior: 'smooth', block: 'end' });
        }
        previousMessagesLengthRef.current = messages.length;
    }, [messages]);

    const handleScroll = () => {
        const container = containerRef.current;
        if (!container) return;

        autoScrollRef.current = container.scrollHeight - container.scrollTop - container.clientHeight < 8;
        if (container.scrollTop < 8 && hasMore) loadMessages();
    };

    const handleSendMessage = async () => {
        const message = messageInput.trim();
        if (!message || isSending) return;

        setIsSending(true);
        setErrorMessage('');
        try {
            await conferenceChatApi.postApiConferenceChatSendMessage({ conferenceId, message });
            setMessageInput('');
            autoScrollRef.current = true;
        } catch (error) {
            console.error('Ошибка отправки сообщения:', error);
            setErrorMessage('Не удалось отправить сообщение.');
        } finally {
            setIsSending(false);
        }
    };

    return (
        <aside className="conference-chat" aria-label="Чат конференции">
            <header className="conference-chat-header">
                <div>
                    <h2>Чат конференции</h2>
                    <span className={`conference-status ${connectionStatus === 'Подключено' ? 'is-connected' : ''}`}>
                        {connectionStatus}
                    </span>
                </div>
            </header>

            <div className="conference-messages" ref={containerRef} onScroll={handleScroll} aria-live="polite">
                {isLoading && <div className="conference-loading">Загрузка сообщений…</div>}
                {!isLoading && messages.length === 0 && (
                    <div className="conference-empty-chat">Сообщений пока нет. Начните беседу.</div>
                )}
                {messages.map((message, index) => (
                    <article className="conference-message" key={`${message.createdAt}-${message.creatorUserName}-${index}`}>
                        <div className="conference-message-avatar" aria-hidden="true">
                            {(message.creatorUserName || '?').slice(0, 1).toUpperCase()}
                        </div>
                        <div className="conference-message-content">
                            <div className="conference-message-heading">
                                <strong>{message.creatorUserName || 'Участник'}</strong>
                                <time>{formatMessageDate(message.createdAt)}</time>
                            </div>
                            <p>{message.message}</p>
                        </div>
                    </article>
                ))}
                <div ref={messagesEndRef} />
            </div>

            <footer className="conference-composer">
                {errorMessage && <div className="conference-chat-error" role="alert">{errorMessage}</div>}
                <div className="conference-input-row">
                    <input
                        type="text"
                        className="conference-message-input"
                        value={messageInput}
                        maxLength={4000}
                        onChange={(event) => setMessageInput(event.target.value)}
                        onKeyDown={(event) => {
                            if (event.key === 'Enter' && !event.shiftKey) handleSendMessage();
                        }}
                        placeholder="Введите сообщение…"
                        aria-label="Сообщение конференции"
                    />
                    <button
                        type="button"
                        className="conference-send-button"
                        onClick={handleSendMessage}
                        disabled={!messageInput.trim() || isSending}
                    >
                        <span className="conference-send-label">Отправить</span>
                        <span className="conference-send-icon" aria-hidden="true">↑</span>
                    </button>
                </div>
            </footer>
        </aside>
    );
};

const formatMessageDate = (value) => {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';

    return date.toLocaleString('ru-RU', {
        day: '2-digit',
        month: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
    });
};

export default ConferencePage;
