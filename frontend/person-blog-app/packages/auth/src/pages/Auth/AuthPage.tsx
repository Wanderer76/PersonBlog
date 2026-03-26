// src/pages/Auth/AuthPage.tsx
import React, { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import SignInForm from './SignInForm';
import SignUpForm from './SignUpForm';
import './AuthPage.css';

const AuthPage: React.FC = () => {
  const [isSignUp, setIsSignUp] = useState(false);
  const [searchParams] = useSearchParams();
  
  // Обработка ошибок из параметров URL
  const error = searchParams.get('error');
  const errorDescription = searchParams.get('error_description');

  return (
    <div className="auth-page">
      <div className="auth-wrapper">
        {/* Логотип / название сервиса */}
        <div className="auth-header">
          <h1>Единый вход</h1>
          <p>Доступ ко всем приложениям через один аккаунт</p>
        </div>

        {/* Блок ошибок из URL */}
        {error && (
          <div className="auth-error-banner" role="alert">
            <span>{errorDescription || decodeURIComponent(error)}</span>
            <button 
              onClick={() => window.history.replaceState({}, '', '/auth')}
              aria-label="Закрыть"
            >
              ✕
            </button>
          </div>
        )}

        {/* Контейнер с анимацией переключения */}
        <div className={`auth-container ${isSignUp ? 'right-panel-active' : ''}`}>
          <SignInForm onSwitchToSignUp={() => setIsSignUp(true)} />
          <SignUpForm onSwitchToSignIn={() => setIsSignUp(false)} />
          
          {/* Overlay панель */}
          <div className="auth-overlay-container">
            <div className="auth-overlay">
              <div className="auth-overlay-panel auth-overlay-left">
                <h1>Уже есть аккаунт?</h1>
                <p>Войдите, чтобы продолжить</p>
                <button 
                  className="auth-authButton ghost" 
                  onClick={() => setIsSignUp(false)}
                >
                  Войти
                </button>
              </div>
              <div className="auth-overlay-panel auth-overlay-right">
                <h1>Нет аккаунта?</h1>
                <p>Зарегистрируйтесь, чтобы начать</p>
                <button 
                  className="auth-authButton ghost" 
                  onClick={() => setIsSignUp(true)}
                >
                  Зарегистрироваться
                </button>
              </div>
            </div>
          </div>
        </div>

        {/* Футер */}
        <div className="auth-footer-info">
          <small>
            Защищено OAuth 2.0 • 
            <a href="/privacy" target="_blank" rel="noopener noreferrer">
              Политика конфиденциальности
            </a>
          </small>
        </div>
      </div>
    </div>
  );
};

export default AuthPage;