import React from 'react';
import styles from './SmallVideoCard.module.css';

const SmallVideoCard = function (props) {
    return <div className={styles.video} onClick={(e) => {
        e.preventDefault();
        props.navigate(`/video/${props.videoCardModel.postId}`);
    }}>
        <img src={props.videoCardModel.previewUrl} className={styles.thumbnail} alt="Превью" />
        <div className={styles.info}>
            <div className={styles.title}>{props.videoCardModel.title}</div>
            <div className={styles.channel}>{props.videoCardModel.blogName}</div>
            <div className={styles.stats}> {props.videoCardModel.viewCount} просмотров • 2 дня назад</div>
        </div>
    </div>
}

export default SmallVideoCard;