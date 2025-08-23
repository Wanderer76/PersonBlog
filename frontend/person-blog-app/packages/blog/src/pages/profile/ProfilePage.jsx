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
        name: null,
        totalPostsCount: 0,
        createdAt: null
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

            const hasBlog = await API.get(`/profile/api/Blog/hasUserBlog`)
            if (hasBlog.data.hasBlog !== null) {
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


    async function handleRemovePlaylist(id) {
        API.post(`/profile/api/PlayList/removePlaylist/${id}`)
            .then(response => {
                if (response.status === 200) {
                    setPlayLists(prev => [...prev.filter(x => x.id != id)])
                }
                if(response.status === 400){
                    alert(response.data);
                }
            });
    }

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
                    <CreatePostCard key={post.id} post={post} lastPostRef={lastPostRef} navigate={navigate} handleRemove={handleRemove} />
                );
            }
            else
                return (<CreatePostCard key={post.id} post={post} lastPostRef={null} navigate={navigate} handleRemove={handleRemove} />)

            // return (
            //     <div key={post.id} className="postCard">
            //         <div className="postThumbnail" onClick={(e) => { e.preventDefault(); if (post.state === 1) navigate(`/video/${post.id}`); }}>
            //             <img src={post.previewId} alt={post.title} />
            //             <div className="videoDuration">{post.duration}</div>
            //             {post.type === 1 &&
            //                 <div className="postStatus">{post.state === 1 ? "Опубликовано" : post.state === 0 ? "В обработке" : post.errorMessage}</div>}
            //         </div>
            //         <div className="postContent">
            //             <h3 className="postTitle" onClick={(e) => { e.preventDefault(); if (post.state === 1) navigate(`/video/${post.id}`); }}>{post.title}</h3>
            //             <p className="postDescription">{post.description}</p>

            //             <div className="postMeta">
            //                 <div className="postStats">
            //                     <span>👁 {post.views}</span>
            //                     <span>📅 {new Date(post.createdAt).toLocaleDateString()}</span>
            //                 </div>
            //                 <div className="postActions">
            //                     <button className="btn btnPrimary" >Редактировать</button>
            //                     <button className="btn btnSecondary" onClick={() => handleRemove(post.id)}>Удалить</button>
            //                 </div>
            //             </div>
            //         </div>

            //     </div>
            // );
        })
    }

    function drawPlayLists() {
        return playLists.map((playlist, index) => {
            return (
                <div key={playlist.id} className="postCard" >
                    <article className="playlist-card">
                        <div className="playlist-cover" onClick={(e) => { e.preventDefault(); navigate(`/playlist/${playlist.id}`); }}>
                            <img src={playlist.thumbnailUrl} alt="Обложка плейлиста" />
                            <span className="playlist-badge video-count">{playlist.posts.length} видео</span>
                            {/* <span class="playlist-badge privacy-status">Приватный</span> */}
                        </div>
                        <div className="playlist-info">
                            <h3 className="playlist-title">{playlist.title}</h3>
                            <p className="playlist-description">{playlist?.description}</p>
                            {/* <div class="playlist-stats">
                                <span>1.2K просмотров</span>
                                <span>•</span>
                                <span>3 дня назад</span>
                            </div> */}
                            <div className="playlist-actions">
                                <button className="btn btnPrimary">Редактировать</button>
                                <button className="btn btnSecondary" onClick={() => handleRemovePlaylist(playlist.id)}>Удалить</button>
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

                    <button className="btn btnSecondary" onClick={() => {
                        if (blogId.current) {
                            navigate('blog/edit')
                        }
                        else {
                            navigate('blog/create');
                        }
                    }}>
                        {blogId.current && 'Редактировать профиль'}
                        {!blogId.current && 'Создать блог'}
                    </button>

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
                            {/* <span className="email">📧 {profile.email}</span> */}
                            <span className="registrationDate">📅 Зарегистрирован: {profile.createdAt && getLocalDateTime(profile.createdAt)}</span>
                            <span className="postsCount">📝 Постов: {profile.totalPostsCount}</span>
                        </div>
                    </div>
                </div>

                <button className="btn btnRemove" onClick={() => {
                    JwtTokenService.cleanAuth();
                    navigate("/auth")
                }}>
                    Выход
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

const CreatePostCard = function ({ post, lastPostRef, navigate, handleRemove }) {

    const [uploadProgress, setUploadProgress] = useState(0);

    if (post.state === 2) {
        // if (result.posts.filter(x => x.state === 0).length > 0) {
        navigator.serviceWorker?.addEventListener('message', (event) => {
            if (event.data.type === 'CHUNK_UPLOADED') {
                if (event.data.payload.postId == post.id) {
                    const data = event.data.payload;
                    setUploadProgress(Math.round(data.chunkNumber / data.totalChunks * 100))
                }
            }
        });
    }
    // }
    return (

        <div className="postCard" ref={lastPostRef ? lastPostRef : null}>

            <div className="postThumbnail" onClick={(e) => { e.preventDefault(); if (post.state === 1) navigate(`/video/${post.id}`); }}>
                <img src={post.previewId} alt={post.title} />
                <div className="videoDuration">{post.duration}</div>
                {post.type === 1 &&
                    <div className="postStatus">{
                        post.state === 1
                            ? "Опубликовано"
                            : post.state === 0
                                ? "В обработке"
                                : post.state === 2 ?
                                    `Загрузка ${uploadProgress}%`
                                    : post.errorMessage
                    }</div>}
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
                        <button className="btn btnPrimary" onClick={() => navigate(`post/edit/${post.id}`)}>Редактировать</button>
                        <button className="btn btnSecondary" onClick={() => handleRemove(post.id)}>Удалить</button>
                    </div>
                </div>
            </div>
        </div >);
}
