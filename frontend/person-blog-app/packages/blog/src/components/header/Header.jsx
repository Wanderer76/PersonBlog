import React, { useState } from "react";
import './Header.css';
import { JwtTokenService } from "../../shared/TokenStrorage.js";
import { useNavigate } from "react-router-dom";

const Header = function () {
    const navigate = useNavigate();
    const [searchQuery, setSearchQuery] = useState('');

    const handleSearch = (e) => {
        e.preventDefault();
        if (searchQuery.trim()) {
            navigate(`/search?q=${encodeURIComponent(searchQuery)}`);
        }
    };

    return (
        <nav className="youtube-header">
            {/* Логотип YouTube */}
            <div className="left-section">
                <a href="#" onClick={(e) => { e.preventDefault(); navigate('/'); }} className="logo">
                    PlayView
                </a>
            </div>

            {/* Поисковая строка */}
            {/* <form className="search-form" onSubmit={handleSearch}>
                <input
                    type="text"
                    placeholder="Поиск"
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    className="search-input"
                />
                <button type="submit" className="search-button">
                    <svg xmlns="http://www.w3.org/2000/svg" height="20" viewBox="0 0 24 24" fill="white">
                        <path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
                    </svg>
                </button>
            </form>
 */}
            {/* Правая секция: Авторизация / Профиль */}
            {!window.location.href.includes('auth') &&
                <div className="right-section">
                    {!JwtTokenService.isAuth() ? (
                        <a href="#" onClick={async (e) => { e.preventDefault(); await JwtTokenService.redirectToAuth(window.location.origin); }} className="auth-button">
                            Войти
                        </a>
                    ) : (
                        <a href="#" onClick={(e) => { e.preventDefault(); navigate('/profile'); }} className="profile-button">
                            <div className="avatar">F</div>
                        </a>
                    )}
                </div>
            }
        </nav>
    );
};

export default Header;

// для того чтобы тема менялась и в приложении на фронте при сене темы нужно вызвать следующий метод
// window.themeBinder.changeTheme('dark')
// сейчас используются следующие константы - 'dark' и 'light'
// я использовал следующий код
// if (window.themeBinder) {
//     window.themeBinder.changeTheme('light')
// }


