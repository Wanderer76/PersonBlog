import { useCallback, useEffect, useMemo, useState } from 'react';
import { DragDropContext, Draggable, Droppable } from 'react-beautiful-dnd';
import { useNavigate } from 'react-router-dom';
import { getPlayList } from '@/shared/api/generated/play-list/play-list';
import { PlayListContentTypeModel, PlayListKindModel } from '@/shared/api/generated/models';
import styles from './CreatePlaylistForm.module.css';

const playListApi = getPlayList();
const ALLOWED_IMAGE_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp']);
const MAX_IMAGE_SIZE = 10 * 1024 * 1024;

const CONTENT_TYPES = [
    {
        value: PlayListContentTypeModel.NUMBER_0,
        title: 'Видео',
        description: 'Соберите выпуски в последовательный просмотр',
        icon: '▶',
    },
    {
        value: PlayListContentTypeModel.NUMBER_1,
        title: 'Статьи',
        description: 'Объедините публикации в тематическую подборку',
        icon: 'Aa',
    },
];

const getContentLabel = (contentType) =>
    contentType === PlayListContentTypeModel.NUMBER_1 ? 'статьи' : 'видео';

const PostPreview = ({ post, contentType }) => {
    if (contentType === PlayListContentTypeModel.NUMBER_0 && post.previewObjectName) {
        return <img className={styles.postPreviewImage} src={post.previewObjectName} alt="" />;
    }

    return (
        <div className={styles.postPreviewPlaceholder} aria-hidden="true">
            {contentType === PlayListContentTypeModel.NUMBER_1 ? 'Aa' : '▶'}
        </div>
    );
};

