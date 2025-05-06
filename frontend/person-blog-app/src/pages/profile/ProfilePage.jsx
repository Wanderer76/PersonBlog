import { useCallback, useEffect, useRef, useState } from "react";
import { JwtTokenService } from "../../scripts/TokenStrorage";
import API from "../../scripts/apiMethod";
import './ProfilePage.css';
import { useNavigate } from "react-router-dom";
import DefaultProfileIcon from '../../defaultProfilePic.png'
import { getLocalDateTime } from "../../scripts/LocalDate";
import PlaylistEditorModal from "../../components/playList/PlaylistEditorModal";

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
    const observer = useRef();
    const pageSize = 10; // Фиксированный размер страницы
    const [activePanel, setActivePanel] = useState('posts');

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
    const [playLists, setPlayLists] = useState([]);
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

    useEffect(() => {
        if (blogId.current) {
            API.get(`/profile/api/PlayList/list?blogId=${blogId.current}`).then(response => {
                if (response.status === 200) {
                    setPlayLists(response.data)
                }
            })
        }
    }, [blogId.current])

    async function loadPosts() {
        if (blogId.current) {
            const url = `/profile/api/Post/list?blogId=${blogId.current}&page=${page}&limit=${pageSize}`;
            await API.get(url).then(response => {
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

    function drawPosts() {
        return posts.map((post, index) => {
            if (posts.length == index + 1) {
                return (
                    <div key={post.id} className="postCard" ref={lastPostRef}>
                        <div className="postThumbnail" onClick={(e) => { e.preventDefault(); if (post.state === 1) navigate(`/video/${post.id}`); }}>
                            <img src={post.previewId} alt={post.title} />
                            <div className="videoDuration">{post.duration}</div>
                            {post.type === 1 &&
                                <div className="postStatus">{post.state === 1 ? "Опубликовано" : post.state === 0 ? "В обработке" : post.errorMessage}</div>}
                        </div>
                        <div className="postContent">
                            <h3 className="postTitle" onClick={(e) => { e.preventDefault(); if (post.state === 1) navigate(`/video/${post.id}`); }}>{post.title}</h3>
                            <p className="postDescription">{post.description}</p>

                            <div className="postMeta">
                                <div className="postStats">
                                    <span>👁 {post.views}</span>
                                    <span>📅 {new Date(post.createdAt).toLocaleDateString()}</span>
                                </div>
                                <div className="postActions">
                                    <button className="btn btnPrimary">Редактировать</button>
                                    <button className="btn btnSecondary" onClick={() => handleRemove(post.id)}>Удалить</button>
                                </div>
                            </div>
                        </div>
                    </div>
                );
            }

            return (
                <div key={post.id} className="postCard">
                    <div className="postThumbnail" onClick={(e) => { e.preventDefault(); if (post.state === 1) navigate(`/video/${post.id}`); }}>
                        <img src={post.previewId} alt={post.title} />
                        <div className="videoDuration">{post.duration}</div>
                        {post.type === 1 &&
                            <div className="postStatus">{post.state === 1 ? "Опубликовано" : post.state === 0 ? "В обработке" : post.errorMessage}</div>}
                    </div>
                    <div className="postContent">
                        <h3 className="postTitle" onClick={(e) => { e.preventDefault(); if (post.state === 1) navigate(`/video/${post.id}`); }}>{post.title}</h3>
                        <p className="postDescription">{post.description}</p>

                        <div className="postMeta">
                            <div className="postStats">
                                <span>👁 {post.views}</span>
                                <span>📅 {new Date(post.createdAt).toLocaleDateString()}</span>
                            </div>
                            <div className="postActions">
                                <button className="btn btnPrimary" >Редактировать</button>
                                <button className="btn btnSecondary" onClick={() => handleRemove(post.id)}>Удалить</button>
                            </div>
                        </div>
                    </div>

                </div>
            );
        })
    }

    function drawPlayLists() {
        return playLists.map((post, index) => {
            // if (playLists.length == index + 1) {
            //     return (
            //         <div key={post.id} className="postCard" ref={lastPostRef} >
            //             <article class="playlist-card">
            //                 <div class="playlist-cover">
            //                     <img src={post.thumbnailUrl} alt="Обложка плейлиста" />
            //                     <span class="playlist-badge video-count">{post.posts.length} видео</span>
            //                     {/* <span class="playlist-badge privacy-status">Приватный</span> */}
            //                 </div>
            //                 <div class="playlist-info">
            //                     <h3 class="playlist-title">{post.title}</h3>
            //                     <p class="playlist-description">{post?.description}</p>
            //                     {/* <div class="playlist-stats">
            //                         <span>1.2K просмотров</span>
            //                         <span>•</span>
            //                         <span>3 дня назад</span>
            //                     </div> */}
            //                     <div class="playlist-actions">
            //                         <button class="btn btnPrimary">Редактировать</button>
            //                         <button class="btn btnSecondary">Удалить</button>
            //                     </div>
            //                 </div>
            //             </article>
            //         </div>
            //     );
            // }

            return (
                <div key={post.id} className="postCard">
                    <article class="playlist-card">
                        <div class="playlist-cover">
                            <img src="cover1.jpg" alt="Обложка плейлиста" />
                            <span class="playlist-badge video-count">{post.posts.length} видео</span>
                            {/* <span class="playlist-badge privacy-status">Приватный</span> */}
                        </div>
                        <div class="playlist-info">
                            <h3 class="playlist-title">{post.title}</h3>
                            <p class="playlist-description">{post?.description}</p>
                            {/* <div class="playlist-stats">
                                <span>1.2K просмотров</span>
                                <span>•</span>
                                <span>3 дня назад</span>
                            </div> */}
                            <div class="playlist-actions">
                                <button class="btn btnPrimary">Редактировать</button>
                                <button class="btn btnSecondary">Удалить</button>
                            </div>
                        </div>
                    </article>
                </div>
            );
        })
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
                    <div className="tabButtons">
                        <button
                            className={`tabButton ${activePanel === 'posts' ? 'active' : ''}`}
                            onClick={() => setActivePanel('posts')}
                        >
                            Мои публикации
                        </button>
                        <button
                            className={`tabButton ${activePanel === 'playlists' ? 'active' : ''}`}
                            onClick={() => setActivePanel('playlists')}
                        >
                            Плейлисты
                        </button>
                    </div>
                    {activePanel == 'posts' && <button className="btn btnPrimary createPostBtn" onClick={(e) => navigate('post/create')}>Создать пост</button>}
                    {activePanel == 'playlists' && <button className="btn btnPrimary createPostBtn" onClick={(e) => navigate('playList/create')}>Создать плейлист</button>}
                </div>
                <div className="postsGrid">
                    {activePanel === 'posts' && drawPosts()}
                    {activePanel === 'playlists' && drawPlayLists()}
                </div>
            </div>
        </div>
    </>
    );
};

// Стили

// Добавляем стили в документ
export default ProfilePage;