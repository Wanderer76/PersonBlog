import React, { useEffect, useRef, useState } from "react";
import { JwtTokenService } from "../../scripts/TokenStrorage";
import './ProfilePage.css'
import { CreatePostForm } from "../../components/profile/CreatePostForm";
import API from "../../scripts/apiMethod";
import VideoPlayer from '../../components/VideoPlayer/VideoPlayer'
import 'video.js';

export class ProfilePage extends React.Component {

    constructor(props) {
        super(props);
        this.state = {
            blogId: null
        }
    }

    updateId(id) {
        this.setState({ blogId: id })
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
        const url = "/profile/Blog/detail";
        if (hasMounted.current) return;

        async function sendRequest() {
            await API.get(url, {
                headers: {
                    'Authorization': JwtTokenService.getFormatedTokenForHeader(),
                    'Content-Type': 'appplication/json'
                },
            }).then(response => {
                if (response.status === 200) {
                    hasMounted.current = true;
                    return response.data
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
    const [showCreateForm, setShowCreateForm] = useState(false);
    const [posts, setPosts] = useState([{
        id: null,
        type: 1,
        title: "",
        description: "",
        createdAt: null,
        previewId: null,
        videoData: {
            id: null,
            length: 0,
            contentType: null,
            resolutions: [] | Array
        } | null,
        isProcessed: true
    }])

    useEffect(() => {
        const url = `/profile/Blog/posts/list?blogId=${props.blogId}&page=${1}&limit=${100}`;
        if (hasMounted.current) return;

        API.get(url, {
            headers: {
                'Authorization': JwtTokenService.isAuth() ? JwtTokenService.getFormatedTokenForHeader() : null,
                'Content-Type': 'appplication/json'
            },
        }).then(response => {
            if (response.status === 200) {
                hasMounted.current = true;
                var result = response.data;
                setPosts(result.posts);
            }
            if (response.status === 401) {
                JwtTokenService.refreshToken();
                window.location.reload();
            }
        }, [])

    })

    async function handleRemove(id) {
        const url = `profile/post/delete/${id}`;
        const response = await API.delete(url, {
            headers: {
                Authorization: JwtTokenService.getFormatedTokenForHeader()
            }
        });
        if (response.status === 200) {
            setPosts(posts.filter(x => x.id !== id))
        }
    }


    return (
        <div>
            <h4>
                Посты
                <br />
                <button onClick={() => setShowCreateForm(true)}>Создать</button>
            </h4>
            {showCreateForm && <CreatePostForm onHandleClose={() => setShowCreateForm(false)}></CreatePostForm>}
            <table>
                <thead>
                    <tr>
                        <th scope="col">Название</th>
                        <th scope="col">Описание</th>
                        <th scope="col">Дата создания</th>
                        <th scope="col">Тип</th>
                        <th scope="col">Видео</th>
                        <th scope="col"></th>
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
                            <td>{x.type === 1 && x.isProcessed ? <p>В обработке</p> : getVideo(x)} </td>
                            <td><button onClick={() => handleRemove(x.id)}>Удалить</button> </td>
                        </tr>
                    })}
                </tbody>
            </table>
        </div>
    );
}

function getVideo(props) {
    const url = `http://localhost:7892/profile/Blog/video/v2/${props.id}/chunks/${props.videoData.objectName}`;
    return <>

        {/* <video controls poster={props.previewId} width={500}>
            <source src={url}></source></video> */}

        <VideoPlayer thumbnail={props.previewId} qualities={[
            { path: url, label: 'd' }]
        }>

        </VideoPlayer >


    </>
}