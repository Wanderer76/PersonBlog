import { Link } from 'react-router-dom';
import React from 'react';
import logo from '@/shared/assets/defaultProfilePic.png';
import styles from '@/entities/post/ui/BigVideoCard/BigVideoCard.module.css';

const BigVideoCard = React.forwardRef(function BigVideoCard({ videoCardModel }, ref) {
    const videoUrl = `/videoPage/${videoCardModel.postId}`;
    const creator = videoCardModel.creator;

    return (
        <article ref={ref} className={styles.videoCard}>
            <Link className={styles.thumbnailContainer} to={videoUrl}>
                <img
                    src={videoCardModel.previewUrl}
                    className={styles.thumbnail}
                    alt={`Превью видео «${videoCardModel.title}»`}
                />
                <span className={styles.playIcon} aria-hidden="true" />
            </Link>

            <div className={styles.videoInfo}>
                <header className={styles.videoHeader}>
                    {creator?.blogId ? (
                        <Link className={styles.channelInfo} to={`/channel/${creator.blogId}`}>
                            <img
                                src={creator.avatarUrl || logo}
                                className={styles.channelIcon}
                                alt=""
                            />
                            <span className={styles.channelName}>{creator.name || 'Неизвестный автор'}</span>
                        </Link>
                    ) : (
                        <div className={styles.channelInfo}>
                            <img src={logo} className={styles.channelIcon} alt="" />
                            <span className={styles.channelName}>Неизвестный автор</span>
                        </div>
                    )}
                    <span className={styles.videoKind}>Видео</span>
                </header>
                <h2 className={styles.videoTitle}>
                    <Link to={videoUrl}>{videoCardModel.title}</Link>
                </h2>
                {Number.isFinite(videoCardModel.viewCount) && (
                    <div className={styles.videoStats}>
                        Просмотров: {videoCardModel.viewCount}
                    </div>
                )}
            </div>
        </article>
    );
});

export default BigVideoCard;
