import React from 'react';
import styles from './SmallVideoCard.module.css';

const SmallVideoCard = function (props) {
    return <button type="button" className={styles.video} onClick={(e) => {
        e.preventDefault();
        props.navigate(`/videoPage/${props.videoCardModel.postId}`);
    }}>
        <img src={props.videoCardModel.previewUrl} className={styles.thumbnail} alt="Превью" />
        <div className={styles.info}>
            <div className={styles.title}>{props.videoCardModel.title}</div>
            {props.videoCardModel.blogName && (
                <div className={styles.channel}>{props.videoCardModel.blogName}</div>
            )}
            {Number.isFinite(props.videoCardModel.viewCount) && (
                <div className={styles.stats}>{props.videoCardModel.viewCount} просмотров</div>
            )}
        </div>
    </button>
}

export default SmallVideoCard;
