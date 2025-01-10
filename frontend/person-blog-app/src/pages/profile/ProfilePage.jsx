import React, { useEffect, useRef, useState } from "react";
import { JwtTokenService } from "../../scripts/TokenStrorage";
import './ProfilePage.css'

export class ProfilePage extends React.Component {

    constructor(props) {
        super(props);
        this.state = {
            blogId: null
        }
    }

    updateId(id) {
        this.setState({ blogId: id })
        console.log("update " + this.state.blogId)
    }

    render() {
        return (
            <>
                <h2>Профиль</h2>
                <CommonProfileData onReceived={(id) => this.updateId(id)} />
                <br />
                {this.state.blogId !== null && <BlogPosts blogId={this.state.blogId} />}
            </>
        );
    }
}



const CommonProfileData = function (props) {
    const hasMounted = useRef(false);

    const [profile, setProfile] = useState({
        id: null,
        name: "",
        description: null,
        createdAt: "",
        photoUrl: null,
        profileId: ""
    })

    useEffect(() => {
        const url = "http://localhost:7892/profile/Blog/detail";
        if (hasMounted.current) return;

        function sendRequest() {
            fetch(url, {
                headers: {
                    'Authorization': JwtTokenService.getFormatedTokenForHeader(),
                    'Content-Type': 'appplication/json'
                },
                method: 'GET'
            }).then(response => {
                if (response.ok) {
                    hasMounted.current = true;
                    return response.json()
                }
                if (response.status === 401) {
                    JwtTokenService.refreshToken();
                    window.location.reload();
                }
            })
                .then(result => {
                    setProfile(result);
                    props.onReceived(result.id);
                }, [])
        }
        sendRequest();
    })

    return (
        <div>
            <ul>
                <img src={profile.url}></img>
                <li>Называние блога: {profile.name}</li>
                <li>Описание: {profile.description}</li>
                <li>Создан: {profile.createdAt}</li>
            </ul>
        </div>
    );

}


const BlogPosts = function (props) {

    const hasMounted = useRef(false);
    const [posts, setPosts] = useState([{
        id: "7a1c3e30-eccb-4fc4-823d-62af6b1ff9df",
        type: 1,
        title: "Тестовое видео",
        description: "Тестовое видео",
        createdAt: "2025-01-09T08:31:44.652343+00:00",
        previewId: null,
        videoData: {
            id: "5bf456a0-4636-4934-af21-1675aa0505f0",
            length: 18851336,
            contentType: "video/mp4"
        }
    }])

    useEffect(() => {
        const url = `http://localhost:7892/profile/Blog/posts/list?blogId=${props.blogId}&page=${1}&limit=${100}`;
        if (hasMounted.current) return;

        function sendRequest() {
            fetch(url, {
                headers: {
                    'Authorization': JwtTokenService.getFormatedTokenForHeader(),
                    'Content-Type': 'appplication/json'
                },
                method: 'GET'
            }).then(response => {
                if (response.ok) {
                    hasMounted.current = true;
                    return response.json()
                }
                if (response.status === 401) {
                    JwtTokenService.refreshToken();
                    window.location.reload();
                }
            })
                .then(result => {
                    setPosts(result.posts);
                    console.log(result);
                }, [])
        }
        sendRequest();
    })


    return (
        <div>
            <h4>
                Посты
                <br />
                <button>Создать</button>
            </h4>
            <table>
                <thead>
                    <tr>
                        <th scope="col">Название</th>
                        <th scope="col">Описание</th>
                        <th scope="col">Дата создания</th>
                        <th scope="col">Тип</th>
                        <th scope="col">Видео</th>
                    </tr>
                </thead>
                <tbody>
                    {posts.map(x => {
                        const date = new Date(x.createdAt);
                        return <tr key={x.id}>
                            <td>{x.title}</td>
                            <td>{x.description}</td>
                            <td>{`${date.toLocaleDateString('ru')} ${date.toLocaleTimeString()}`}</td>
                            <td>{x.type === 1 ? 'Видео' : 'Текстовый'}</td>
                            <td>{x.previewId === null ? <></> : getVideo(x)} </td>
                        </tr>
                    })}
                </tbody>
            </table>
        </div>
    );
}

function getVideo(props) {
    const url = `http://localhost:7892/profile/Blog/video/chunks?postId=${props.id}&resolution=1080`;
    return <>
        <video controls poster={props.previewId} width={500}>
            <source src={url}></source></video>
    </>
}