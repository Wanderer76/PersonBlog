import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { JwtTokenService, subscribeToAuthState } from '../../shared/TokenStrorage.js';
import SideBar from '../sidebar/SideBar';
import './Header.css';

const Header = function () {
    const [isMenuOpen, setIsMenuOpen] = useState(false);
    const [isAuthenticated, setIsAuthenticated] = useState(() => JwtTokenService.isAuth());

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
                        <Link to="/profile" className="profile-button" aria-label="Открыть профиль">
                            <span className="avatar" aria-hidden="true">F</span>
                        </Link>
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

export default Header;
