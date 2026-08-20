import { useNavigate } from "react-router-dom";
import { PageShell } from '@/widgets/page-shell';
import { PostListItem } from '@/entities/post';
import styles from './LikedPage.module.css';
import { getLocalDate } from "@/shared/lib/date";
import { useEffect, useState } from "react";
import { getView } from "@/shared/api/generated/view/view";

const viewApi = getView();

const LikedPage = function () {
    const [likedList, setLikedList] = useState([]);
    const navigate = useNavigate();
    useEffect(() => {
        viewApi.getApiViewLiked().then(response => {
            if (response.status == 200) {
                setLikedList(response.data ?? {})
            }
        })
    }, [])

    return (
        <PageShell className={styles.pageContainer} contentClassName={styles.contentContainer}>
                <div className={styles.likedContainer}>
                    {Object.entries(likedList).map(([day, items]) => (
                        items.length > 0 && (
                            <div key={day} className={styles.dayGroup}>
                                <h2 className={styles.dayHeader}>{getLocalDate(day)}</h2>
                                <div className={styles.likedList}>
                                    {items.map(x => {
                                        var data = {
                                            postId: x.postDetail.id,
                                            previewUrl: x.postDetail.previewUrl,
                                            title: x.postDetail.title,
                                            description: x.postDetail.description,
                                            viewCount: x.postDetail.viewCount,
                                            watchTime: null,
                                            lastWatched: x.createdAt
                                        }
                                        return <PostListItem item={data} navigate={() => navigate(`/videoPage/${data.postId}?time=${data.watchTime}`)} key={x.id} />
                                    })}
                                </div>
                            </div>
                        )
                    ))}

                </div>
        </PageShell>
    );
}

export default LikedPage;
