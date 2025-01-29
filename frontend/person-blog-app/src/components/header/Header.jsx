import React, { useEffect } from "react";
import './Header.css';
import { JwtTokenService } from "../../scripts/TokenStrorage";


export class Header extends React.Component {

    constructor(props) {
        super(props);
        this.state = { isAuth: false }
    }

    render() {
        return (
            <nav className="navbar">
                <a href='/' className="left">Главная</a>
                {!JwtTokenService.isAuth() && <a href='/auth' className="right">Авторизация</a>}
                {JwtTokenService.isAuth() && <a href='/profile' className="right">Профиль</a>}
            </nav>
        );
    }
}