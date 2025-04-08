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