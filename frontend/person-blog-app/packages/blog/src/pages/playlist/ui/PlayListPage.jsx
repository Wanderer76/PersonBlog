import { memo, useCallback, useEffect, useMemo, useState } from 'react';
import { DragDropContext, Draggable, Droppable } from 'react-beautiful-dnd';
import { useNavigate, useParams } from 'react-router-dom';
import { getPlayList } from '@/shared/api/generated/play-list/play-list';
import { PlayListContentTypeModel } from '@/shared/api/generated/models';
import { loadPlaylist } from '@/entities/playlist';
import { PageShell } from '@/widgets/page-shell';
import './PlaylistPage.css';

const playListApi = getPlayList();

const PostArtwork = memo(({ post, isText, className = '' }) => {
    if (!isText && post.previewObjectName) {
        return <img className={className} src={post.previewObjectName} alt="" />;
    }

    return <span className={`playlistArtworkPlaceholder ${className}`} aria-hidden="true">{isText ? 'Aa' : '▶'}</span>;
});

const PlaylistPost = memo(({ post, index, isText, canEdit, onOpen, onRemove }) => (
    <Draggable draggableId={post.id} index={index} isDragDisabled={!canEdit}>
        {(provided, snapshot) => (
            <article
                ref={provided.innerRef}
                {...provided.draggableProps}
                className={`playlistPost ${snapshot.isDragging ? 'playlistPostDragging' : ''}`}
            >
                <button
                    type="button"
                    className="playlistDragHandle"
                    aria-label={canEdit ? 'Перетащить публикацию' : `Позиция ${index + 1}`}
                    {...provided.dragHandleProps}
                >
                    <span>{String(index + 1).padStart(2, '0')}</span>
                    {canEdit && <small>⋮⋮</small>}
                </button>
                <button type="button" className="playlistPostMain" onClick={() => onOpen(post.id)}>
                    <PostArtwork post={post} isText={isText} className="playlistPostArtwork" />
                    <span className="playlistPostCopy">
                        <strong>{post.title || 'Без названия'}</strong>
                        <span>{post.description || (isText ? 'Статья' : 'Видео')}</span>
                        {post.creator?.name && <small>Автор: {post.creator.name}</small>}
                    </span>
                </button>
                {canEdit && (
                    <button type="button" className="playlistRemoveButton" onClick={() => onRemove(post.id)}>
                        Удалить
                    </button>
                )}
            </article>
        )}
    </Draggable>
));

const AddPostsModal = memo(({ isOpen, isText, posts, selectedIds, isLoading, isSaving, error, onToggle, onClose, onSave }) => {
    if (!isOpen) return null;
    const label = isText ? 'статьи' : 'видео';

    return (
        <div className="playlistModalOverlay" role="presentation" onMouseDown={onClose}>
            <section className="playlistModal" role="dialog" aria-modal="true" aria-labelledby="add-posts-title" onMouseDown={(event) => event.stopPropagation()}>
                <header className="playlistModalHeader">
                    <div>
                        <span>Пополнить плейлист</span>
                        <h2 id="add-posts-title">Добавить {label}</h2>
                    </div>
                    <button type="button" onClick={onClose} aria-label="Закрыть">×</button>
                </header>
                <div className="playlistModalBody">
                    {error && <div className="playlistError" role="alert">{error}</div>}
                    {isLoading && <div className="playlistModalState">Загружаем публикации…</div>}
                    {!isLoading && posts.length === 0 && <div className="playlistModalState">Все доступные публикации уже добавлены</div>}
                    {!isLoading && posts.map((post) => {
                        const isSelected = selectedIds.has(post.id);
                        return (
                            <button
                                type="button"
                                key={post.id}
                                className={`playlistOption ${isSelected ? 'playlistOptionSelected' : ''}`}
                                onClick={() => onToggle(post.id)}
                            >
                                <PostArtwork post={post} isText={isText} className="playlistOptionArtwork" />
                                <span>
                                    <strong>{post.title || 'Без названия'}</strong>
                                    <small>{post.creator?.name || post.description || (isText ? 'Статья' : 'Видео')}</small>
                                </span>
                                <i aria-hidden="true">{isSelected ? '✓' : '+'}</i>
                            </button>
                        );
                    })}
                </div>
                <footer className="playlistModalFooter">
                    <span>Выбрано: <strong>{selectedIds.size}</strong></span>
                    <div>
                        <button type="button" className="playlistButtonSecondary" onClick={onClose}>Отмена</button>
                        <button type="button" className="playlistButtonPrimary" disabled={!selectedIds.size || isSaving} onClick={onSave}>
                            {isSaving ? 'Добавляем…' : 'Добавить'}
                        </button>
                    </div>
                </footer>
            </section>
        </div>
    );
});

