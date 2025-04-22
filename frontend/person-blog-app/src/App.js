import { BrowserRouter, Routes, Route, Navigate, Outlet, Link } from 'react-router-dom';
import { lazy, Suspense, useEffect } from 'react';
import './App.css';
import { JwtTokenService } from './scripts/TokenStrorage';
import API from './scripts/apiMethod';

// Ленивая загрузка компонентов
const MainPage = lazy(() => import('./pages/main/MainPage'));
const AuthPage = lazy(() => import('./pages/auth/AuthPage'));
const ProfilePage = lazy(() => import('./pages/profile/ProfilePage'));
const VideoPage = lazy(() => import('./pages/post/VideoPage'));
const ConferencePage = lazy(() => import('./pages/conference/ConferencePage'));
const CreatePostForm = lazy(() => import('./components/profile/post/CreatePostForm'));
const HistoryPage = lazy(() => import('./pages/history/HistoryPage'));
const Header = lazy(() => import('./components/header/Header'));

// Приватный маршрут
const PrivateRoute = ({ redirectPath = '/auth' }) => {
  const isAuthenticated = JwtTokenService.isAuth();
  return isAuthenticated ? <Outlet /> : <Navigate to={redirectPath} />;
};

// Публичный маршрут (если нужно ограничить доступ к auth)
const PublicRoute = ({ children }) => {
  const isAuthenticated = JwtTokenService.isAuth();
  return !isAuthenticated ? children : <Navigate to="/" />;
};

// Компонент проверки сессии
const Session = ({ children }) => {
  useEffect(() => {
    API.get("auth/api/Auth/session", { withCredentials: true })
      .catch(() => JwtTokenService.cleanAuth());
  }, []);

  return <>{children}</>;
};

function App() {
  return (
    <div className="App">
      <Session>
        <BrowserRouter>
          <Suspense fallback={<div className="loader">Загрузка...</div>}>
            <Header />
            <Routes>
              {/* Публичные маршруты */}
              <Route path="/" element={<MainPage />} />
              <Route path="/video/:postId" element={<VideoPage />} />
              <Route path="/auth" element={
                <PublicRoute>
                  <AuthPage />
                </PublicRoute>
              } />

              {/* Приватные маршруты */}
              <Route element={<PrivateRoute />}>
                <Route path="/profile" element={<ProfilePage />} />
                <Route path="/conference/:id" element={<ConferencePage />} />
                <Route path="/history" element={<HistoryPage />} />

                {/* Вложенные маршруты профиля */}
                <Route path="/profile" >
                  <Route index element={<ProfilePage />} />
                  <Route path="post/create" element={<CreatePostForm />} />
                  <Route path="history" element={<HistoryPage />} />
                </Route>
              </Route>

            </Routes>
          </Suspense>
        </BrowserRouter>
      </Session>
    </div>
  );
}

// Макет для вложенных маршрутов профиля
const ProfileLayout = () => {
  return (
    <div className="profile-layout">
      {/* Боковая панель профиля */}
      <aside>
        <nav>
          <ul>
            <li><Link to="/profile">Мой профиль</Link></li>
            <li><Link to="/profile/post/create">Создать пост</Link></li>
            <li><Link to="/profile/history">История просмотров</Link></li>
          </ul>
        </nav>
      </aside>
      <main>
        <Outlet />
      </main>
    </div>
  );
};

export default App;