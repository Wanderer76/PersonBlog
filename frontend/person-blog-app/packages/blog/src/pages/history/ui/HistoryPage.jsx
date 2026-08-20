import { useEffect, useState } from "react";
import { JwtTokenService } from '@/shared/auth/tokenStorage';
import { getView } from "@/shared/api/generated/view/view";
import { useNavigate } from "react-router-dom";
import { PageShell } from '@/widgets/page-shell';
import styles from './HistoryPage.module.css';
import { getLocalDate, getLocalDateTime, secondsToHumanReadable } from "@/shared/lib/date";
import { PostListItem } from '@/entities/post';

const viewApi = getView();

const HistoryPage = function (props) {

  const [historyList, setHistoryList] = useState([]);
  const navigate = useNavigate();
  useEffect(() => {
    viewApi.getApiViewHistory().then(response => {
      if (response.status == 200) {
        setHistoryList(response.data ?? {})
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

  return (
    <PageShell className={styles.pageContainer} contentClassName={styles.contentContainer}>
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
                    return <PostListItem item={data} navigate={() => navigate(`/videoPage/${data.postId}?time=${data.watchTime}`)} key={x.id} />
                  })}
                </div>
              </div>
            )
          ))}
        </div>
    </PageShell>
  );


}

export default HistoryPage;
