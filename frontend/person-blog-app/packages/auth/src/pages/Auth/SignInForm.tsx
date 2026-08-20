// src/pages/Auth/SignInForm.tsx
import React, { useState, type FormEvent } from "react";
import { useSearchParams } from "react-router-dom";
import './AuthPage.css';
import { getAuth } from "../../lib/api/generated/auth/auth";
import { buildAuthRedirectUrl } from "../../utils/validation";

interface SignInFormProps {
    onSwitchToSignUp: () => void;
}

const SignInForm: React.FC<SignInFormProps> = ({ onSwitchToSignUp }) => {
    const [searchParams] = useSearchParams();
    const [formData, setFormData] = useState({ login: "", password: "" });
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const authApi = getAuth();

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError(null);

        const { login, password } = formData;

        // Валидация
        if (!login.trim() || !password.trim()) {
            setError("Введите логин и пароль");
            return;
        }

        setIsLoading(true);

        try {
            // Шаг 1: Аутентификация → получаем authCode
            const clientId = searchParams.get("client_id");
            const redirectUri = searchParams.get("redirectUri");
            const loginResponse = await authApi.postApiAuthLogin({
                login,
                password,
                clientId,
                redirectUrl: redirectUri
            });

            if (loginResponse.status !== 200 || !loginResponse.data) {
                throw new Error('Invalid login response');
            }

            const { authCode } = loginResponse.data;
            const state = searchParams.get("state");
            const redirectUrl = buildAuthRedirectUrl(redirectUri!, authCode!, state);
            window.location.href = redirectUrl;

        } catch (e: any) {
            console.error("Auth flow error:", e);

            if (e.response?.status === 401) {
                setError("Неверный логин или пароль");
            } else if (e.response?.data?.errors) {
                const errors = e.response.data.errors;
                const firstError = Object.values(errors).flat()[0] as string;
                setError(firstError || "Ошибка авторизации");
            } else {
                setError(e.message || "Ошибка соединения. Попробуйте позже.");
            }
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="auth-form-container auth-sign-in-container">
            <form className="auth-form" onSubmit={handleSubmit}>
                <h2 className="auth-modal-title">Вход через единый аккаунт</h2>

                {error && (
                    <div className="auth-error" role="alert">
                        {error}
                        <button
                            type="button"
                            className="auth-error-close"
                            onClick={() => setError(null)}
                        >
                            ×
                        </button>
                    </div>
                )}

                <div className="auth-field">
                    <label className="auth-label" htmlFor="signin-login">
                        Логин
                    </label>
                    <input
                        id="signin-login"
                        className="auth-input"
                        type="text"
                        name="login"
                        value={formData.login}
                        onChange={(e) => setFormData(prev => ({ ...prev, login: e.target.value }))}
                        required
                        disabled={isLoading}
                        autoComplete="username"
                        placeholder="Введите логин"
                    />
                </div>

                <div className="auth-field">
                    <label className="auth-label" htmlFor="signin-password">
                        Пароль
                    </label>
                    <input
                        id="signin-password"
                        className="auth-input"
                        type="password"
                        name="password"
                        value={formData.password}
                        onChange={(e) => setFormData(prev => ({ ...prev, password: e.target.value }))}
                        required
                        disabled={isLoading}
                        autoComplete="current-password"
                        placeholder="Введите пароль"
                    />
                </div>

                <a className="auth-forgot-link" href="/auth/forgot-password">
                    Забыли пароль?
                </a>

                <button
                    className="auth-submit-btn"
                    type="submit"
                    disabled={isLoading}
                >
                    {isLoading ? "Вход..." : "Войти"}
                </button>

                <div className="auth-switch">
                    <span>Нет аккаунта?</span>
                    <button
                        type="button"
                        className="auth-link-button"
                        onClick={onSwitchToSignUp}
                        disabled={isLoading}
                    >
                        Зарегистрироваться
                    </button>
                </div>

                <div className="auth-footer">
                    <small>🔒 Единый вход для всех приложений</small>
                </div>
            </form>
        </div>
    );
};

export default SignInForm;
