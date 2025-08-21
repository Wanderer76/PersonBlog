import { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import type { BanRequest } from "../../types/BanTypes";
import API from "../../scripts/apiMethod";

export default function PostComplaintsDetailPage() {
    const { postId } = useParams<{ postId: string }>();
    const [complaints, setComplaints] = useState<BanRequest[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    async function fetchComplaintsByPost(postId: string): Promise<BanRequest[]> {
        const res = await API.get(`/api/post/banRequest/list?postId=${postId}`);
        if (res.status != 200) throw new Error("Ошибка загрузки жалоб по посту");
        return res.data;
    }
    useEffect(() => {
        (async () => {
            if (!postId) return;
            try {
                setLoading(true);
                const data = await fetchComplaintsByPost(postId);
                setComplaints(data);
            } catch (e: any) {
                setError(e.message);
            } finally {
                setLoading(false);
            }
        })();
    }, [postId]);

    if (loading) return <div className="alert alert-secondary">Загрузка...</div>;
    if (error) return <div className="alert alert-danger">{error}</div>;

    return (
        <div>
            <div className="d-flex justify-content-between align-items-center mb-3">
                <h2>Жалобы на пост {postId}</h2>
                <Link to="/admin/complaints/posts" className="btn btn-outline-secondary btn-sm">
                    ← Назад к списку
                </Link>
            </div>

            {complaints.length === 0 && (
                <div className="alert alert-info">Нет жалоб на этот пост</div>
            )}

            {complaints.map(c => (
                <div key={c.id} className="card mb-3">
                    <div className="card-header">
                        Жалоба #{c.id} — {new Date(c.createdAt).toLocaleString("ru-RU")}
                    </div>
                    <div className="card-body">
                        <p><strong>ID причины:</strong> {c.reasonId}</p>
                        <p><strong>Сообщение:</strong> {c.userMessage || "—"}</p>
                    </div>
                </div>
            ))}
        </div>
    );
}