const PlaylistPage = () => {
    const navigate = useNavigate();
    const { playlistId } = useParams();
    const [playlist, setPlaylist] = useState(null);
    const [posts, setPosts] = useState([]);
    const [availablePosts, setAvailablePosts] = useState([]);
    const [selectedIds, setSelectedIds] = useState(new Set());
    const [isLoading, setIsLoading] = useState(true);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [isModalLoading, setIsModalLoading] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [isReordering, setIsReordering] = useState(false);
    const [error, setError] = useState('');
    const [modalError, setModalError] = useState('');

    const isText = playlist?.contentType === PlayListContentTypeModel.NUMBER_1;
    const contentLabel = isText ? 'статей' : 'видео';

    const fetchPlaylist = useCallback(async () => {
        if (!playlistId) return;
        setIsLoading(true);
        setError('');
        try {
            const data = await loadPlaylist(playlistId);
            setPlaylist(data.playList ?? null);
            setPosts(data.postPage?.items ?? []);
        } catch {
            setError('Не удалось загрузить плейлист. Попробуйте обновить страницу.');
        } finally {
            setIsLoading(false);
        }
    }, [playlistId]);

    useEffect(() => { void fetchPlaylist(); }, [fetchPlaylist]);

    const openPost = useCallback((postId) => {
        navigate(isText
            ? `/textPost/${postId}`
            : `/videoPage/${postId}?playlistId=${encodeURIComponent(playlistId)}`);
    }, [isText, navigate, playlistId]);

    const openModal = useCallback(async () => {
        setIsModalOpen(true);
        setIsModalLoading(true);
        setModalError('');
        setSelectedIds(new Set());
        try {
            const { data } = await playListApi.getApiPlayListAvailableVideos({ playListId: playlistId });
            setAvailablePosts(data ?? []);
        } catch {
            setAvailablePosts([]);
            setModalError('Не удалось загрузить доступные публикации.');
        } finally {
            setIsModalLoading(false);
        }
    }, [playlistId]);

    const togglePost = useCallback((postId) => {
        setSelectedIds((current) => {
            const next = new Set(current);
            if (next.has(postId)) next.delete(postId);
            else next.add(postId);
            return next;
        });
    }, []);

    const addPosts = useCallback(async () => {
        if (!selectedIds.size || isSaving) return;
        setIsSaving(true);
        setModalError('');
        try {
            await playListApi.postApiPlayListAddVideo({ playListId: playlistId, postsToAdd: [...selectedIds] });
            await fetchPlaylist();
            setIsModalOpen(false);
        } catch {
            setModalError(`Не удалось добавить ${contentLabel}. Проверьте выбранные публикации.`);
        } finally {
            setIsSaving(false);
        }
    }, [contentLabel, fetchPlaylist, isSaving, playlistId, selectedIds]);

    const removePost = useCallback(async (postId) => {
        const previous = posts;
        setPosts((current) => current.filter((post) => post.id !== postId));
        setError('');
        try {
            await playListApi.postApiPlayListRemoveVideo({ playListId: playlistId, postId });
        } catch {
            setPosts(previous);
            setError('Не удалось удалить публикацию из плейлиста.');
        }
    }, [playlistId, posts]);

    const reorderPosts = useCallback(async ({ source, destination }) => {
        if (!destination || destination.index === source.index || isReordering) return;
        const previous = posts;
        const reordered = [...posts];
        const [moved] = reordered.splice(source.index, 1);
        reordered.splice(destination.index, 0, moved);
        setPosts(reordered);
        setIsReordering(true);
        setError('');
        try {
            await playListApi.postApiPlayListUpdatePositions({
                playlistId,
                postId: moved.id,
                destination: destination.index + 1,
            });
        } catch {
            setPosts(previous);
            setError('Не удалось сохранить новый порядок публикаций.');
        } finally {
            setIsReordering(false);
        }
    }, [isReordering, playlistId, posts]);

    const heroArtworkUrl = useMemo(
        () => playlist?.thumbnailUrl || (!isText ? posts.find((post) => post.previewObjectName)?.previewObjectName : ''),
        [isText, playlist?.thumbnailUrl, posts],
    );

    if (isLoading) {
        return <PageShell className="playlistPage" contentClassName="playlistShell"><div className="playlistPageState">Загружаем плейлист…</div></PageShell>;
    }

    if (!playlist) {
        return <PageShell className="playlistPage" contentClassName="playlistShell"><div className="playlistPageState playlistPageError">{error || 'Плейлист не найден'}</div></PageShell>;
    }

    return (
        <PageShell className="playlistPage" contentClassName="playlistShell">
            <button type="button" className="playlistBack" onClick={() => navigate(-1)}>← Назад</button>
            <header className="playlistHero">
                <div className="playlistHeroArtwork">
                    {heroArtworkUrl
                        ? <img src={heroArtworkUrl} alt="Обложка плейлиста" />
                        : <span aria-hidden="true">{isText ? 'Aa' : '▶'}</span>}
                    <i>{posts.length}</i>
                </div>
                <div className="playlistHeroCopy">
                    <span className="playlistEyebrow">Плейлист · {isText ? 'Статьи' : 'Видео'}</span>
                    <h1>{playlist.title || 'Без названия'}</h1>
                    <p>{posts.length} {contentLabel} в подборке</p>
                    {playlist.canEdit && <button type="button" className="playlistButtonPrimary" onClick={openModal}>＋ Добавить публикации</button>}
                </div>
            </header>

            <section className="playlistContent">
                <div className="playlistSectionHeader">
                    <div><span>Содержимое</span><h2>Публикации</h2></div>
                    {playlist.canEdit && <small>{isReordering ? 'Сохраняем порядок…' : 'Перетаскивайте карточки для сортировки'}</small>}
                </div>
                {error && <div className="playlistError" role="alert">{error}</div>}
                {posts.length > 0 ? (
                    <DragDropContext onDragEnd={reorderPosts}>
                        <Droppable droppableId="playlist-posts">
                            {(provided) => (
                                <div className="playlistPosts" ref={provided.innerRef} {...provided.droppableProps}>
                                    {posts.map((post, index) => (
                                        <PlaylistPost key={post.id} post={post} index={index} isText={isText} canEdit={Boolean(playlist.canEdit) && !isReordering} onOpen={openPost} onRemove={removePost} />
                                    ))}
                                    {provided.placeholder}
                                </div>
                            )}
                        </Droppable>
                    </DragDropContext>
                ) : (
                    <div className="playlistEmpty">
                        <span aria-hidden="true">{isText ? 'Aa' : '▶'}</span>
                        <h3>Плейлист пока пуст</h3>
                        <p>{playlist.canEdit ? 'Добавьте первые публикации — формат плейлиста уже зафиксирован.' : 'Автор пока не добавил публикации.'}</p>
                        {playlist.canEdit && <button type="button" className="playlistButtonPrimary" onClick={openModal}>Добавить публикации</button>}
                    </div>
                )}
            </section>

            <AddPostsModal
                isOpen={isModalOpen}
                isText={isText}
                posts={availablePosts}
                selectedIds={selectedIds}
                isLoading={isModalLoading}
                isSaving={isSaving}
                error={modalError}
                onToggle={togglePost}
                onClose={() => setIsModalOpen(false)}
                onSave={addPosts}
            />
        </PageShell>
    );
};

export default PlaylistPage;
