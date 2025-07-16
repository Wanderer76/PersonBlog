import { useState } from "react";
import { getLocalDate, secondsToHumanReadable } from "../../../scripts/LocalDate";
import styles from './PostListItem.module.css';

// item  = {
//   postId
//   previewUrl
//   title
//   description
//   viewCount
//   watchTime
//   lastWatched
// };

const PostListItem = ({ item, navigate }) => {
  const [showMenu, setShowMenu] = useState(false);

  return (
    <div className={styles.historyItem} onClick={() => {
      navigate()
    }}>
      <img src={item.previewUrl} alt="Превью" className={styles.thumbnail} />
      <div className={styles.details}>
        <h3 className={styles.title}>{item.title}</h3>
        <div className={styles.meta}>
          <div className={styles.author}>{item.blogName}</div>
          <div className={styles.stats}>
            <span>{item.views}</span>
            {item.watchTime && <span>•</span>}
            {item.watchTime &&
              <span>Просмотрено: {secondsToHumanReadable(item.watchTime)}</span>}
            <span>{item.uploaded}</span>
          </div>
          {item.lastWatched &&
            <div className={styles.watchTime}>{getLocalDate(item.lastWatched)}</div>}
        </div>
      </div>
      <div className={styles.menuContainer}>
        <button
          className={styles.menuBtn}
          onClick={() => setShowMenu(!showMenu)}
        >
          ⋮
        </button>

      </div>
    </div>
  );
};

export default PostListItem;