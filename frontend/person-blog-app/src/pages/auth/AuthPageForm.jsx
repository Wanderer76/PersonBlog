import API from '../../scripts/apiMethod';
import { saveAccessToken, saveRefreshToken } from '../../scripts/TokenStrorage';
import styles from './AuthPage.module.css';
import React, { useState } from "react";
import { useNavigate } from "react-router-dom"


const AuthPageForm = function () {
    const url = 'http://localhost:7892/auth/api/Auth/login';

    const [authForm, setAuthForm] = useState({
        login: "",
        password: ""
    });

    const navigate = useNavigate()

    function updateAuthForm(event) {
        const key = event.target.name;
        const value = event.target.value;

        setAuthForm((prev) => ({

            ...prev,
            [key]: value

        }))
    }
    console.log(authForm);


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

                <input className={styles.modalContent} placeholder="login" value={authForm.login} name='login'
                    onChange={updateAuthForm} />

                <input className={styles.modalContent} placeholder="password" name='password' type="password" autoSave='false' value={authForm.password}
                    onChange={updateAuthForm} />

                <button className={styles.modalContent} onClick={() => {
                    sendAuthRequest(url, authForm)
                }
                }>
                    Войти
                </button>
            </div>
        </>)
}

export default AuthPageForm;