import { useEffect, useMemo, useState } from "react";
import API, { BaseApUrl } from "../../scripts/apiMethod";
import type { PostBanRequest, PostBanRequestsViewModel } from "../../types/BanTypes";
import styles from "./BanRequests.module.css";

export default function BanRequestsPage() {
    const [page, setPage] = useState(0);
    const size = 10;
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [totalCount, setTotalCount] = useState(0);
    const [totalPages, setTotalPages] = useState(0);
    const [currentPage, setCurrentPage] = useState(0);
    const [banRequests, setBanRequests] = useState<any[]>([]);
    const [banMessageDrafts, setBanMessageDrafts] = useState<Record<string, string>>({});
    const [expandedForms, setExpandedForms] = useState<Set<string>>(new Set());

    // Функция для получения данных о жалобах
    async function fetchBanRequests(page = 0, size = 10) {
        const url = new URL(`${BaseApUrl}/api/post/banRequest/list`);
        url.searchParams.set("page", String(page));
        url.searchParams.set("size", String(size));

        const res = await API.get(url.toString());
        if (res.status != 200) throw new Error("Не удалось загрузить список жалоб");

        const data = (await res.data) as PostBanRequestsViewModel;

        const totalCount = data.count ?? 0;
        const totalPages = Math.max(1, Math.ceil(totalCount / size));

        return {
            totalCount,
            totalPages,
            currentPage: page,
            pageSize: size,
            banRequests: data.items ?? [],
        };
    }

    // Функция для бана поста
    async function sendToBan(postId: string, message: string) {
        var body = {
            postId: postId,
            userMessage: message
        } as PostBanRequest;

        const res = await API.post(`api/post/sendToBan`, body);
        if (res.status != 200) throw new Error("Ошибка сервера при бане поста");
    }

    // Функция для восстановления поста
    async function restoreFromBan(postId: string) {
        var body = {
            postId: postId,
            userMessage: null
        } as PostBanRequest;

        const res = await API.post(`api/post/restoreFromBan`, body);
        if (res.status != 200) throw new Error("Ошибка сервера при восстановлении поста");
    }

    // Загрузка данных
    useEffect(() => {
        let cancelled = false;
        (async () => {
            try {
                setLoading(true);
                const res = await fetchBanRequests(page, size);
                if (!cancelled) {
                    setTotalCount(res.totalCount);
                    setTotalPages(res.totalPages);
                    setCurrentPage(res.currentPage);
                    setBanRequests(res.banRequests);
                    setError(null);
                }
            } catch (e: any) {
                if (!cancelled) setError(e.message ?? "Ошибка загрузки");
            } finally {
                if (!cancelled) setLoading(false);
            }
        })();
        return () => { cancelled = true; };
    }, [page, size]);

    // Функция для переключения формы бана
    const toggleBanForm = (postId: string) => {
        setExpandedForms(prev => {
            const newSet = new Set(prev);
            if (newSet.has(postId)) {
                newSet.delete(postId);
            } else {
                // Скрываем все другие формы
                newSet.clear();
                newSet.add(postId);
            }
            return newSet;
        });
    };

    // Функция для открытия поста в новой вкладке
    const openPostInNewTab = (postId: string) => {
        window.open(`http://localhost:3000/video/${postId}`, '_blank');
    };

    // Функция для восстановления поста
    const handleRestorePost = async (postId: string) => {
        if (!window.confirm('Вы уверены, что хотите восстановить этот пост?')) {
            return;
        }

        try {
            await restoreFromBan(postId);
            alert('Пост успешно восстановлен');
            setPage(p => p); // Обновляем список
        } catch (e: any) {
            alert('Ошибка: ' + (e.message ?? 'Неизвестная ошибка'));
        }
    };

    // Функция для подтверждения бана
    const handleConfirmBan = async (postId: string, message: string) => {
        if (!message.trim()) {
            alert("Введите причину бана");
            return;
        }

        try {
            await sendToBan(postId, message);
            alert('Пост успешно забанен');

            // Скрываем форму бана и очищаем поле сообщения
            setExpandedForms(prev => {
                const newSet = new Set(prev);
                newSet.delete(postId);
                return newSet;
            });

            setBanMessageDrafts(d => ({ ...d, [postId]: '' }));

            // Обновляем список
            setPage(p => p);
        } catch (e: any) {
            alert('Ошибка: ' + (e.message ?? 'Неизвестная ошибка'));
        }
    };

    // Генерация индексов страниц для пагинации
    const pageIndexes = useMemo(
        () => Array.from({ length: totalPages }, (_, i) => i),
        [totalPages]
    );

    if (loading) {
        return (
            <div className={styles.loading}>
                <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Загрузка...</span>
                </div>
            </div>
        );
    }

    return (
        <div className={`container-fluid ${styles.container}`}>
            <div className="row">
                <div className="col-12">
                    {/* Заголовок */}
                    <div className={styles.header}>
                        <h1 className={styles.title}>
                            <i className={`fas fa-ban ${styles.titleIcon}`}></i>
                            Управление жалобами на посты
                        </h1>
                        <div className="btn-group">
                            <button
                                className="btn btn-outline-primary"
                                onClick={() => setPage(p => p)}
                                disabled={loading}
                            >
                                <i className="fas fa-sync-alt me-1"></i>Обновить
                            </button>
                        </div>
                    </div>

                    {/* Статистика */}
                    <div className="row mb-4">
                        <div className="col-12">
                            <div className={`card ${styles.statsCard}`}>
                                <div className="card-body">
                                    <div className="row">
                                        <div className="col-md-6">
                                            <h5 className="card-title">Всего жалоб</h5>
                                            <h2 className="display-4">{totalCount}</h2>
                                        </div>
                                        <div className="col-md-6 d-flex align-items-center justify-content-end">
                                            <i className={`fas fa-exclamation-triangle ${styles.statsIcon}`}></i>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* Ошибка */}
                    {error && (
                        <div className={`alert alert-danger ${styles.alert}`}>
                            <i className="fas fa-times me-2"></i>
                            {error}
                        </div>
                    )}

                    {/* Список жалоб */}
                    {!loading && totalCount === 0 ? (
                        <div className={`alert alert-info ${styles.alert}`}>
                            <i className="fas fa-info-circle me-2"></i>Нет жалоб для обработки
                        </div>
                    ) : (
                        banRequests.map((request) => (
                            <div key={request.id} className={styles.requestCard}>
                                <div className={styles.requestHeader}>
                                    <div className={styles.requestHeaderContent}>
                                        <h6 className={styles.requestId}>
                                            <i className={`fas fa-flag ${styles.flagIcon}`}></i>
                                            Жалоба #{request.id}
                                        </h6>
                                        <span className={`badge bg-secondary ${styles.badge}`}>
                                            <i className={`fas fa-clock ${styles.clockIcon}`}></i>
                                            {new Date(request.createdAt).toLocaleString('ru-RU', {
                                                day: '2-digit',
                                                month: '2-digit',
                                                year: 'numeric',
                                                hour: '2-digit',
                                                minute: '2-digit'
                                            })}
                                        </span>
                                    </div>
                                </div>

                                <div className={styles.requestBody}>
                                    <div className="row">
                                        <div className="col-md-6">
                                            <div
                                                className={`mb-2 ${styles.redirectable}`}
                                                onClick={() => openPostInNewTab(request.postId)}
                                                title="Открыть пост"
                                            >
                                                <strong>ID поста:</strong>
                                                <code className={styles.code}>{request.postId}</code>
                                            </div>
                                            <div className="mb-2">
                                                <strong>Название:</strong>
                                                <span className="ms-1">{request.title}</span>
                                            </div>
                                            <div className="mb-2">
                                                <strong>ID причины:</strong>
                                                <span className="ms-1">{request.reasonId}</span>
                                            </div>
                                        </div>
                                        <div className="col-md-6">
                                            <div className="mb-2">
                                                <strong>Сообщение пользователя:</strong>
                                                <p className="text-muted mt-1">
                                                    {request.userMessage || 'Нет сообщения'}
                                                </p>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <div className={styles.requestActions}>
                                    <div className={styles.actionsGroup}>
                                        <button
                                            className="btn btn-danger btn-sm"
                                            onClick={() => toggleBanForm(request.postId)}
                                        >
                                            <i className="fas fa-ban me-1"></i>Забанить
                                        </button>

                                        <button
                                            className="btn btn-success btn-sm"
                                            onClick={() => handleRestorePost(request.postId)}
                                        >
                                            <i className="fas fa-undo me-1"></i>Восстановить
                                        </button>
                                    </div>

                                    {/* Форма для бана */}
                                    <div className={`${styles.banForm} ${expandedForms.has(request.postId) ? styles.banFormVisible : ''}`}>
                                        <div className="mb-2">
                                            <label className={styles.formLabel}>
                                                <i className={`fas fa-comment ${styles.commentIcon}`}></i>
                                                Причина бана:
                                            </label>
                                            <textarea
                                                className="form-control"
                                                rows={3}
                                                placeholder="Введите причину бана..."
                                                required
                                                value={banMessageDrafts[request.postId] || ''}
                                                onChange={(e) => setBanMessageDrafts(d => ({
                                                    ...d,
                                                    [request.postId]: e.target.value
                                                }))}
                                            ></textarea>
                                        </div>

                                        <div className={styles.formButtons}>
                                            <button
                                                type="button"
                                                className="btn btn-danger btn-sm"
                                                onClick={() => handleConfirmBan(request.postId, banMessageDrafts[request.postId] || '')}
                                            >
                                                <i className="fas fa-check me-1"></i>Подтвердить бан
                                            </button>
                                            <button
                                                type="button"
                                                className="btn btn-secondary btn-sm"
                                                onClick={() => toggleBanForm(request.postId)}
                                            >
                                                <i className="fas fa-times me-1"></i>Отмена
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        ))
                    )}

                    {/* Пагинация */}
                    {totalPages > 1 && (
                        <div className={styles.paginationContainer}>
                            <nav aria-label="Page navigation">
                                <ul className={`pagination ${styles.pagination}`}>
                                    {/* Previous page */}
                                    <li className={`page-item ${currentPage === 0 ? 'disabled' : ''}`}>
                                        <button
                                            className="page-link"
                                            onClick={() => setPage(Math.max(0, currentPage - 1))}
                                            disabled={currentPage === 0}
                                        >
                                            <i className="fas fa-chevron-left"></i>
                                        </button>
                                    </li>

                                    {/* Page numbers */}
                                    {pageIndexes.map(i => (
                                        <li key={i} className={`page-item ${i === currentPage ? 'active' : ''}`}>
                                            <button className="page-link" onClick={() => setPage(i)}>
                                                {i + 1}
                                            </button>
                                        </li>
                                    ))}

                                    {/* Next page */}
                                    <li className={`page-item ${currentPage === totalPages - 1 ? 'disabled' : ''}`}>
                                        <button
                                            className="page-link"
                                            onClick={() => setPage(Math.min(totalPages - 1, currentPage + 1))}
                                            disabled={currentPage === totalPages - 1}
                                        >
                                            <i className="fas fa-chevron-right"></i>
                                        </button>
                                    </li>
                                </ul>
                            </nav>

                            <div className={styles.pageInfo}>
                                Показано {banRequests.length} из {totalCount} жалоб
                            </div>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}