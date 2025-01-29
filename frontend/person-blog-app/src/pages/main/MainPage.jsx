import React from "react";
import './MainPage.css';

export class MainPage extends React.Component {

    render() {
        return (
            <>
                <div className="video-container">
                    <div className="video-grid">

                        <div className="video-card">
                            <img src="https://shkafnsk.ru/image/catalog/materialy/fotopechat/4/4-139.jpg" className="thumbnail" alt="Превью видео" onClick={(e)=>{
                                console.log("asd")
                            }} />
                            <div className="video-info">

                                <h3 className="video-title">Еще одно интересное видео</h3>
                                <div className="channel-info">
                                    <img src="https://zooclub.ru/attach/4672.jpg" className="channel-icon" alt="Логотип канала" />
                                    <span className="channel-name">Другой канал</span>
                                </div>
                                <div className="video-stats">
                                    45 тыс. просмотров • 1 неделя назад
                                </div>
                            </div>
                        </div>
                        <div className="video-card">
                            <img src="https://shkafnsk.ru/image/catalog/materialy/fotopechat/4/4-139.jpg" className="thumbnail" alt="Превью видео" onClick={(e)=>{
                                console.log("asd")
                            }} />
                            <div className="video-info">

                                <h3 className="video-title">Еще одно интересное видео</h3>
                                <div className="channel-info">
                                    <img src="https://zooclub.ru/attach/4672.jpg" className="channel-icon" alt="Логотип канала" />
                                    <span className="channel-name">Другой канал</span>
                                </div>
                                <div className="video-stats">
                                    45 тыс. просмотров • 1 неделя назад
                                </div>
                            </div>
                        </div>
                        <div className="video-card">
                            <img src="https://shkafnsk.ru/image/catalog/materialy/fotopechat/4/4-139.jpg" className="thumbnail" alt="Превью видео" onClick={(e)=>{
                                console.log("asd")
                            }} />
                            <div className="video-info">

                                <h3 className="video-title">Еще одно интересное видео</h3>
                                <div className="channel-info">
                                    <img src="https://zooclub.ru/attach/4672.jpg" className="channel-icon" alt="Логотип канала" />
                                    <span className="channel-name">Другой канал</span>
                                </div>
                                <div className="video-stats">
                                    45 тыс. просмотров • 1 неделя назад
                                </div>
                            </div>
                        </div>
                        <div className="video-card">
                            <img src="https://shkafnsk.ru/image/catalog/materialy/fotopechat/4/4-139.jpg" className="thumbnail" alt="Превью видео" onClick={(e)=>{
                                console.log("asd")
                            }} />
                            <div className="video-info">

                                <h3 className="video-title">Еще одно интересное видео</h3>
                                <div className="channel-info">
                                    <img src="https://zooclub.ru/attach/4672.jpg" className="channel-icon" alt="Логотип канала" />
                                    <span className="channel-name">Другой канал</span>
                                </div>
                                <div className="video-stats">
                                    45 тыс. просмотров • 1 неделя назад
                                </div>
                            </div>
                        </div>



                    </div>
                </div>
            </>)
    }
}
