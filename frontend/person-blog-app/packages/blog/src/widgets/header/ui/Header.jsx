import React, { useEffect, useRef, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useProfilePreview } from '@/entities/profile/model/profilePreview';
import { JwtTokenService, subscribeToAuthState } from '@/shared/auth/tokenStorage';
import SideBar from '@/widgets/sidebar';
import { useNotifications } from '@/app/providers/notificationContext';
import './Header.css';

const Header = function () {
    const [isMenuOpen, setIsMenuOpen] = useState(false);
    const [isAccountOpen, setIsAccountOpen] = useState(false);
    const accountRef = useRef(null);
    const triggerRef = useRef(null);
    const { person, features, blog, status, reloadBlog } = useProfilePreview();
    const { pathname } = useLocation();
    const navigate = useNavigate();
    const [isAuthenticated, setIsAuthenticated] = useState(() => JwtTokenService.isAuth());
    const { unreadCount } = useNotifications();

    useEffect(() => { setIsAccountOpen(false); }, [pathname, isAuthenticated]);
    useEffect(() => {
        if (!isAccountOpen) return;
        const onPointerDown = event => { if (!accountRef.current?.contains(event.target)) setIsAccountOpen(false); };
        const onKeyDown = event => {
            if (event.key === 'Escape') { setIsAccountOpen(false); triggerRef.current?.focus(); }
        };
        document.addEventListener('pointerdown', onPointerDown);
        document.addEventListener('keydown', onKeyDown);
        return () => { document.removeEventListener('pointerdown', onPointerDown); document.removeEventListener('keydown', onKeyDown); };
    }, [isAccountOpen]);

    useEffect(() => subscribeToAuthState(() => {
        setIsAuthenticated(JwtTokenService.isAuth());
    }), []);

    useEffect(() => {
        const closeMenu = () => setIsMenuOpen(false);
        window.addEventListener('resize', closeMenu);
        return () => window.removeEventListener('resize', closeMenu);
    }, []);

    useEffect(() => {
        if (!isMenuOpen) return undefined;

        const previousOverflow = document.body.style.overflow;
        const closeOnEscape = (event) => {
            if (event.key === 'Escape') setIsMenuOpen(false);
        };

        document.body.style.overflow = 'hidden';
        window.addEventListener('keydown', closeOnEscape);

        return () => {
            document.body.style.overflow = previousOverflow;
            window.removeEventListener('keydown', closeOnEscape);
        };
    }, [isMenuOpen]);

    return (
        <header className="youtube-header">
            <div className="left-section">
                <button
                    type="button"
                    className="menu-button"
                    aria-label={isMenuOpen ? 'Закрыть меню' : 'Открыть меню'}
                    aria-expanded={isMenuOpen}
                    aria-controls="mobile-navigation"
                    onClick={() => setIsMenuOpen((isOpen) => !isOpen)}
                >
                    <span />
                    <span />
                    <span />
                </button>
                <Link to="/" className="logo" aria-label="PlayView — главная">
                    <span>PlayView</span>
                </Link>
            </div>

            {!window.location.href.includes('auth') && (
                <div className="right-section">
                    {!isAuthenticated ? (
                        <button
                            type="button"
                            onClick={async () => JwtTokenService.redirectToAuth(window.location.origin)}
                            className="auth-button"
                        >
                            Войти
                        </button>
                    ) : (
                        <>
                            {features.messagesEnabled && <Link to="/messages" className="notification-button" aria-label="Сообщения"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 3h16v14H8l-4 4V3Zm3 4v2h10V7H7Zm0 4v2h7v-2H7Z" /></svg></Link>}
                            <Link to="/notifications" className="notification-button"
                                aria-label={unreadCount > 0 ? `Уведомления: ${unreadCount} непрочитанных` : 'Уведомления'}>
                                <BellIcon />
                                {unreadCount > 0 && (
                                    <span className="notification-badge" aria-hidden="true">
                                        {unreadCount > 99 ? '99+' : unreadCount}
                                    </span>
                                )}
                            </Link>
                            <div className="account-dropdown" ref={accountRef} onBlur={event => {
                                if (!event.currentTarget.contains(event.relatedTarget)) setIsAccountOpen(false);
                            }}>
                                <button ref={triggerRef} type="button" className="account-trigger" aria-label="Меню аккаунта" aria-expanded={isAccountOpen} aria-controls="account-navigation" onClick={() => { setIsMenuOpen(false); setIsAccountOpen(value => !value); }}>
                                    <span className="account-avatar">{person.photoUrl ? <img src={person.photoUrl} alt="" /> : person.name.slice(0, 1)}</span><span aria-hidden="true">⌄</span>
                                </button>
                                {isAccountOpen && <nav className="account-panel" id="account-navigation" aria-label="Меню аккаунта">
                                    <div className="account-person"><span className="account-avatar">{person.photoUrl ? <img src={person.photoUrl} alt="" /> : person.name.slice(0, 1)}</span><div><strong>{person.name}</strong><small>@{person.username}</small></div></div>
                                    <Link to="/profile" aria-current={pathname === '/profile' ? 'page' : undefined} onClick={() => setIsAccountOpen(false)}>Личный профиль</Link>
                                    {status === 'ready' ? <Link to={blog ? '/studio' : '/profile/blog/create'} aria-current={pathname === '/studio' ? 'page' : undefined} onClick={() => setIsAccountOpen(false)}>{blog ? 'Управление блогом' : 'Создать блог'}</Link> : status === 'error' ? <button type="button" onClick={reloadBlog}>Повторить загрузку блога</button> : <span className="account-loading">Загрузка блога…</span>}
                                    <button type="button" className="account-logout" onClick={() => { setIsAccountOpen(false); JwtTokenService.cleanAuth(); navigate('/'); }}>Выйти</button>
                                </nav>}
                            </div>
                        </>
                    )}
                </div>
            )}

            {isMenuOpen && (
                <>
                    <button
                        className="menu-backdrop"
                        type="button"
                        aria-hidden="true"
                        tabIndex={-1}
                        onClick={() => setIsMenuOpen(false)}
                    />
                    <SideBar
                        variant="mobile"
                        id="mobile-navigation"
                        onNavigate={() => setIsMenuOpen(false)}
                    />
                </>
            )}
        </header>
    );
};

const BellIcon = () => (
    <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
        <path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9Zm-8 11a2 2 0 0 0 4 0h-4Z" />
    </svg>
);

export default Header;
