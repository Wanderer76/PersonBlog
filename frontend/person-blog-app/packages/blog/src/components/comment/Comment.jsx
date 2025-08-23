import { useState } from "react";
import { getLocalDateTime } from "../../scripts/LocalDate";
import API from "../../scripts/apiMethod";

const Comment = ({ comment, postId, depth }) => {
    const [newCommentText, setNewCommentText] = useState('')
    const [showReply, setShowReply] = useState(false);
    const [children, setChildren] = useState(comment.children ?? [])
    const handleAddComment = async (replyId) => {
        if (!newCommentText.trim() && !replyId && !postId) return;
        try {
            const response = await API.post(`video/api/Comments/create`, {
                postId: postId,
                replyTo: replyId,
                text: newCommentText
            });

            if (response.status === 200) {
                setNewCommentText('');
                setChildren(prev => [...prev, response.data])
                setShowReply(false);
            }
        } catch (error) {
            console.error("Ошибка при добавлении комментария:", error);
        }
    };

    return (
        <div className='comment'>
            <img
                src={comment.userAvatar || "https://picsum.photos/40/40"}
                className="comment-avatar"
                alt="Аватар пользователя"
            />

            <div className="comment-content">
                <div className="comment-header">
                    <div className="comment-author">{comment.username}</div>
                    <div className="comment-date">{getLocalDateTime(comment.createdAt)}</div>
                </div>

                <div className="comment-text">{comment.text}</div>

                <div className="comment-actions">
                    {!showReply &&
                        <button className="btn btnPrimary" onClick={() => setShowReply(true)}>
                            Ответить
                        </button>
                    }
                    {showReply &&
                        <div className="comment-input-container">
                            <input
                                type="text"
                                value={newCommentText}
                                onChange={(e) => setNewCommentText(e.target.value)}
                                placeholder="Добавьте комментарий..."
                                className="comment-input"
                            />
                            <button
                                onClick={() => handleAddComment(comment.id)}
                                className="btn btnPrimary"
                                disabled={!newCommentText.trim()}>
                                Отправить
                            </button>
                            <button
                                onClick={() => { setShowReply(false); setNewCommentText('') }}
                                className="btn btnSecondary"
                            >
                                Отмена
                            </button>
                        </div>
                    }
                </div>

                {children && (
                    <div className="comment-replies">
                        {children.map(childComment => (
                            <Comment
                                key={childComment.id}
                                comment={childComment}
                                postId={postId}
                                depth={depth + 1}
                            />
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
};

// Компонент списка комментариев
const CommentsList = ({ comments, postId }) => {
    return (
        <div className="comments-list">
            {comments.length > 0 ? (
                comments.map(comment => (
                    <Comment key={comment.id} comment={comment} postId={postId} depth={0} />
                ))
            ) : (
                <div className="no-comments">Пока нет комментариев. Будьте первым!</div>
            )}
        </div>
    );
};

export default CommentsList;