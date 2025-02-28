import React, { useEffect } from "react";
import './Header.css';
import { JwtTokenService } from "../../scripts/TokenStrorage";
import API from "../../scripts/apiMethod";


const Header = function () {

    useEffect(() => {
        API.get("auth/api/Auth/session",{withCredentials:true})
    }, [])
    return (
        <nav className="navbar">
            <a href='/' className="left">Главная</a>
            {!JwtTokenService.isAuth() && <a href='/auth' className="right">Авторизация</a>}
            {JwtTokenService.isAuth() && <a href='/profile' className="right">Профиль</a>}
        </nav>
    );
}

export default Header;