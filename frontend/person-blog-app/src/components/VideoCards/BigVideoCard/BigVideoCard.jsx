import { useNavigate } from "react-router-dom";
import logo from '../../../defaultProfilePic.png';
import React from "react";
import styles from './BigVideoCard.module.css';


const BigVideoCard = React.forwardRef(function ({ videoCardModel }, ref) {
    const navigate = useNavigate();
    return (
        <div ref={ref} className={styles.videoCard}>
            <div
                className={styles.thumbnailContainer}
                onClick={() => navigate(`/video/${videoCardModel.postId}`)}
            >
                <img
                    src={videoCardModel.previewUrl}
                    className={styles.thumbnail}
                    alt="Превью видео"
                />
                <div className={styles.playIcon}></div>
            </div>
            <div className={styles.videoInfo}>
                <h3 className={styles.videoTitle}>{videoCardModel.title}</h3>
                <div className={styles.channelInfo}>
                    <img
                        src={videoCardModel.blogLogo || logo}
                        className={styles.channelIcon}
                        alt="Логотип канала"
                    />
                    <span className={styles.channelName}>{videoCardModel.blogName}</span>
                </div>
                <div className={styles.videoStats}>
                    Просмотров: {videoCardModel.viewCount}
                </div>
            </div>
        </div>
    );
});

export default BigVideoCard;