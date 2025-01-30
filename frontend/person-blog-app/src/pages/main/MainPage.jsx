import React, { useState } from "react";
import './MainPage.css';
import API from "../../scripts/apiMethod";

const playUrl = "https://cdn3.iconfinder.com/data/icons/audio-sound-and-video/64/audio-video-outline-play-1024.png";

const MainPage = function () {

    const [videos, setVideos] = useState([])

    useState(() => {

        API.get("/video/recommendations?page=1&limit=20")
            .then(response => {
                if (response.status === 200) {
                    setVideos(response.data);
                }
            });

    }, [])

    return (
        <>
            <div className="video-container">
                <div className="video-grid">
                    {videos.map(x => createVideoCard(x))}
                </div>
            </div>
        </>)

}

function createVideoCard(videoCardModel) {
    return <div className="video-card">
        <div class="thumbnail-container" onClick={(e) => {
            console.log("asd");
        }}>

            <img src={videoCardModel.previewUrl} className="thumbnail" alt="Превью видео" />
            <div class="play-icon"></div>
        </div>
        <div className="video-info">

            <h3 className="video-title">{videoCardModel.title}</h3>
            <div className="channel-info">
                <img src={videoCardModel.blogLogo === null ? 'https://rostov.ucstroitel.ru/upload/iblock/30b/30b1723419475e59e357fe8842575c10.png' : videoCardModel.blogLogo} className="channel-icon" alt="Логотип канала" />
                <span className="channel-name">{videoCardModel.blogName}</span>
            </div>
            <div className="video-stats">
                Просмотров: {videoCardModel.viewCount}
            </div>
        </div>
    </div>;
}

export default MainPage;