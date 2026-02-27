import React, { useState } from "react";
import { BaseApUrl } from "../../lib/api/client";
import { saveAccessToken, saveRefreshToken } from "../../shared/TokenStrorage.js";
import { useNavigate, useSearchParams } from "react-router-dom";
import { getAuth } from "@/lib/api/generated/auth/auth.js";

const SignUpForm = ({ onSwitchToSignIn }) => {

  const [searchParams] = useSearchParams();

  const [registerForm, setRegisterForm] = useState({
    login: "",
    password: "",
    passwordConfirm: "",
    userName: null,
    // surname: null,
    // lastName: null,
    // birthdate: null,
    email: null,
    redirectUrl: searchParams.get("redirect")

  });

  const navigate = useNavigate();

  const handleSubmit = (e) => {
    e.preventDefault();
    sendAuthRequest(registerForm);
    //  console.log("Регистрация:", { username, email, password, fullName, birthdate });
    // Здесь можно добавить логику для отправки данных на сервер
  };

  function updateRegisterForm(event) {
    const key = event.target.name;
    const value = event.target.value;

    setRegisterForm((prev) => ({
      ...prev,
      [key]: value
    }));
  }

  async function sendAuthRequest(body) {
    console.log(body);
    if (body.login === "" && body.password === "") {
      return
    }
    try {
      var authApi = getAuth();

      const resonse = await authApi.postApiAuthCreate(body);
      const data = resonse.data;
      saveAccessToken(data.accessToken);
      saveRefreshToken(data.refreshToken);
      if (data.authCode != null && registerForm.redirectUrl != null) {
        const loginUrl = new URL(registerForm.redirectUrl);
        loginUrl.searchParams.append('authCode', data.refreshToken);
        window.location.href = loginUrl.toString()
      }
      else {
        navigate("/");
        window.location.reload();
      }
    } catch (e) {
      console.log(e)
    }
  }

  return (
    <form className="auth-signup auth-form" onSubmit={handleSubmit}>
      <h2 className="auth-modal-title">Создать аккаунт</h2>
      <input className="auth-input"
        type="text"
        placeholder="Логин"
        value={registerForm.login}
        name="login"
        onChange={updateRegisterForm}
        required
      />
      {/* <input className="auth-input"
        type="email"
        placeholder="Почта"
        value={registerForm.email}
        name="email"
        onChange={updateRegisterForm}
        required
      /> */}
      <input className="auth-input"
        type="password"
        placeholder="Пароль"
        value={registerForm.password}
        name="password"
        onChange={updateRegisterForm}
        required
      />
      <input className="auth-input"
        type="password"
        placeholder="Повторите пароль"
        value={registerForm.passwordConfirm}
        name="passwordConfirm"
        onChange={updateRegisterForm}
        required
      />
      <input className="auth-input"
        type="text"
        placeholder="Имя"
        value={registerForm.userName}
        name="userName"
        onChange={updateRegisterForm}
      />
      {/* <input className="auth-input"
        type="text"
        placeholder="Фамилия (необязательно)"
        value={registerForm.surname}
        name="surname"
        onChange={updateRegisterForm}
      />
      <input className="auth-input"
        type="text"
        placeholder="Отчество (необязательно)"
        value={registerForm.lastName}
        name="lastname"
        onChange={updateRegisterForm}
      /> */}
      {/* <input className="auth-input"
        type="date"
        placeholder="Дата рождения (необязательно)"
        value={registerForm.birthdate}
        name="birthdate"
        onChange={updateRegisterForm}
      /> */}
      <button className="auth-authButton" type="submit">Зарегистрироваться</button>
    </form>
  );
};

export default SignUpForm;