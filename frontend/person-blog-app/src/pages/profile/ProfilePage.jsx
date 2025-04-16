import { useEffect, useRef, useState } from "react";
import { JwtTokenService } from "../../scripts/TokenStrorage";
import API from "../../scripts/apiMethod";
import './ProfilePage.css';
import { CreatePostForm } from "../../components/blog/CreatePostForm";
import { EditPostForm } from "../../components/blog/EditPostForm";
import { useNavigate } from "react-router-dom";

const ProfilePage = () => {

    const [profile, setProfile] = useState({
        avatar: "https://example.com/avatar.jpg",
        name: "Мой видеоблог",
        email: "user@example.com",
        postsCount: 12,
        createdAt: "15 марта 2024"
    });

    const blogId = useRef(null);
    const [showCreateForm, setShowCreateForm] = useState(false);
    const [editForm, setEditForm] = useState(null);

    const [posts, setPosts] = useState([
        {
            id: 1,
            title: "Первый пост в блоге",
            description: "Это мой первый видео-пост, посвященный...",
            createdAt: "2024-05-15",
            views: 245,
            state: "Опубликован",
            previewId: "/post-thumb-1.jpg",
            duration: "08:32"
        },

        // ... другие посты
    ]);
    const navigate = useNavigate();

    useEffect(() => {
        const url = "/profile/api/Blog/detail";

        async function sendRequest() {
            await API.get(url).then(response => {
                if (response.status === 200) {
                    return response.data
                }
                if (response.status === 401) {
                    JwtTokenService.refreshToken();
                    window.location.reload();
                }
            })
                .then(result => {
                    setProfile(result);
                    if (!blogId.current)
                        blogId.current = result.id
                    loadPosts();
                }, [])
        }
        sendRequest();
    }, [])

    function loadPosts() {
        if (blogId.current) {
            const url = `/profile/api/Post/list?blogId=${blogId.current}&page=${1}&limit=${10}`;
            API.get(url, {
                headers: {
                    'Authorization': JwtTokenService.isAuth() ? JwtTokenService.getFormatedTokenForHeader() : null,
                    'Content-Type': 'appplication/json'
                },
            }).then(response => {
                if (response.status === 200) {
                    var result = response.data;
                    setPosts(result.posts);
                }
                if (response.status === 401) {
                    JwtTokenService.refreshToken();
                    window.location.reload();
                }
            });
        }
    }

    async function handleRemove(id) {
        const url = `profile/api/Post/delete/${id}`;
        const response = await API.delete(url, {
            headers: {
                Authorization: JwtTokenService.getFormatedTokenForHeader()
            }
        });
        if (response.status === 200) {
            setPosts(posts.filter(x => x.id !== id))
        }
    }

    return (<>
        <div className="profileContainer">
            <div className="profileHeader">
                <div className="avatarSection">
                    <div className="avatarWrapper">
                        <img
                            src={profile.avatar}
                            alt="Аватар"
                            className="profileAvatar"
                        />
                        <button className="avatarEditBtn">
                            ✏️
                        </button>
                    </div>
                    <div className="profileInfo">
                        <h1 className="blogTitle">{profile.name}</h1>
                        <div className="profileMeta">
                            <span className="email">📧 {profile.email}</span>
                            <span className="registrationDate">📅 Зарегистрирован: {profile.createdAt}</span>
                            <span className="postsCount">📝 Постов: {profile.postsCount}</span>
                        </div>
                    </div>
                </div>

                <button className="btn btnSecondary">
                    Редактировать профиль
                </button>
            </div>

            <div className="postsSection">
                <div className="postSectionHeader">
                    <h2 >Мои публикации</h2>
                    <button className="btn btnPrimary createPostBtn" onClick={(e) => navigate('post/create')}>Создать</button>
                </div>
                <div className="postsGrid">
                    {posts.map(post => (
                        <div key={post.id} className="postCard">
                            <div className="postThumbnail">
                                <img src={post.previewId} alt={post.title} />
                                <div className="videoDuration">{post.duration}</div>
                                {post.type === 1 &&
                                    <div className="postStatus">{post.state === 1 ? "Опубликовано" : post.state === 0 ? "В обработке" : post.errorMessage}</div>
                                }
                            </div>

                            <div className="postContent">
                                <h3 className="postTitle">{post.title}</h3>
                                <p className="postDescription">{post.description}</p>

                                <div className="postMeta">
                                    <div className="postStats">
                                        <span>👁 {post.views}</span>
                                        <span>📅 {new Date(post.createdAt).toLocaleDateString()}</span>
                                    </div>
                                    <div className="postActions">
                                        <button className="btn btnSecondary" onClick={() => setEditForm({ ...post })}>Редактировать</button>
                                        <button className="btn btnPrimary" onClick={() => handleRemove(post.id)}>Удалить</button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            </div>
        </div>
        {showCreateForm && <CreatePostForm onHandleClose={() => setShowCreateForm(false)} onCreate={() => {
            loadPosts()
        }}></CreatePostForm>}
        {editForm && <EditPostForm post={editForm} onHandleClose={() => setEditForm(null)}></EditPostForm>}
    </>
    );
};

// Стили

// Добавляем стили в документ
export default ProfilePage;