import { BrowserRouter, Routes, Route, Outlet } from 'react-router-dom';
import { lazy, Suspense, useEffect, type ReactNode } from 'react';
import './App.css';
import { JwtTokenService } from './shared/TokenStrorage.js';
import PlaylistPage from './pages/playlist/PlayListPage';
import CreatePlaylistForm from './components/playList/CreatePlaylistForm';
import SubscriptionPage from './pages/subscriptions/SubscriptionPage';
import ChannelPage from './pages/channel/ChannelPage';
import CreateBlogForm from './components/profile/blog/CreateBlogForm';
import LikedPage from './pages/liked/LikedPage';
import OAuthCallback from './pages/callback/OAuthCallback.js';
import CreateTextPostForm from './components/profile/post/CreateTextPostForm.js';

// Ленивая загрузка компонентов
const MainPage = lazy(() => import('./pages/main/MainPage'));

const ProfilePage = lazy(() => import('./widgets/ProfilePage/ProfilePage'));
const VideoPage = lazy(() => import('./pages/post/VideoPage'));
const ConferencePage = lazy(() => import('./pages/conference/ConferencePage'));
const CreatePostForm = lazy(() => import('./components/profile/post/CreatePostForm'));
const EditPostForm = lazy(() => import('./components/profile/post/EditPostForm'));
const HistoryPage = lazy(() => import('./pages/history/HistoryPage'));
const Header = lazy(() => import('./components/header/Header'));

// Приватный маршрут
const PrivateRoute = () => {
  const isAuthenticated = JwtTokenService.isAuth();
  return isAuthenticated ? <Outlet /> : <button onClick={async () => await JwtTokenService.redirectToAuth(window.location.origin)} />;
};
// Публичный маршрут (если нужно ограничить доступ к auth)
// Публичный маршрут
// Компонент проверки сессии
interface SessionProps {
  children: ReactNode;
}

const Session = ({ children }: SessionProps) => {
  useEffect(() => {
    if (JwtTokenService.isAuth()) {
      navigator.serviceWorker?.controller?.postMessage({
        type: 'UPLOAD_ALL_CHUNKS'
      });
    }
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
              <Route path="/videoPage/:postId" element={<VideoPage />} />
              <Route path="/channel/:channelId" element={<ChannelPage />} />
                  <Route path="/callback" element={<OAuthCallback />} />                

              {/* Приватные маршруты */}
              <Route element={<PrivateRoute />}>
                <Route path="/conference/:id" element={<ConferencePage />} />
                <Route path="/history" element={<HistoryPage />} />
                <Route path="/subscriptions" element={<SubscriptionPage />} />
                <Route path="/liked" element={<LikedPage />} />

                {/* Вложенные маршруты профиля */}
                <Route path="/profile" >
                  <Route index element={<ProfilePage />} />
                  <Route path="post/create" element={<CreatePostForm />} />
                  <Route path="post/edit/:id" element={<EditPostForm />} />
                  <Route path="blog/create" element={<CreateBlogForm />} />
                  <Route path="history" element={<HistoryPage />} />
                  <Route path="playList/create" element={<CreatePlaylistForm />} />
                  <Route path="textPost/create" element={<CreateTextPostForm />} />
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

export default App;
