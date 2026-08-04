// src/pages/Auth/SignUpForm.tsx
import React, { useState, type FormEvent } from "react";
import { useSearchParams } from "react-router-dom";
import './AuthPage.css';
import type { RegisterRequest } from "../../lib/api/generated/models";
import { getAuth } from "../../lib/api/generated/auth/auth";
import { buildAuthRedirectUrl } from "../../utils/validation";

interface SignUpFormProps {
    onSwitchToSignIn: () => void;
}

const SignUpForm: React.FC<SignUpFormProps> = ({ onSwitchToSignIn }) => {
    const [searchParams] = useSearchParams();
    const [formData, setFormData] = useState<RegisterRequest>({
        login: "",
        password: "",
        passwordConfirm: "",
        userName: "",
    });
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const authApi = getAuth();

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setFormData(prev => ({ ...prev, [name]: value }));

        // Очищаем ошибку при вводе
        if (error) setError(null);
    };

    const validateForm = (): string | null => {
        const { login, password, passwordConfirm, userName } = formData;

        if (!login.trim()) return "Введите логин";
        if (login.length < 3) return "Логин должен быть не менее 3 символов";

        if (!password) return "Введите пароль";
        if (password.length < 6) return "Пароль должен быть не менее 6 символов";

        if (password !== passwordConfirm) return "Пароли не совпадают";

        if (!userName?.trim()) return "Введите имя";

        return null;
    };

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError(null);

        // Валидация
        const validationError = validateForm();
        if (validationError) {
            setError(validationError);
            return;
        }

        setIsLoading(true);

        try {
             // Валидация redirect_uri
            const redirectUri = searchParams.get("redirectUri") || searchParams.get("redirect") || '/';

            // Шаг 1: Регистрация пользователя
            const registerResponse = await authApi.postApiAuthCreate({
                login: formData.login,
                password: formData.password,
                userName: formData.userName,
                passwordConfirm: formData.passwordConfirm,
                redirectUrl: redirectUri
            });

            if (registerResponse.status !== 200 && registerResponse.status !== 201) {
                throw new Error('Invalid registration response');
            }

            const { authCode } = registerResponse.data;
            const state = searchParams.get("state");

           

            // Шаг 2: Редирект обратно в клиентское приложение с authCode
            const redirectUrl = buildAuthRedirectUrl(redirectUri, authCode!, state);
            window.location.href = redirectUrl;

        } catch (e: any) {
            console.error("Registration error:", e);

            if (e.response?.status === 400) {
                const errors = e.response.data?.errors;
                if (errors) {
                    const firstError = Object.values(errors).flat()[0] as string;
                    setError(firstError || "Ошибка регистрации");
                } else {
                    setError("Ошибка регистрации");
                }
            } else if (e.response?.status === 409) {
                setError("Пользователь с таким логином уже существует");
            } else {
                setError(e.message || "Ошибка соединения. Попробуйте позже.");
            }
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="auth-form-container auth-sign-up-container">
            <form className="auth-form auth-signup" onSubmit={handleSubmit}>
                <h2 className="auth-modal-title">Создать аккаунт</h2>

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
                    <label className="auth-label" htmlFor="signup-login">
                        Логин *
                    </label>
                    <input
                        id="signup-login"
                        className="auth-input"
                        type="text"
                        name="login"
                        value={formData.login}
                        onChange={handleChange}
                        required
                        disabled={isLoading}
                        autoComplete="username"
                        placeholder="Придумайте логин"
                    />
                </div>

                <div className="auth-field">
                    <label className="auth-label" htmlFor="signup-username">
                        Имя *
                    </label>
                    <input
                        id="signup-username"
                        className="auth-input"
                        type="text"
                        name="userName"
                        value={formData.userName || ''}
                        onChange={handleChange}
                        required
                        disabled={isLoading}
                        autoComplete="name"
                        placeholder="Ваше имя"
                    />
                </div>

                <div className="auth-field">
                    <label className="auth-label" htmlFor="signup-password">
                        Пароль *
                    </label>
                    <input
                        id="signup-password"
                        className="auth-input"
                        type="password"
                        name="password"
                        value={formData.password}
                        onChange={handleChange}
                        required
                        disabled={isLoading}
                        autoComplete="new-password"
                        placeholder="Минимум 6 символов"
                    />
                </div>

                <div className="auth-field">
                    <label className="auth-label" htmlFor="signup-password-confirm">
                        Подтвердите пароль *
                    </label>
                    <input
                        id="signup-password-confirm"
                        className="auth-input"
                        type="password"
                        name="passwordConfirm"
                        value={formData.passwordConfirm}
                        onChange={handleChange}
                        required
                        disabled={isLoading}
                        autoComplete="new-password"
                        placeholder="Повторите пароль"
                    />
                </div>

                <button
                    className="auth-submit-btn"
                    type="submit"
                    disabled={isLoading}
                >
                    {isLoading ? "Регистрация..." : "Зарегистрироваться"}
                </button>

                <div className="auth-switch">
                    <span>Уже есть аккаунт?</span>
                    <button
                        type="button"
                        className="auth-link-button"
                        onClick={onSwitchToSignIn}
                        disabled={isLoading}
                    >
                        Войти
                    </button>
                </div>

                <div className="auth-footer">
                    <small>🔒 Защищено OAuth 2.0</small>
                </div>
            </form>
        </div>
    );
};

export default SignUpForm;
