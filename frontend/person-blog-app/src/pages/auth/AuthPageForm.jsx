import { JwtTokenService, saveAccessToken, saveRefreshToken } from '../../scripts/TokenStrorage';
import styles from './AuthPage.module.css';
import React, { useEffect, useState } from "react";


const AuthPageForm = function () {
    const url = 'http://localhost:7892/auth/api/Auth/login';
    const [login, setLogin] = useState("");
    const [password, setPassword] = useState("");

    async function sendAuthRequest(url, body) {
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
        }
        else {
            console.log("error")
        }
    }

    return (
        <>
            <div className={styles.modalBody}>
                <label>Войти</label>
                <br />

                <input className={styles.modalContent} placeholder="login" value={login}
                    onChange={(e) => setLogin(e.target.value)} />

                <input className={styles.modalContent} placeholder="password" type="password" autoSave='false' value={password}
                    onChange={(e) => setPassword(e.target.value)} />

                <button className={styles.modalContent} onClick={() => {
                    sendAuthRequest(url, { login, password })
                    JwtTokenService.refreshToken();
                }
                }>
                    Войти
                </button>
            </div>
        </>)
}

export default AuthPageForm;