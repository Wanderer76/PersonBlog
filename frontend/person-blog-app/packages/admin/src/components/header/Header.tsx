import { useNavigate, Link } from "react-router-dom";
import styles from "./Header.module.css";
import { useEffect, useState } from "react";
import { JwtTokenService } from "../../scripts/TokenStrorage";

export default function Header() {
  const [isAuth, setIsAuth] = useState<boolean>(false);
  const navigate = useNavigate();

  useEffect(() => {
    // простая проверка наличия токена
    setIsAuth(JwtTokenService.isAuth());
  }, []);

  const handleLogout = () => {
    JwtTokenService.cleanAuth()
    setIsAuth(false);
    navigate("/login", { replace: true });
  };

  return (
    <header className={styles.header}>
      <Link to="/" className={styles.logo}>
        Админка
      </Link>

      <nav className={styles.nav}>
        {isAuth ? (
          <button className={styles.buttonSecondary} onClick={handleLogout}>
            Выйти
          </button>
        ) : (
          <button
            className={styles.buttonPrimary}
            onClick={() => navigate("/login")}
          >
            Войти
          </button>
        )}
      </nav>
    </header>
  );
}
