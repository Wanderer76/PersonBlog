import { BrowserRouter, Routes, Route, Navigate, Outlet, Link } from 'react-router-dom';
import { lazy, Suspense, useEffect } from 'react';
import './App.css';
import { getAccessToken, JwtTokenService } from './scripts/TokenStrorage';
import PlaylistPage from './pages/playlist/PlayListPage';
import CreatePlaylistForm from './components/playList/CreatePlaylistForm';
import SubscriptionPage from './pages/subscriptions/SubscriptionPage';
import ChannelPage from './pages/channel/ChannelPage';
import { deleteChunk } from './serviceWorker/IndexedDB';

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
    // слушаем сообщения от Service Worker


    // при загрузке страницы триггерим догрузку всех чанков


    // при загрузке страницы триггерим догрузку всех чанков
    navigator.serviceWorker?.controller?.postMessage({
      type: 'UPLOAD_ALL_CHUNKS'
    });
    // API.get("video/api/Auth/session", { withCredentials: true })
    //   .then(response => {
    //     setSession(response.data)
    //   })
    //   .catch(() => JwtTokenService.cleanAuth());
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
              <Route path="/channel/:channelId" element={<ChannelPage />} />
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
                <Route path="/subscriptions" element={<SubscriptionPage />} />

                {/* Вложенные маршруты профиля */}
                <Route path="/profile" >
                  <Route index element={<ProfilePage />} />
                  <Route path="post/create" element={<CreatePostForm />} />
                  <Route path="history" element={<HistoryPage />} />
                  <Route path="playList/create" element={<CreatePlaylistForm />} />
                </Route>
              </Route>
              <Route path='playlist/:playlistId' element={<PlaylistPage />} />

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