import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { getLocalDateTime } from "../../scripts/LocalDate";
import VideoPlayer from "../../components/VideoPlayer/VideoPlayer";
import logo from '../../defaultProfilePic.png';
import API, { BaseApUrl } from "../../scripts/apiMethod";
import { HttpTransportType, HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import './ConferencePage.css';
import '../post/VideoPage.css';

const ConferencePage = function () {
    const conferenceId = useParams();
    const [post, setPost] = useState();
    const [blog, setBlog] = useState();
    const [messages, setMessages] = useState([]);
    const [connection, setConnection] = useState(null);
    const navigate = useNavigate();
    const playerRef = useRef(null);
    const isSeeking = useRef(false); // Флаг блокировки обновлений

    useEffect(() => {
        API.get(`http://localhost:5193/ConferenceRoom/joinLink?roomId=${conferenceId.id}`)
            .then(async response => {
                const postId = response.data.postId;

                await API.get(`/video/Video/video/${postId}`)
                    .then(response => {
                        setPost(response.data.post);
                        setBlog(response.data.blog);
                    });

            })
    }, []);

    useEffect(() => {
        const connection_chat = new HubConnectionBuilder()
            .withUrl("http://localhost:5193/conference?conferenceId=" + conferenceId.id, {
                headers: { 'conferenceId': `${conferenceId.id}` },
                skipNegotiation: true,
                transport: HttpTransportType.WebSockets,
            })
            .configureLogging(LogLevel.Debug)
            .withAutomaticReconnect()
            .build();

        connection_chat.on("onconferenceconnect", function (message) {
            setMessages([message]);
        });

        connection_chat.on("OnMessageSend", function (message) {
            console.log('message comes')
            setMessages((prev) => [...prev, message]);
        });

        connection_chat.on("OnPause", function (time) {
            handleSignalRPause(time);
        });

        connection_chat.on("OnPlay", function () {
            handleSignalRPlay();
        })

        connection_chat.on("OnTimeSeek", function (time) {
            if (!isSeeking.current)
                handleSignalRSeek(time);
        });

        setConnection(connection_chat);     
    }, []);

    useEffect(() => {
        const startConnection = async () => {
            await connection.start();
        }
        if (connection)
            startConnection()
                .then(() => console.log("SignalR connected."))
                .catch(() => console.assert(connection.state === HubConnectionState.Connected));

    }, [connection]);

    if (post == null || blog == null)
        return <></>;

    return (
        <div className="video-container">
            <div className="main-content">

                {videoWindow(connection)}
                {videoMetadata(post)}
                {channelInfo(blog)}

                <div className="video-description">
                    <span>Описание: </span>
                    {post.description}
                </div>

                <div className="comments-section">
                    <h3>432 комментария</h3>

                    <div className="comment">
                        <img src="https://picsum.photos/40/40" className="comment-avatar" alt="Аватар пользователя" />
                        <div className="comment-content">
                            <div className="comment-author">Иван Петров</div>
                            <div className="comment-text">Отличное видео! Очень познавательно и интересно подано.</div>
                        </div>
                    </div>

                    <div className="comment">
                        <img src="https://picsum.photos/40/40" className="comment-avatar" alt="Аватар пользователя" />
                        <div className="comment-content">
                            <div className="comment-author">Иван Петров</div>
                            <div className="comment-text">Отличное видео! Очень познавательно и интересно подано.</div>
                        </div>
                    </div>

                </div>
            </div>
            <div className="chat-container">
                <Messages messages={messages} setMessages={setMessages} conferenceId={conferenceId.id} />
               
            </div>
        </div>
    );


    function handleSignalRSeek(time) {
        if (playerRef.current) {
            isSeeking.current = true;
            setTimeout(() => isSeeking.current = false, 500)
            playerRef.current.currentTime(time)
            // seekFunction(time); // Вызываем seekFunction, передавая ей время от SignalR
        } else {
            console.warn("Seek function not available.");
        }
    }

    function handleSignalRPause(time) {
        if (playerRef.current) {
            var isPlaying = playerRef.current.currentTime > 0 && !playerRef.current.paused && !playerRef.current.ended
                && playerRef.current.readyState > playerRef.current.HAVE_CURRENT_DATA;

            if (isPlaying !== true) {
                playerRef.current.currentTime(time);
                playerRef.current.pause();
            }
        }
    }

    function handleSignalRPlay() {
        if (playerRef.current && playerRef.current.paused) {
            var isPlaying = playerRef.current.currentTime > 0 && !playerRef.current.paused && !playerRef.current.ended
                && playerRef.current.readyState > playerRef.current.HAVE_CURRENT_DATA;

            if (!isPlaying)
                playerRef.current.play();
        }
    }

    function getUrl(postId, objectName) {
        if (postId !== null && objectName !== null)
            return `${BaseApUrl}/video/Video/video/v2/${postId}/chunks/${objectName}`;
    }

    function videoWindow(connection) {
        return <div className="video-player">
            <VideoPlayer className="myVideo"
                thumbnail={post.previewUrl}
                path={{
                    url: getUrl(post.id, post.videoData.objectName),
                    label: '',
                    postId: post.id,
                    autoplay: false,
                    preload: 'none',
                    objectName: post.videoData.objectName
                }}

                onTimeupdate={(player) => {
                    connection.invoke("SetCurrentTime", player.currentTime());
                }}

                setPlayerRef={(e) => {
                    playerRef.current = e
                }}
                onUserSeek={(time) => {
                    connection.invoke("Seek", time);
                }}
                onPause={(time) => {
                    connection.invoke("PauseVideo", time);
                }}
                onPlay={() => {
                    connection.invoke("ResumeVideo");
                }}
            />
        </div>;
    }

    function videoMetadata(post) {
        return <div className="video-metadata">
            <h1 className="video-title">{post.title}</h1>

            <div className="video-stats">
                <div className="views-date">
                    <span>{post.viewCount} Просмотров </span> •
                    <span> Опубликовано {getLocalDateTime(post.createdAt)}</span>
                </div>
                <div className="video-actions">
                    <button className="action-button" onClick={(e) => {navigate(`/video/${post.id}?time=${playerRef.current.currentTime()}`)}}>
                        <span>📁</span> Отключится от конференции
                    </button>
                    <button className="action-button" onClick={(e) => { navigator.clipboard.writeText(window.location.href) }}>
                        <span>📁</span> Ссылка для присоединения
                    </button>
                </div>
            </div>
        </div>;
    }

    function channelInfo(blog) {
        return <div className="channel-info">
            <div className="channel-left">
                <img src={blog.photoUrl === null ? logo : blog.photoUrl} className="channel-avatar" alt="Аватар канала" />
                <div>
                    <div className="channel-name">{blog.name}</div>
                    <div className="subscribers-count">{blog.subscribersCount} подписчиков</div>
                </div>
            </div>
        </div>;
    }
}

const Messages = function ({ messages, setMessages, conferenceId }) {
    const [page, setPage] = useState(1);
    const [isLoading, setIsLoading] = useState(false);
    const [hasMore, setHasMore] = useState(true);
    const [messageInput, setMessageInput] = useState('');
    const messagesEndRef = useRef(null);
    const containerRef = useRef(null);
    const [isAutoScroll, setIsAutoScroll] = useState(true);
    const prevMessagesLength = useRef(messages.length);

    const handleSendMessage = async () => {
        if (messageInput.trim()) {
            try {

                await API.post("http://localhost:5193/api/ConferenceChat/sendMessage", {
                    conferenceId: conferenceId,
                    message: messageInput
                });

                setMessageInput('');
            } catch (error) {
                console.error('Ошибка отправки сообщения:', error);
            }
        }
    };
    const loadMessages = async (pageNumber) => {
        if (!hasMore || isLoading) return;

        setIsLoading(true);
        try {
            const response = await API.get(`http://localhost:5193/api/ConferenceChat/messages/${conferenceId}`, {
                params: {
                    offset: pageNumber,
                    count: 10
                }
            });

            if (response.data.length === 0) {
                setHasMore(false);
            } else {
                setMessages(prev => [...response.data, ...prev]);
            }
        } catch (error) {
            console.error('Ошибка загрузки сообщений:', error);
        } finally {
            setIsLoading(false);
        }
    };

    const handleScroll = () => {
        const container = containerRef.current;
        // Определяем находится ли пользователь внизу контейнера
        const isAtBottom = container.scrollHeight - container.scrollTop === container.clientHeight;
        setIsAutoScroll(isAtBottom);
        if (containerRef.current.scrollTop === 0 && hasMore) {
           

            setPage(prev => {
                const newPage = prev + 1;
                loadMessages(newPage);
                return newPage;
            });
        }
    };

    useEffect(() => {
        loadMessages(1);
    }, []);

    useEffect(() => {
        if (isAutoScroll && messages.length > prevMessagesLength.current) {
            messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
        }
        prevMessagesLength.current = messages.length;
    }, [messages]);

    return (
        <aside className="conference-sidebar">
            <h3 className="comments-section">Чат конференции</h3>
            <div
                className="messages-container"
                ref={containerRef}
                onScroll={handleScroll}
            >
                {isLoading && <div className="loading-text">Загрузка сообщений...</div>}
                {messages.map((x, i) => (
                    <div className="comment" key={i}>
                        <img
                            src={x.creatorAvatar || "https://picsum.photos/40/40"}
                            className="comment-avatar"
                            alt="Аватар"
                        />
                        <div className="comment-content">
                            <div className="comment-author">{x.creatorUserName}</div>
                            <div className="comment-text">{x.message}</div>
                            <div className="views-date">
                                {new Date(x.createdAt).toLocaleDateString('ru-RU', {
                                    hour: '2-digit',
                                    minute: '2-digit'
                                })}
                            </div>
                        </div>
                    </div>
                ))}
                <div ref={messagesEndRef} />
            </div>

            <div className="message-input-container">
                <input
                    type="text"
                    className="message-input"
                    value={messageInput}
                    onChange={(e) => setMessageInput(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && handleSendMessage()}
                    placeholder="Введите сообщение..."
                />
                <button
                    className="subscribe-button"
                    onClick={handleSendMessage}
                    style={{ padding: '8px 16px' }}
                >
                    Отправить
                </button>
            </div>
        </aside>
    );
};

export default ConferencePage;