import API, { BaseApUrl } from '../../scripts/apiMethod';
import { saveAccessToken, saveRefreshToken } from '../../scripts/TokenStrorage';
import './AuthPage.css';
import React, { useState } from "react";
import { useNavigate } from "react-router-dom"
import SignInForm from './SignInForm';
import SignUpForm from './SignUpForm';


const AuthPageForm = function () {
    const url = BaseApUrl + '/auth/api/Auth/login';

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

    async function sendAuthRequest(url, body) {

        if (body.login === "" && body.password === "") {
            return
        }
        try {
            const resonse = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(body)
            });

            if (resonse.status === 200) {
                const data = await resonse.json();
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

    const [isSignUp, setIsSignUp] = useState(false);

    const handleSwitchToSignUp = () => {
        setIsSignUp(true);
    };

    const handleSwitchToSignIn = () => {
        setIsSignUp(false);
    };

    return (
        <>
            <div className={`container ${isSignUp ? "right-panel-active" : ""}`}>
                <div className="form-container sign-up-container">
                    <SignUpForm onSwitchToSignIn={handleSwitchToSignIn} />
                </div>
                <div className="form-container sign-in-container">
                    <SignInForm onSwitchToSignUp={handleSwitchToSignUp} />
                </div>
                <div className="overlay-container">
                    <div className="overlay">
                        <div className="overlay-panel overlay-left">
                            <h1>Уже есть аккаунт?</h1>
                            <p>Войдите, чтобы продолжить.</p>
                            <button id="signIn" className="ghost" onClick={handleSwitchToSignIn}>
                                Войти
                            </button>
                        </div>
                        <div className="overlay-panel overlay-right">
                            <h1>Нет аккаунта?</h1>
                            <p>Зарегистрируйтесь, чтобы начать.</p>
                            <button id='signUp' className="ghost" onClick={handleSwitchToSignUp}>
                                Зарегистрироваться
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </>)
}

export default AuthPageForm;