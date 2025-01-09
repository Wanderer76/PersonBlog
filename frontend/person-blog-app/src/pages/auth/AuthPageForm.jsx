import { saveAccessToken, saveRefreshToken } from '../../scripts/TokenStrorage';
import styles from './AuthPage.module.css';
import React, { useState } from "react";
import { useNavigate } from "react-router-dom"


const AuthPageForm = function () {
    const url = 'http://localhost:7892/auth/api/Auth/login';
    const [login, setLogin] = useState("");
    const [password, setPassword] = useState("");
    const navigate = useNavigate()

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
                }
                }>
                    Войти
                </button>
            </div>
        </>)
}

export default AuthPageForm;