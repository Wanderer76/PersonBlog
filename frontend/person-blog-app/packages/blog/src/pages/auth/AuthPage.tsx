// src/pages/AuthPage.tsx
import React, { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import SignInForm from './SignInForm';
import SignUpForm from './SignUpForm';
import './AuthPage.css';
type AuthMode = 'signin' | 'signup';

const AuthPage: React.FC = () => {
    const [mode, setMode] = useState<AuthMode>('signin');
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();

    // Обработка ошибок из параметров URL
    const error = searchParams.get('error');

    return (
        <div className="auth-page">
            <div className="auth-wrapper">
                {/* Логотип / название сервиса */}
                <div className="auth-header">
                    <h1>Единый вход</h1>
                    <p>Доступ ко всем приложениям через один аккаунт</p>
                </div>

                {/* Блок ошибок */}
                {error && (
                    <div className="auth-error-banner" role="alert">
                        {decodeURIComponent(error)}
                        <button onClick={() => navigate('/auth')}>✕</button>
                    </div>
                )}

                {/* Переключение между входом и регистрацией */}
                {mode === 'signin' ? (
                    <SignInForm onSwitchToSignUp={() => setMode('signup')} />
                ) : (
                    <SignUpForm onSwitchToSignIn={() => setMode('signin')} />
                )}

                {/* Футер с информацией */}
                <div className="auth-footer">
                    <small>
                        Защищено OAuth 2.0 • 
                        <a href="/privacy">Политика конфиденциальности</a>
                    </small>
                </div>
            </div>
        </div>
    );
};

export default AuthPage;