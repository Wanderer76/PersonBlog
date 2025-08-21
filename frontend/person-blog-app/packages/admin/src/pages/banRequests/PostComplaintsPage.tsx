import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import API from "../../scripts/apiMethod";
import type { BanRequest, GroupedComplaintsResponse, GroupedPostComplaint } from "../../types/BanTypes";

export default function PostComplaintsPage() {
  const [posts, setPosts] = useState<GroupedPostComplaint[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

 async function fetchGroupedPostComplaints(): Promise<GroupedComplaintsResponse> {
  const res = await API.get(`/api/post/banRequest/grouped`);
  if (res.status != 200) throw new Error("Ошибка загрузки постов с жалобами");
  return res.data;
}

  useEffect(() => {
    (async () => {
      try {
        setLoading(true);
        const data = await fetchGroupedPostComplaints();
        setPosts(data.items);
      } catch (e: any) {
        setError(e.message);
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  if (loading) return <div className="alert alert-secondary">Загрузка...</div>;
  if (error) return <div className="alert alert-danger">{error}</div>;

  return (
    <div>
      <h2>Посты с жалобами</h2>
      {posts.length === 0 && (
        <div className="alert alert-info">Нет жалоб</div>
      )}
      <ul className="list-group">
        {posts.map(post => (
          <li
            key={post.postId}
            className="list-group-item d-flex justify-content-between align-items-center"
          >
            <div>
              <strong>{post.title}</strong> <br />
              <small className="text-muted">{post.postId}</small>
            </div>
            <div>
              <span className="badge bg-danger me-3">
                {post.complaintsCount} жалоб
              </span>
              <Link
                to={`/admin/complaints/posts/${post.postId}`}
                className="btn btn-outline-primary btn-sm"
              >
                Просмотреть
              </Link>
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}
