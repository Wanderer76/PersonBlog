import React from "react";
import './Header.css';


export class Header extends React.Component {
    render() {
        return (
            <nav className="navbar">
                <a href='/'>Главная</a>
                <a href='/auth' className="right">Авторизация</a>
            </nav>
        )
    }
}