import React, { useState } from "react";
import API, { BaseApUrl } from "../../scripts/apiMethod";
import { useNavigate } from "react-router-dom";
import { saveAccessToken, saveRefreshToken } from "../../scripts/TokenStrorage";

const SignInForm = ({ onSwitchToSignUp }) => {

    const [authForm, setAuthForm] = useState({
        login: "",
        password: ""
    });

    const navigate = useNavigate()

    function updateAuthForm(event) {
        const key = event.target.name;
        const value = event.target.value;

        setAuthForm((prev) => ({
            ...prev,
            [key]: value

        }))
    }

    async function sendAuthRequest(body) {
        if (body.login === "" && body.password === "") {
            return
        }
        try {
            const resonse = await API.post('/auth/api/Auth/login',
                JSON.stringify(body),
                {
                    headers: {
                        'Content-Type': 'application/json',
                    },
                }
            );

            if (resonse.status === 200) {
                const data = await resonse.data;
                saveAccessToken(data.accessToken);
                saveRefreshToken(data.refreshToken);
                navigate("/");
                window.location.reload();
            }
            else {
                console.log("error")
            }
        } catch (e) {
            console.log(e)
        }
    }

    const handleSubmit = async (e) => {
        e.preventDefault();
        await sendAuthRequest(authForm);
        // Здесь можно добавить логику для отправки данных на сервер
    };

    return (
        <form className="auth-signin auth-form" onSubmit={handleSubmit}>
            <h2 className="auth-modal-title">Войти</h2>
            <input className="auth-input"
                // type="email"
                placeholder="Логин"
                name="login"
                value={authForm.login}
                onChange={updateAuthForm}
                required
            />
            <input className="auth-input"
                type="password"
                name="password"
                placeholder="Пароль"
                value={authForm.password}
                onChange={updateAuthForm}
                required
            />
            <a className="auth-a" href="#">Забыли пароль?</a>
            <button className="auth-authButton" type="submit">Войти</button>
        </form>
    );
};

export default SignInForm;