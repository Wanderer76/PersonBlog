import { useEffect, useState } from "react";
import API from "../../scripts/apiMethod";
import { Link } from "react-router-dom";
import styles from "./SubscriptionPage.module.css";
import SideBar from "../../components/sidebar/SideBar";

const SubscriptionPage = function () {
    const [subscriptions, setSubscriptions] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [pagination, setPagination] = useState({
        page: 1,
        size: 20,
        totalPages: 1,
        totalCount: 0
    });

    useEffect(() => {
        const fetchSubscriptions = async () => {
            try {
                setLoading(true);
                const response = await API.get(`video/api/Subscriber/subscriptions?page=${pagination.page}&size=${pagination.size}`);

                setSubscriptions(response.data.items);
                setPagination(prev => ({
                    ...prev,
                    totalPages: response.data.totalPageCount,
                    totalCount: response.data.totalPostsCount
                }));
            } catch (err) {
                setError("Не удалось загрузить подписки. Попробуйте позже.");
                console.error("Error fetching subscriptions:", err);
            } finally {
                setLoading(false);
            }
        };

        fetchSubscriptions();
    }, [pagination.page, pagination.size]);

    const handlePageChange = (newPage) => {
        if (newPage > 0 && newPage <= pagination.totalPages) {
            setPagination(prev => ({ ...prev, page: newPage }));
        }
    };

    if (loading) {
        return (
            <div className={styles.container}>
                <div className={styles.loading}>Загрузка подписок...</div>
            </div>
        );
    }

    if (error) {
        return (
            <div className={styles.container}>
                <div className={styles.error}>{error}</div>
            </div>
        );
    }

    return (
        <div className={styles.container}>
            <SideBar />
            <div className={styles.content}>
                <h1 className={styles.title}>Мои подписки</h1>
                <p className={styles.count}>Всего подписок: {pagination.totalCount}</p>

                {subscriptions.length === 0 ? (
                    <div className={styles.empty}>
                        <p>У вас пока нет подписок</p>
                        <Link to="/channels" className={styles.exploreButton}>Найти каналы</Link>
                    </div>
                ) : (
                    <>
                        <div className={styles.grid}>
                            {subscriptions.map(channel => (
                                <Link
                                    to={`/channel/${channel.profileId}`}
                                    key={channel.id}
                                    className={styles.card}
                                >
                                    <div className={styles.avatarContainer}>
                                        <div className={styles.avatar}>
                                            {channel.photoUrl ? (
                                                <img src={channel.photoUrl} alt={channel.name} />
                                            ) : (
                                                channel.name.charAt(0).toUpperCase()
                                            )}
                                        </div>
                                        <div className={styles.info}>
                                            <h3 className={styles.channelName}>{channel.name}</h3>
                                            <p className={styles.subscribers}>
                                                {channel.subscribersCount} подписчиков
                                            </p>
                                            <p className={styles.description}>
                                                {channel.description || "Нет описания"}
                                            </p>
                                        </div>
                                    </div>
                                </Link>
                            ))}
                        </div>

                        {pagination.totalPages > 1 && (
                            <div className={styles.pagination}>
                                <button
                                    onClick={() => handlePageChange(pagination.page - 1)}
                                    disabled={pagination.page === 1}
                                >
                                    Назад
                                </button>
                                <span>Страница {pagination.page} из {pagination.totalPages}</span>
                                <button
                                    onClick={() => handlePageChange(pagination.page + 1)}
                                    disabled={pagination.page === pagination.totalPages}
                                >
                                    Вперед
                                </button>
                            </div>
                        )}
                    </>
                )}
            </div>
        </div>
    );
};

export default SubscriptionPage;