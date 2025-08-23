import { useState } from "react";
import { useNavigate } from "react-router-dom";
import styles from "./LoginPage.module.css";
import API, { AuthUrl } from "../../scripts/apiMethod";
import { JwtTokenService, saveAccessToken, saveRefreshToken } from "../../scripts/TokenStrorage";

type LoginResponse = {
    accessToken?: string;
    refreshToken?: string;
};

async function apiLogin(payload: { login: string; password: string }): Promise<LoginResponse> {
    const res = await API.post(`${AuthUrl}/video/api/Auth/login`, payload);
    if (res.status != 200) {
        const text = await res.data.catch(() => "");
        throw new Error(text || "Неверный логин или пароль");
    }
    return res.data;
}

export default function LoginPage() {
    const [login, setLogin] = useState<string>("");
    const [password, setPassword] = useState<string>("");
    const [submitting, setSubmitting] = useState<boolean>(false);
    const [error, setError] = useState<string>("");
    const navigate = useNavigate();

    const canSubmit = login.trim().length > 0 && password.length > 0 && !submitting;

    async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();
        setError("");
        setSubmitting(true);
        try {
            const data = await apiLogin({ login, password });
            if (data.accessToken && data.refreshToken) {
                // Демонстрационно. В проде лучше httpOnly cookie.
                saveAccessToken(data.accessToken);
                saveRefreshToken(data.refreshToken);
            }
            navigate("/admin", { replace: true });
        } catch (err: unknown) {
            setError(err instanceof Error ? err.message : "Ошибка авторизации");
        } finally {
            setSubmitting(false);
        }
    }

    return (
        <div className={styles.container}>
            <div className={styles.requestCard}>
                <div className={styles.requestHeader}>
                    <div className={styles.requestHeaderContent}>
                        <h1 className={styles.title}>
                            <span className={styles.titleIcon} aria-hidden>🔐</span>
                            Авторизация
                        </h1>
                    </div>
                </div>

                <div className={styles.requestBody}>
                    {error && (
                        <div className={styles.alert} role="alert">
                            {error}
                        </div>
                    )}

                    <form onSubmit={handleSubmit}>
                        <label className={styles.formLabel} htmlFor="login">Логин</label>
                        <input
                            id="login"
                            type="text"
                            value={login}
                            onChange={(e) => setLogin(e.target.value)}
                            placeholder="Введите логин"
                            className={styles.input}
                            autoComplete="username"
                        />

                        <label className={styles.formLabel} htmlFor="password">Пароль</label>
                        <input
                            id="password"
                            type="password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            placeholder="Введите пароль"
                            className={styles.input}
                            autoComplete="current-password"
                        />

                        <div className={styles.requestActions}>
                            <div className={styles.actionsGroup}>
                                <button
                                    type="submit"
                                    disabled={!canSubmit}
                                    className={styles.buttonPrimary}
                                >
                                    {submitting ? "Входим..." : "Войти"}
                                </button>
                                <button
                                    type="button"
                                    onClick={() => { setLogin(""); setPassword(""); setError(""); }}
                                    className={styles.buttonSecondary}
                                >
                                    Сбросить
                                </button>
                            </div>
                        </div>
                    </form>

                </div>
            </div>
        </div>
    );
}
