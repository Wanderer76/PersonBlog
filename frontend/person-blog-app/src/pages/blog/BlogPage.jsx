import React, { useEffect, useRef, useState } from "react";
import { JwtTokenService } from "../../scripts/TokenStrorage";
import './BlogPage.css'
import { CreatePostForm } from "../../components/blog/CreatePostForm";
import API, { BaseApUrl } from "../../scripts/apiMethod";
import VideoPlayer from '../../components/VideoPlayer/VideoPlayer'
import 'video.js';
import { EditPostForm } from "../../components/blog/EditPostForm";

export class BlogPage extends React.Component {

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
                <h2>Блог</h2>
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
    }, [])

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
    const [editForm, setEditForm] = useState(null);
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

    async function handleEdit(params) {
    }


    return (
        <div>
            <h4>
                Посты
                <br />
                <button onClick={() => setShowCreateForm(true)}>Создать</button>
            </h4>
            {showCreateForm && <CreatePostForm onHandleClose={() => setShowCreateForm(false)}></CreatePostForm>}
            {editForm && <EditPostForm post={editForm} onHandleClose={() => setEditForm(null)}></EditPostForm>}
            <table>
                <thead>
                    <tr>
                        <th scope="col">Название</th>
                        <th scope="col">Описание</th>
                        <th scope="col">Дата создания</th>
                        <th scope="col">Тип</th>
                        <th scope="col">Видео</th>
                        <th scope="col"></th>
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
                            <td><button onClick={() => setEditForm({ ...x })}>Редактировать</button> </td>
                        </tr>
                    })}
                </tbody>
            </table>
        </div>
    );
}

function getVideo(props) {
    const url = `${BaseApUrl}/video/Video/video/v2/${props.id}/chunks/${props.videoData.objectName}`;
    return <>
        <VideoPlayer thumbnail={props.previewId} path={
            { url: url, label: 'd', postId: props.id, res: 0, objectName: props.videoData.objectName }}>
        </VideoPlayer >
    </>
}