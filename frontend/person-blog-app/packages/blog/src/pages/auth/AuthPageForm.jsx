import './AuthPage.css';
import React, { useState } from "react";
import SignInForm from './SignInForm';
import SignUpForm from './SignUpForm';


const AuthPageForm = function () {

    const [isSignUp, setIsSignUp] = useState(false);

    const handleSwitchToSignUp = () => {
        setIsSignUp(true);
    };

    const handleSwitchToSignIn = () => {
        setIsSignUp(false);
    };

    return (
        <>
            <div className={`auth-container ${isSignUp ? "right-panel-active" : ""}`}>
                <div className="auth-form-container auth-sign-up-container">
                    <SignUpForm onSwitchToSignIn={handleSwitchToSignIn} />
                </div>
                <div className="auth-form-container auth-sign-in-container">
                    <SignInForm onSwitchToSignUp={handleSwitchToSignUp} />
                </div>
                <div className="auth-overlay-container">
                    <div className="auth-overlay">
                        <div className="auth-overlay-panel auth-overlay-left">
                            <h1>Уже есть аккаунт?</h1>
                            <p>Войдите, чтобы продолжить.</p>
                            <button id="signIn" className="auth-authButton ghost" onClick={handleSwitchToSignIn}>
                                Войти
                            </button>
                        </div>
                        <div className="auth-overlay-panel auth-overlay-right">
                            <h1>Нет аккаунта?</h1>
                            <p>Зарегистрируйтесь, чтобы начать.</p>
                            <button id='signUp' className="auth-authButton ghost" onClick={handleSwitchToSignUp}>
                                Зарегистрироваться
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </>);
}

export default AuthPageForm;