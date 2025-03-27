import { useEffect, useRef, useState } from "react";
import { useParams } from "react-router-dom";
import { getLocalDateTime } from "../../scripts/LocalDate";
import VideoPlayer from "../../components/VideoPlayer/VideoPlayer";
import logo from '../../defaultProfilePic.png';
import API, { BaseApUrl } from "../../scripts/apiMethod";
import { HttpTransportType, HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";

export const ConferencePage = function () {
    const conferenceId = useParams();
    const [post, setPost] = useState();
    const [blog, setBlog] = useState();
    const [messages, setMessages] = useState([]);
    const [connection, setConnection] = useState(null);
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
            console.log("SignalR Connected.");
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

            <aside className="sidebar">
                <p>Чат</p>
                {messages.map((x, i) => <div key={i}><p >{x}</p><br /></div>)}
            </aside>
        </div>
    );


    function handleSignalRSeek(time) {
        if (playerRef.current) {
            isSeeking.current = true;
            playerRef.current.currentTime(time)
            setTimeout(() => isSeeking.current = false, 500)
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
            // seekFunction(time); // Вызываем seekFunction, передавая ей время от SignalR
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


export default ConferencePage;