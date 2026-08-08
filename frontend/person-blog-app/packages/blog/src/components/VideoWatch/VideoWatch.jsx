import SideBar from '../sidebar/SideBar';
import { getLocalDateTime } from '../../shared/LocalDate';
import logo from '../../defaultProfilePic.png';
import './VideoWatch.css';

export const VideoWatchLayout = ({ children, className = '' }) => (
    <div className="watch-page-layout">
        <SideBar />
        <main className={`watch-page-content ${className}`.trim()}>{children}</main>
    </div>
);

export const VideoPlayerFrame = ({ children }) => (
    <div className="watch-player-frame">{children}</div>
);

export const VideoMetadata = ({ post, children }) => (
    <section className="watch-video-metadata">
        <h1 className="watch-video-title">{post.title}</h1>
        <div className="watch-video-stats">
            <div className="watch-views-date">
                <span>{post.viewCount} просмотров</span>
                <span aria-hidden="true">•</span>
                <span>Опубликовано {getLocalDateTime(post.createdAt)}</span>
            </div>
            {children && <div className="watch-video-actions" aria-label="Действия с видео">{children}</div>}
        </div>
    </section>
);

export const VideoActionButton = ({ className = '', children, ...props }) => (
    <button type="button" className={`watch-action-button ${className}`.trim()} {...props}>
        {children}
    </button>
);

export const ChannelSummary = ({ blog, onClick, action }) => {
    const content = (
        <>
            <img src={blog.photoUrl || logo} className="watch-channel-avatar" alt="" />
            <span className="watch-channel-copy">
                <span className="watch-channel-name">{blog.name}</span>
                <span className="watch-subscribers-count">{blog.subscribersCount ?? 0} подписчиков</span>
            </span>
        </>
    );

    return (
        <section className="watch-channel-info" aria-label="Информация о канале">
            {onClick
                ? <button type="button" className="watch-channel-main" onClick={onClick}>{content}</button>
                : <div className="watch-channel-main">{content}</div>}
            {action}
        </section>
    );
};

export const VideoDescription = ({ description }) => {
    if (!description?.trim()) return null;

    return (
        <section className="watch-video-description">
            <h2>Описание</h2>
            <p>{description}</p>
        </section>
    );
};

export const VideoPageState = ({ title, children, action, loading = false }) => (
    <VideoWatchLayout>
        <div className="watch-page-state" role={title ? 'alert' : undefined} aria-live="polite">
            {loading && <div className="watch-page-spinner" />}
            {title && <h1>{title}</h1>}
            <p>{children}</p>
            {action}
        </div>
    </VideoWatchLayout>
);
