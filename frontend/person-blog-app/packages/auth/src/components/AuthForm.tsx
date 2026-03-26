// src/pages/Auth/components/AuthForm.tsx
import { useAuthForm } from '../../../hooks/useAuthForm';
import styles from '../AuthPage.module.css';


type AuthMode = 'login' | 'register';

interface AuthFormProps {
  mode: AuthMode;
  onSubmit: (any : any) => Promise<void>;
}

export const AuthForm: React.FC<AuthFormProps> = ({ mode, onSubmit }) => {
  const {
    formData,
    errors,
    isLoading,
    showPassword,
    handleChange,
    handleSubmit,
    setShowPassword,
  } = useAuthForm({ mode, onSubmit });

  return (
    <form className={styles.form} onSubmit={handleSubmit} noValidate>
      {mode === 'register' && (
        <div className={styles.formRow}>
          <div className={styles.formGroup}>
            <label htmlFor="userName" className={styles.label}>
              Имя *
            </label>
            <input
              id="userName"
              name="userName"
              type="text"
              value={formData.userName}
              onChange={handleChange}
              className={`${styles.input} ${errors.userName ? styles.error : ''}`}
              placeholder="Ваше имя"
              disabled={isLoading}
            />
            {errors.userName && (
              <span className={styles.errorMessage}>{errors.userName}</span>
            )}
          </div>
        </div>
      )}

      <div className={styles.formGroup}>
        <label htmlFor="login" className={styles.label}>
          {mode === 'register' ? 'Логин *' : 'Логин или email *'}
        </label>
        <input
          id="login"
          name="login"
          type="text"
          value={formData.login}
          onChange={handleChange}
          className={`${styles.input} ${errors.login ? styles.error : ''}`}
          placeholder={mode === 'register' ? 'Придумайте логин' : 'Введите логин'}
          disabled={isLoading}
          autoComplete={mode === 'login' ? 'username' : 'new-username'}
        />
        {errors.login && <span className={styles.errorMessage}>{errors.login}</span>}
      </div>

      {mode === 'register' && (
        <div className={styles.formGroup}>
          <label htmlFor="email" className={styles.label}>
            Email *
          </label>
          <input
            id="email"
            name="email"
            type="email"
            value={formData.email}
            onChange={handleChange}
            className={`${styles.input} ${errors.email ? styles.error : ''}`}
            placeholder="your@email.com"
            disabled={isLoading}
            autoComplete="email"
          />
          {errors.email && <span className={styles.errorMessage}>{errors.email}</span>}
        </div>
      )}

      <div className={styles.formGroup}>
        <label htmlFor="password" className={styles.label}>
          Пароль *
        </label>
        <div className={styles.passwordWrapper}>
          <input
            id="password"
            name="password"
            type={showPassword ? 'text' : 'password'}
            value={formData.password}
            onChange={handleChange}
            className={`${styles.input} ${errors.password ? styles.error : ''}`}
            placeholder="••••••••"
            disabled={isLoading}
            autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
          />
          <button
            type="button"
            className={styles.passwordToggle}
            onClick={() => setShowPassword(!showPassword)}
            tabIndex={-1}
            aria-label={showPassword ? 'Скрыть пароль' : 'Показать пароль'}
          >
            {showPassword ? '🙈' : '👁️'}
          </button>
        </div>
        {errors.password && <span className={styles.errorMessage}>{errors.password}</span>}
      </div>

      {mode === 'register' && (
        <div className={styles.formGroup}>
          <label htmlFor="passwordConfirm" className={styles.label}>
            Подтвердите пароль *
          </label>
          <input
            id="passwordConfirm"
            name="passwordConfirm"
            type="password"
            value={formData.passwordConfirm}
            onChange={handleChange}
            className={`${styles.input} ${errors.passwordConfirm ? styles.error : ''}`}
            placeholder="••••••••"
            disabled={isLoading}
            autoComplete="new-password"
          />
          {errors.passwordConfirm && (
            <span className={styles.errorMessage}>{errors.passwordConfirm}</span>
          )}
        </div>
      )}

      {mode === 'login' && (
        <div className={styles.formActions}>
          <label className={styles.checkbox}>
            <input
              type="checkbox"
              name="rememberMe"
              checked={formData.rememberMe}
              onChange={handleChange}
              disabled={isLoading}
            />
            <span>Запомнить меня</span>
          </label>
          <button type="button" className={styles.forgotLink}>
            Забыли пароль?
          </button>
        </div>
      )}

      <button 
        type="submit" 
        className={styles.submitButton}
        disabled={isLoading}
      >
        {isLoading ? (
          <span className={styles.loader} />
        ) : mode === 'login' ? (
          'Войти'
        ) : (
          'Зарегистрироваться'
        )}
      </button>
    </form>
  );
};