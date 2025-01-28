import React, { useState } from "react";

const SignUpForm = ({ onSwitchToSignIn }) => {
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [fullName, setFullName] = useState("");
  const [birthdate, setBirthdate] = useState("");

  const handleSubmit = (e) => {
    e.preventDefault();
    console.log("Регистрация:", { username, email, password, fullName, birthdate });
    // Здесь можно добавить логику для отправки данных на сервер
  };

  return (
    <form className="signup" onSubmit={handleSubmit}>
      <h2 className="modal-title">Создать аккаунт</h2>
      <input
        type="text"
        placeholder="Юзернейм"
        value={username}
        onChange={(e) => setUsername(e.target.value)}
        required
      />
      <input
        type="email"
        placeholder="Почта"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        required
      />
      <input
        type="password"
        placeholder="Пароль"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        required
      />
      <input
        type="text"
        placeholder="ФИО (необязательно)"
        value={fullName}
        onChange={(e) => setFullName(e.target.value)}
      />
      <input
        type="date"
        placeholder="Дата рождения (необязательно)"
        value={birthdate}
        onChange={(e) => setBirthdate(e.target.value)}
      />
      <button type="submit">Зарегистрироваться</button>
    </form>
  );
};

export default SignUpForm;