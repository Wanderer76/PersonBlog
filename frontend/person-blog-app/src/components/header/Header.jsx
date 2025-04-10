import React from "react";
import './Header.css';
import { JwtTokenService } from "../../scripts/TokenStrorage";
import { useNavigate } from "react-router-dom";


const Header = function () {
    const navigate = useNavigate();
    return (
        <nav className="navbar">
            <a className="left" href="#" onClick={() => {
                navigate('/')
            }}>Главная</a>
            {!JwtTokenService.isAuth() && <a href="#" className="right" onClick={() => {
                navigate('/auth')
            }}>Авторизация</a>}
            {JwtTokenService.isAuth() && <a className="right" href="#" onClick={() => {
                navigate('/profile')
            }}>Профиль</a>}
        </nav>
    );
}

export default Header;

// для того чтобы тема менялась и в приложении на фронте при сене темы нужно вызвать следующий метод
// window.themeBinder.changeTheme('dark')
// сейчас используются следующие константы - 'dark' и 'light'
// я использовал следующий код
// if (window.themeBinder) {
//     window.themeBinder.changeTheme('light')
// }


