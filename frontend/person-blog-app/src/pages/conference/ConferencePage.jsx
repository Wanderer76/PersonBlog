import { useEffect, useState } from "react";
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

    useEffect(() => {
        API.get(`http://localhost:5193/ConferenceRoom/joinLink?roomId=${conferenceId.id}`)
            .then(async response => {
                const postId = response.data.postId
                await API.get(`/video/Video/video/${postId}`)
                    .then(response => {
                        setPost(response.data.post);
                        setBlog(response.data.blog);
                    })
            })
        const connection_chat = new HubConnectionBuilder()
            .withUrl("http://localhost:5193/chat", {
                skipNegotiation: true,
                transport: HttpTransportType.WebSockets
            })

            .configureLogging(LogLevel.Debug)
            .withAutomaticReconnect()
            .build();
        connection_chat.on("onconferenceconnect", function (message) {
            console.log(message);
            console.log("message");
            setMessages([message]);
        });
        connection_chat.start().then(() => {
            setConnection(connection_chat);
            console.log("SignalR Connected.")

        });
    }, []);

    if (post == null || blog == null)
        return <></>;

    return (
        <div className="video-container">
            <div className="main-content">

                {videoWindow()}
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
                {messages.map(x => <><p>{x}</p><br /></>)}
            </aside>
        </div>
    );

    function getUrl(postId, objectName) {
        if (postId !== null && objectName !== null)
            return `${BaseApUrl}/video/Video/video/v2/${postId}/chunks/${objectName}`;
    }

    function videoWindow() {
        return <div className="video-player">
            <VideoPlayer className="myVideo"
                thumbnail={post.previewUrl}
                path={{
                    url: getUrl(post.id, post.videoData.objectName),
                    label: '',
                    postId: post.id,
                    autoplay: false,
                    objectName: post.videoData.objectName
                }}
                onTimeupdate={() => { }} />
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