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
            <div className="video-container" >
                <div className="video-grid">
                    {videos.map(x => {
                        return <VideoCard videoCardModel={x} key={x.postId} />
                    })}
                </div>
            </div>
        </>)

}

const VideoCard = function ({ videoCardModel }) {
    const navigate = useNavigate();
    return <div key={videoCardModel.postId} className="video-card">
        <div className="thumbnail-container" onClick={(e) => {
            navigate(`/video/${videoCardModel.postId}/${videoCardModel.videoId}`);
        }}>

            <img src={videoCardModel.previewUrl} className="thumbnail" alt="Превью видео" />
            <div className="play-icon"></div>
        </div>
        <div className="video-info">
            <h3 className="video-title">{videoCardModel.title}</h3>
            <div className="channel-info">
                <img src={videoCardModel.blogLogo === null ? logo : videoCardModel.blogLogo} className="channel-icon" alt="Логотип канала" />
                <span className="channel-name">{videoCardModel.blogName}</span>
            </div>
            <div className="video-stats">
                Просмотров: {videoCardModel.viewCount}
            </div>
        </div>
    </div>
        ;
    ;
}

// export default MainPage;