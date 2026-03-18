// src/components/auth/SignInForm.tsx
import React, { useState, FormEvent } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { getAuth } from "@/lib/api/generated/auth/auth.js";
import './AuthPage.css';
import { AuthResponse } from "@/lib/api/generated/models/authResponse.js";
import { JwtTokenService, saveAccessToken, saveRefreshToken } from "@/shared/TokenStrorage";

interface SignInFormProps {
    onSwitchToSignUp: () => void;
}

const SignInForm: React.FC<SignInFormProps> = ({ onSwitchToSignUp }) => {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();

    const [formData, setFormData] = useState({ login: "", password: "" });
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const authApi = getAuth();

    // Валидация редиректа (защита от open redirect)
    const getSafeRedirectUrl = (url: string | null): string => {
        if (!url) return '/';
        if (url.startsWith('/') && !url.startsWith('//')) return url;
        try {
            const parsed = new URL(url);
            if (parsed.origin === window.location.origin) {
                return parsed.pathname + parsed.search;
            }
        } catch { }
        return '/';
    };

    // Обработчик отправки формы
    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();

        const { login, password } = formData;
        if (!login.trim() || !password.trim()) {
            setError("Введите логин и пароль");
            return;
        }

        setIsLoading(true);
        setError(null);

        try {
            const returnUrl = getSafeRedirectUrl(searchParams.get("redirect") || searchParams.get("returnUrl"));

            // Шаг 1: Логин → получаем authCode
            const loginResponse = await authApi.postApiAuthLogin({ login, password, redirectUrl: returnUrl });

            if (loginResponse.status !== 200 || !loginResponse.data) {
                throw new Error('Invalid login response');
            }

            const loginData = loginResponse.data;

            // // Проверяем, что сервер вернул authCode
            // if (!loginData.authCode) {
            //     // Если бэкенд вернул токены сразу — сохраняем их (фолбэк)
            //     if (loginData.accessToken) {
            //         saveAccessToken(loginData.accessToken);
            //         if (loginData.refreshToken) {
            //             saveRefreshToken(loginData.refreshToken);
            //         }
            //         navigate(returnUrl, { replace: true });
            //         return;
            //     }
            //     throw new Error('Server did not return authCode or tokens');
            // }


            // Шаг 2: Обмен authCode на токены
            const tokenData = await JwtTokenService.exchangeCodeForTokens(loginData.authCode!);

            // Шаг 4: Редирект на целевую страницу
            if(JwtTokenService.isAuth())
            navigate(returnUrl, { replace: true });

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
        <div className="auth-card">
            <h2 className="auth-modal-title">Вход через единый аккаунт</h2>

            {error && (
                <div className="auth-error" role="alert">
                    {error}
                    <button type="button" className="auth-error-close" onClick={() => setError(null)}>×</button>
                </div>
            )}

            <form className="auth-form" onSubmit={handleSubmit}>
                <div className="auth-field">
                    <label className="auth-label">Логин</label>
                    <input
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
                    <label className="auth-label">Пароль</label>
                    <input
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

                <a className="auth-forgot-link" href="/auth/forgot-password">Забыли пароль?</a>

                <button className="auth-submit-btn" type="submit" disabled={isLoading}>
                    {isLoading ? "Вход..." : "Войти"}
                </button>
            </form>

            <div className="auth-switch">
                <span>Нет аккаунта? </span>
                <button type="button" className="auth-link-button" onClick={onSwitchToSignUp} disabled={isLoading}>
                    Зарегистрироваться
                </button>
            </div>

            <div className="auth-footer">
                <small>🔒 Единый вход для всех приложений</small>
            </div>
        </div>
    );
};

export default SignInForm;