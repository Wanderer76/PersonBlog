import { useState } from "react";
import { JwtTokenService } from '../../scripts/TokenStrorage';
import API from "../../scripts/apiMethod";
import { useNavigate } from "react-router-dom";
import BigVideoCard from "../../components/VideoCards/BigVideoCard/BigVideoCard";
import SideBar from "../../components/sidebar/SideBar";
import styles from './HistoryPage.module.css';
import { getLocalDate, getLocalDateTime, secondsToHumanReadable } from "../../scripts/LocalDate";
import PostListItem from "../../components/VideoCards/PostListItem/PostListItem";

const HistoryPage = function (props) {

  const [historyList, setHistoryList] = useState([]);
  const navigate = useNavigate();
  useState(() => {

    API.get("video/api/View/history").then(response => {
      if (response.status == 200) {
        setHistoryList(response.data)
      }
    })

  }, [])

  if (!JwtTokenService.isAuth())
    return (<>
      <div>Вы не авторизованы</div>
    </>);

  const HistoryItem = ({ item, navigate }) => {
    const [showMenu, setShowMenu] = useState(false);

    return (
      <div className={styles.historyItem} onClick={() => {
        navigate(`/video/${item.postId}?time=${item.watchTime}`)
      }}>
        <img src={item.previewUrl} alt="Превью" className={styles.thumbnail} />
        <div className={styles.details}>
          <h3 className={styles.title}>{item.title}</h3>
          <div className={styles.meta}>
            <div className={styles.author}>{item.blogName}</div>
            <div className={styles.stats}>
              <span>{item.views}</span>
              <span>•</span>
              <span>Просмотрено: {secondsToHumanReadable(item.watchTime)}</span>
              <span>{item.uploaded}</span>
            </div>
            <div className={styles.watchTime}>{getLocalDate(item.lastWatched)}</div>
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

  return (<>
    <div className={styles.pageContainer}>
      <SideBar />
      <div className={styles.contentContainer}>
        <div className={styles.historyContainer}>
          {Object.entries(historyList).map(([day, items]) => (
            items.length > 0 && (
              <div key={day} className={styles.dayGroup}>
                <h2 className={styles.dayHeader}>{getLocalDate(day)}</h2>
                <div className={styles.historyList}>
                  {items.map(x => {
                    var data = {
                      postId: x.postDetail.id,
                      previewUrl: x.postDetail.previewUrl,
                      title: x.postDetail.title,
                      description: x.postDetail.description,
                      viewCount: x.postDetail.viewCount,
                      watchTime: x.watchedTime,
                      lastWatched: x.lastWatched
                    }
                    return <PostListItem item={data} navigate={() => navigate(`/video/${data.postId}?time=${data.watchTime}`)} key={x.id} />
                  })}
                </div>
              </div>
            )
          ))}
        </div>
      </div>
    </div>
  </>);


}

export default HistoryPage;