const CreatePlaylistForm = () => {
    const navigate = useNavigate();
    const [title, setTitle] = useState('');
    const [contentType, setContentType] = useState(PlayListContentTypeModel.NUMBER_0);
    const [thumbnail, setThumbnail] = useState(null);
    const [thumbnailPreview, setThumbnailPreview] = useState('');
    const [availablePosts, setAvailablePosts] = useState([]);
    const [selectedPosts, setSelectedPosts] = useState([]);
    const [isLoadingPosts, setIsLoadingPosts] = useState(true);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState('');

    const contentLabel = getContentLabel(contentType);
    const selectedIds = useMemo(
        () => new Set(selectedPosts.map((post) => post.id)),
        [selectedPosts],
    );

    const fetchAvailablePosts = useCallback(async () => {
        setIsLoadingPosts(true);
        setError('');

        try {
            const { data } = await playListApi.getApiPlayListAvailableVideos({ contentType });
            setAvailablePosts(data ?? []);
        } catch {
            setAvailablePosts([]);
            setError(`Не удалось загрузить доступные ${contentLabel}. Попробуйте ещё раз.`);
        } finally {
            setIsLoadingPosts(false);
        }
    }, [contentLabel, contentType]);

    useEffect(() => {
        setSelectedPosts([]);
        void fetchAvailablePosts();
    }, [fetchAvailablePosts]);

    useEffect(() => () => {
        if (thumbnailPreview) URL.revokeObjectURL(thumbnailPreview);
    }, [thumbnailPreview]);

    const selectContentType = (nextContentType) => {
        if (nextContentType !== contentType) setContentType(nextContentType);
    };

    const addPost = (post) => {
        setSelectedPosts((current) =>
            current.some((item) => item.id === post.id) ? current : [...current, post],
        );
    };

    const removePost = (postId) => {
        setSelectedPosts((current) => current.filter((post) => post.id !== postId));
    };

    const selectThumbnail = (file) => {
        if (!file) return;
        if (!ALLOWED_IMAGE_TYPES.has(file.type)) {
            setError('Для обложки выберите изображение JPG, PNG или WebP.');
            return;
        }
        if (file.size > MAX_IMAGE_SIZE) {
            setError('Размер обложки не должен превышать 10 МБ.');
            return;
        }

        setError('');
        setThumbnail(file);
        setThumbnailPreview(URL.createObjectURL(file));
    };

    const removeThumbnail = () => {
        setThumbnail(null);
        setThumbnailPreview('');
    };

    const onDragEnd = ({ source, destination }) => {
        if (!destination || source.index === destination.index) return;

        setSelectedPosts((current) => {
            const reordered = [...current];
            const [moved] = reordered.splice(source.index, 1);
            reordered.splice(destination.index, 0, moved);
            return reordered;
        });
    };

    const handleSubmit = async (event) => {
        event.preventDefault();
        const normalizedTitle = title.trim();
        if (!normalizedTitle || isSubmitting) return;

        setIsSubmitting(true);
        setError('');

        try {
            const { data } = await playListApi.postApiPlayListCreate({
                Title: normalizedTitle,
                Thumbnail: thumbnail ?? undefined,
                PostIds: selectedPosts.map((post) => post.id),
                ContentType: contentType,
                Kind: PlayListKindModel.NUMBER_0,
            });
            const playlistId = data.playList?.id;
            if (!playlistId) throw new Error('Playlist id is missing');
            navigate(`/playlist/${playlistId}`);
        } catch {
            setError('Не удалось создать плейлист. Проверьте данные и повторите попытку.');
        } finally {
            setIsSubmitting(false);
        }
    };

    const remainingPosts = availablePosts.filter((post) => !selectedIds.has(post.id));

    return (
        <main className={styles.page}>
            <div className={styles.shell}>
                <button className={styles.backButton} type="button" onClick={() => navigate('/profile')}>
                    ← Вернуться в профиль
                </button>

                <header className={styles.hero}>
                    <span className={styles.eyebrow}>Новый плейлист</span>
                    <h1>Соберите публикации в одном месте</h1>
                    <p>Выберите формат, добавьте свои материалы и расставьте их в нужном порядке.</p>
                </header>

                <form className={styles.workspace} onSubmit={handleSubmit}>
                    <section className={styles.settingsCard}>
                        <div className={styles.sectionHeading}>
                            <span>01</span>
                            <div>
                                <h2>Основные настройки</h2>
                                <p>Название и формат плейлиста.</p>
                            </div>
                        </div>

                        <label className={styles.titleField}>
                            <span>Название</span>
                            <input
                                value={title}
                                onChange={(event) => setTitle(event.target.value)}
                                maxLength={120}
                                placeholder="Например, Разработка продукта с нуля"
                                autoFocus
                                required
                            />
                            <small>{title.length}/120</small>
                        </label>

                        <div className={styles.coverField}>
                            <div className={styles.coverHeading}>
                                <div>
                                    <strong>Обложка</strong>
                                    <span>Необязательно · JPG, PNG или WebP до 10 МБ</span>
                                </div>
                                {thumbnail && <button type="button" onClick={removeThumbnail}>Удалить</button>}
                            </div>
                            <label className={`${styles.coverUpload} ${thumbnailPreview ? styles.coverUploadFilled : ''}`}>
                                {thumbnailPreview ? (
                                    <>
                                        <img src={thumbnailPreview} alt="Предпросмотр обложки плейлиста" />
                                        <span className={styles.coverOverlay}>Заменить изображение</span>
                                    </>
                                ) : (
                                    <span className={styles.coverPlaceholder}>
                                        <i aria-hidden="true">＋</i>
                                        <strong>Загрузить изображение</strong>
                                        <small>Нажмите, чтобы выбрать файл</small>
                                    </span>
                                )}
                                <input
                                    type="file"
                                    accept="image/jpeg,image/png,image/webp"
                                    onChange={(event) => {
                                        selectThumbnail(event.currentTarget.files?.[0]);
                                        event.currentTarget.value = '';
                                    }}
                                />
                            </label>
                        </div>

                        <div className={styles.typeChooser} role="radiogroup" aria-label="Формат плейлиста">
                            {CONTENT_TYPES.map((type) => (
                                <button
                                    key={type.value}
                                    type="button"
                                    role="radio"
                                    aria-checked={contentType === type.value}
                                    className={`${styles.typeCard} ${contentType === type.value ? styles.typeCardActive : ''}`}
                                    onClick={() => selectContentType(type.value)}
                                >
                                    <span className={styles.typeIcon}>{type.icon}</span>
                                    <span>
                                        <strong>{type.title}</strong>
                                        <small>{type.description}</small>
                                    </span>
                                </button>
                            ))}
                        </div>
                    </section>

                    <section className={styles.postsCard}>
                        <div className={styles.sectionHeading}>
                            <span>02</span>
                            <div>
                                <h2>Содержимое</h2>
                                <p>В плейлисте могут находиться только {contentLabel}.</p>
                            </div>
                        </div>

                        {error && <div className={styles.errorBanner} role="alert">{error}</div>}

                        <div className={styles.columns}>
                            <div className={styles.postColumn}>
                                <div className={styles.columnHeader}>
                                    <div>
                                        <h3>Доступно</h3>
                                        <p>Ваши опубликованные {contentLabel}</p>
                                    </div>
                                    <span>{remainingPosts.length}</span>
                                </div>

                                <div className={styles.postList}>
                                    {isLoadingPosts && <div className={styles.listState}>Загружаем публикации…</div>}
                                    {!isLoadingPosts && remainingPosts.map((post) => (
                                        <article className={styles.postRow} key={post.id}>
                                            <PostPreview post={post} contentType={contentType} />
                                            <div className={styles.postDetails}>
                                                <strong>{post.title}</strong>
                                                <span>{post.description || 'Без описания'}</span>
                                            </div>
                                            <button className={styles.addButton} type="button" onClick={() => addPost(post)} aria-label={`Добавить «${post.title}»`}>
                                                +
                                            </button>
                                        </article>
                                    ))}
                                    {!isLoadingPosts && remainingPosts.length === 0 && (
                                        <div className={styles.listState}>Нет публикаций для добавления</div>
                                    )}
                                </div>
                            </div>

                            <div className={styles.postColumn}>
                                <div className={styles.columnHeader}>
                                    <div>
                                        <h3>В плейлисте</h3>
                                        <p>Перетаскивайте, чтобы изменить порядок</p>
                                    </div>
                                    <span>{selectedPosts.length}</span>
                                </div>

                                <DragDropContext onDragEnd={onDragEnd}>
                                    <Droppable droppableId="selected-playlist-posts">
                                        {(provided) => (
                                            <div className={styles.postList} ref={provided.innerRef} {...provided.droppableProps}>
                                                {selectedPosts.map((post, index) => (
                                                    <Draggable key={post.id} draggableId={post.id} index={index}>
                                                        {(dragProvided, snapshot) => (
                                                            <article
                                                                ref={dragProvided.innerRef}
                                                                {...dragProvided.draggableProps}
                                                                {...dragProvided.dragHandleProps}
                                                                className={`${styles.postRow} ${styles.selectedPost} ${snapshot.isDragging ? styles.dragging : ''}`}
                                                            >
                                                                <span className={styles.position}>{index + 1}</span>
                                                                <PostPreview post={post} contentType={contentType} />
                                                                <div className={styles.postDetails}>
                                                                    <strong>{post.title}</strong>
                                                                    <span>Зажмите и переместите</span>
                                                                </div>
                                                                <button className={styles.removeButton} type="button" onClick={() => removePost(post.id)} aria-label={`Убрать «${post.title}»`}>
                                                                    ×
                                                                </button>
                                                            </article>
                                                        )}
                                                    </Draggable>
                                                ))}
                                                {provided.placeholder}
                                                {selectedPosts.length === 0 && (
                                                    <div className={styles.emptySelection}>
                                                        <span>＋</span>
                                                        <strong>Плейлист пока пуст</strong>
                                                        <p>Можно создать его сейчас и добавить публикации позже.</p>
                                                    </div>
                                                )}
                                            </div>
                                        )}
                                    </Droppable>
                                </DragDropContext>
                            </div>
                        </div>
                    </section>

                    <footer className={styles.actions}>
                        <div>
                            <strong>{selectedPosts.length}</strong>
                            <span>{contentLabel} выбрано</span>
                        </div>
                        <button type="button" className={styles.cancelButton} onClick={() => navigate('/profile')}>
                            Отмена
                        </button>
                        <button type="submit" className={styles.submitButton} disabled={!title.trim() || isSubmitting}>
                            {isSubmitting ? 'Создаём…' : 'Создать плейлист'}
                        </button>
                    </footer>
                </form>
            </div>
        </main>
    );
};

export default CreatePlaylistForm;
