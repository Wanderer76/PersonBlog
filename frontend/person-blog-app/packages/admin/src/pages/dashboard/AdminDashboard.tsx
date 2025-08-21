import { useState } from "react";
import styles from "./AdminDashboard.module.css";

import PostComplaintsPage from "../banRequests/BanRequestsPage";
// Подключишь потом аналогичные компоненты
// import BlogComplaintsPage from "./BlogComplaintsPage";
// import UserComplaintsPage from "./UserComplaintsPage";

type TabKey = "posts" | "blogs" | "users";

export default function AdminDashboard() {
  const [activeTab, setActiveTab] = useState<TabKey>("posts");

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>
        <i className={`fas fa-shield-alt ${styles.titleIcon}`} />
        Панель администратора
      </h1>

      {/* Навигация по вкладкам */}
      <ul className={`nav nav-tabs ${styles.tabs}`}>
        <li className="nav-item">
          <button
            className={`nav-link ${activeTab === "posts" ? "active" : ""}`}
            onClick={() => setActiveTab("posts")}
          >
            <i className="fas fa-flag me-1" /> Жалобы на посты
          </button>
        </li>
        <li className="nav-item">
          <button
            className={`nav-link ${activeTab === "blogs" ? "active" : ""}`}
            onClick={() => setActiveTab("blogs")}
          >
            <i className="fas fa-rss me-1" /> Жалобы на блоги
          </button>
        </li>
        <li className="nav-item">
          <button
            className={`nav-link ${activeTab === "users" ? "active" : ""}`}
            onClick={() => setActiveTab("users")}
          >
            <i className="fas fa-user me-1" /> Жалобы на пользователей
          </button>
        </li>
      </ul>

      {/* Содержимое вкладок */}
      <div className={styles.tabContent}>
        {activeTab === "posts" && <PostComplaintsPage />}
        {activeTab === "blogs" && (
          <div className="p-3">
            <h5>Жалобы на блоги</h5>
            {/* <BlogComplaintsPage /> */}
            <p>Здесь будет список жалоб на блоги.</p>
          </div>
        )}
        {activeTab === "users" && (
          <div className="p-3">
            <h5>Жалобы на пользователей</h5>
            {/* <UserComplaintsPage /> */}
            <p>Здесь будет список жалоб на пользователей.</p>
          </div>
        )}
      </div>
    </div>
  );
}
