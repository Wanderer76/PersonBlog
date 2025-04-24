import { useState } from "react";
import { JwtTokenService } from '../../scripts/TokenStrorage';
import API from "../../scripts/apiMethod";
import { useNavigate } from "react-router-dom";
import BigVideoCard from "../../components/VideoCards/BigVideoCard/BigVideoCard";
import SideBar from "../../components/sidebar/SideBar";
import './HistoryPage.css';
import { getLocalDate, getLocalDateTime, secondsToHumanReadable } from "../../scripts/LocalDate";

const HistoryPage = function (props) {

    const [historyList, setHistoryList] = useState([]);
    const navigate = useNavigate();
    useState(() => {

        API.get("video/api/View/history").then(response => {
            console.log(response)
            if (response.status == 200) {
                setHistoryList(response.data)
                console.log(response.data)
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
      <div className="history-item" onClick={()=>{
        navigate(`/video/${item.postId}?time=${item.watchTime}`)
      }}>
        <img src={item.previewUrl} alt="Превью" className="thumbnail" />
        <div className="details">
          <h3 className="title">{item.title}</h3>
          <div className="meta">
            <div className="author">{item.blogName}</div>
            <div className="stats">
              <span>{item.views}</span>
              <span>•</span>
              <span>Просмотрено: {secondsToHumanReadable(item.watchTime)}</span>
              <span>{item.uploaded}</span>
            </div>
            <div className="watch-time">{getLocalDate(item.lastWatched)}</div>
          </div>
        </div>
        <div className="menu-container">
          <button 
            className="menu-btn" 
            onClick={() => setShowMenu(!showMenu)}
          >
            ⋮
          </button>
          
        </div>
      </div>
    );
  };

    return (<>
        <div className="page-container">
            <SideBar />
            <div className="content-container">
                <div className="history-container">
                    {Object.entries(historyList).map(([day, items]) => (
                        items.length > 0 && (
                            <div key={day} className="day-group">
                                <h2 className="day-header">{getLocalDate(day)}</h2>
                                <div className="history-list">
                                    {items.map(x => {
                                        var data = {
                                            postId: x.postDetail.id,
                                            previewUrl: x.postDetail.previewUrl,
                                            title: x.postDetail.title,
                                            description: x.postDetail.description,
                                            viewCount: x.postDetail.viewCount,
                                            watchTime: x.watchedTime,
                                            lastWatched:x.lastWatched
                                        }
                                        return <HistoryItem item={data} navigate={navigate} key={x.id} />
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