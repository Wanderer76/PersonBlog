import React, { useEffect, useState } from "react";
import './MainPage.css';
import API from "../../scripts/apiMethod";
import logo from '../../defaultProfilePic.png';
import { useNavigate } from "react-router-dom";

export const MainPage = function () {
    const [videos, setVideos] = useState([]);
    const [page, setPage] = useState(1);
    const limit = 40;

    useEffect(() => {

        API.get(`/video/recommendations?page=${page}&limit=${limit}`)
            .then(response => {
                if (response.status === 200) {
                    setVideos(response.data);
                }
            });

    }, [])

    return (
        <>
            <div className="mainpage-container" >
                <div className="mainpage-video-grid">
                    {videos.map(x => {
                        return <VideoCard videoCardModel={x} key={x.postId} />
                    })}
                </div>
            </div>
        </>)

}

const VideoCard = function ({ videoCardModel }) {
    const navigate = useNavigate();
    return <div key={videoCardModel.postId} className="mainpage-video-card">
        <div className="mainpage-thumbnail-container" onClick={(e) => {
            navigate(`/video/${videoCardModel.postId}/${videoCardModel.videoId}`);
        }}>

            <img src={videoCardModel.previewUrl} className="mainpage-thumbnail" alt="Превью видео" />
            <div className="mainpage-play-icon"></div>
        </div>
        <div className="mainpage-video-info">
            <h3 className="mainpage-video-title">{videoCardModel.title}</h3>
            <div className="mainpage-channel-info">
                <img src={videoCardModel.blogLogo === null ? logo : videoCardModel.blogLogo} className="mainpage-channel-icon" alt="Логотип канала" />
                <span className="mainpage-channel-name">{videoCardModel.blogName}</span>
            </div>
            <div className="mainpage-video-stats">
                Просмотров: {videoCardModel.viewCount}
            </div>
        </div>
    </div>;
}

// export default MainPage;