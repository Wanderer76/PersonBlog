import { useCallback, useEffect, useRef, useState } from "react";
import { JwtTokenService } from "../../scripts/TokenStrorage";
import API from "../../scripts/apiMethod";
import './ProfilePage.css';
import { useNavigate } from "react-router-dom";
import DefaultProfileIcon from '../../defaultProfilePic.png'
import { getLocalDateTime } from "../../scripts/LocalDate";

const ProfilePage = () => {

    const [profile, setProfile] = useState({
        avatar: DefaultProfileIcon,
        name: "Мой видеоблог",
        email: "user@example.com",
        totalPostsCount: 12,
        createdAt: "15 марта 2024"
    });
    const blogId = useRef(null);
    const [page, setPage] = useState(1);
    const [hasMore, setHasMore] = useState(true);
    const [isLoading, setIsLoading] = useState(false);
    const observer = useRef();
    const pageSize = 10; // Фиксированный размер страницы

    const lastPostRef = useCallback(node => {
        if (observer.current) observer.current.disconnect();

        observer.current = new IntersectionObserver(entries => {
            if (entries[0].isIntersecting && hasMore) {
                setPage(prev => prev + 1);
            }
        });

        if (node) observer.current.observe(node);
    }, [hasMore]);

    const [posts, setPosts] = useState([]);
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

                }, [])
        }
        sendRequest();
    }, [])

    useEffect(() => {
        if (blogId.current) {
            loadPosts();
        }
    }, [blogId.current, page])

    function loadPosts() {
        if (blogId.current) {
            const url = `/profile/api/Post/list?blogId=${blogId.current}&page=${page}&limit=${pageSize}`;
            API.get(url).then(response => {
                if (response.status === 200) {
                    var result = response.data;
                    setPosts(prev => [...prev, ...result.posts]);
                    setProfile((prev) => (
                        {
                            ...prev,
                            ['totalPostsCount']: result.totalPostsCount
                        }
                    ))
                    setHasMore(result.posts.length >= pageSize);
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
                            src={profile.photoUrl ?? DefaultProfileIcon}
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
                            <span className="registrationDate">📅 Зарегистрирован: {getLocalDateTime(profile.createdAt)}</span>
                            <span className="postsCount">📝 Постов: {profile.totalPostsCount}</span>
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
                    {posts.map((post, index) => {

                        if (posts.length == index + 1) {
                            return (
                                <div key={post.id} className="postCard" ref={lastPostRef} >
                                    <div className="postThumbnail">
                                        <img src={post.previewId} alt={post.title} />
                                        <div className="videoDuration">{post.duration}</div>
                                        {post.type === 1 &&
                                            <div className="postStatus">{post.state === 1 ? "Опубликовано" : post.state === 0 ? "В обработке" : post.errorMessage}</div>}
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
                                                <button className="btn btnSecondary">Редактировать</button>
                                                <button className="btn btnPrimary" onClick={() => handleRemove(post.id)}>Удалить</button>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            );
                        }


                        return (
                            <div key={post.id} className="postCard">
                                <div className="postThumbnail">
                                    <img src={post.previewId} alt={post.title} />
                                    <div className="videoDuration">{post.duration}</div>
                                    {post.type === 1 &&
                                        <div className="postStatus">{post.state === 1 ? "Опубликовано" : post.state === 0 ? "В обработке" : post.errorMessage}</div>}
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
                                            <button className="btn btnSecondary">Редактировать</button>
                                            <button className="btn btnPrimary" onClick={() => handleRemove(post.id)}>Удалить</button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        );
                    })}
                </div>
            </div>
        </div>
    </>
    );
};

// Стили

// Добавляем стили в документ
export default ProfilePage;