import React, { useState } from "react";
import { BaseApUrl } from "../../scripts/apiMethod";
import { saveAccessToken, saveRefreshToken } from "../../scripts/TokenStrorage";
import { useNavigate } from "react-router-dom";

const SignUpForm = ({ onSwitchToSignIn }) => {

  const [registerForm, setRegisterForm] = useState({
    login: "",
    password: "",
    passwordConfirm: "",
    name: null,
    surname: null,
    lastName: null,
    birthdate: null,
    email: null
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
      const url = BaseApUrl + '/auth/api/Auth/create';
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
    <form className="auth-signup auth-form" onSubmit={handleSubmit}>
      <h2 className="auth-modal-title">Создать аккаунт</h2>
      <input className="auth-input"
        type="text"
        placeholder="Юзернейм"
        value={registerForm.login}
        name="login"
        onChange={updateRegisterForm}
        required
      />
      <input className="auth-input"
        type="email"
        placeholder="Почта"
        value={registerForm.email}
        name="email"
        onChange={updateRegisterForm}
        required
      />
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
        placeholder="Имя (необязательно)"
        value={registerForm.name}
        name="name"
        onChange={updateRegisterForm}
      />
      <input className="auth-input"
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
      />
      <input className="auth-input"
        type="date"
        placeholder="Дата рождения (необязательно)"
        value={registerForm.birthdate}
        name="birthdate"
        onChange={updateRegisterForm}
      />
      <button className="auth-authButton" type="submit">Зарегистрироваться</button>
    </form>
  );
};

export default SignUpForm;