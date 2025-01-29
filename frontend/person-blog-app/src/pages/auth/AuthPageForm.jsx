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
                            <button id="signIn" className="authButton ghost" onClick={handleSwitchToSignIn}>
                                Войти
                            </button>
                        </div>
                        <div className="overlay-panel overlay-right">
                            <h1>Нет аккаунта?</h1>
                            <p>Зарегистрируйтесь, чтобы начать.</p>
                            <button id='signUp' className="authButton ghost" onClick={handleSwitchToSignUp}>
                                Зарегистрироваться
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </>);
}

export default AuthPageForm;