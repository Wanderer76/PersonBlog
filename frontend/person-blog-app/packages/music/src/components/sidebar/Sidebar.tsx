// src/components/Sidebar/Sidebar.tsx
import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import './Sidebar.css';

interface SidebarItem {
  icon: string;
  label: string;
  path: string;
}

const Sidebar: React.FC = () => {
  const location = useLocation();
  const items: SidebarItem[] = [
    { icon: '🏠', label: 'Главная', path: '/' },
    { icon: '🎵', label: 'Плейлисты', path: '/playlists' },
    { icon: '📚', label: 'Библиотека', path: '/library' },
    { icon: '🔔', label: 'Уведомления', path: '/notifications' },
    { icon: '❤️', label: 'Понравившееся', path: '/liked' },
    { icon: '📁', label: 'Мои загрузки', path: '/downloads' },
    { icon: '📷', label: 'PlayView', path: 'http://localhost:3000/' },
  ];

  return (
    <aside className="sidebar">
      <div className="sidebar-content">
        {items.map((item) => (
          <Link
            key={item.path}
            to={item.path}
            className={`sidebar-item ${location.pathname === item.path ? 'active' : ''}`}
          >
            <i>{item.icon}</i>
            <span>{item.label}</span>
          </Link>
        ))}
      </div>
    </aside>
  );
};

export default Sidebar;