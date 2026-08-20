import { lazy, Suspense, useEffect } from 'react';
import { Outlet, Route, Routes } from 'react-router-dom';
import { JwtTokenService } from '@/shared/auth';
import { AppHeader } from '@/widgets/header';

const MainPage = lazy(() => import('@/pages/main'));
const ProfilePage = lazy(() => import('@/pages/profile'));
const VideoPage = lazy(() => import('@/pages/video'));
const ConferencePage = lazy(() => import('@/pages/conference'));
const CreatePostPage = lazy(() => import('@/pages/post-create'));
const EditPostPage = lazy(() => import('@/pages/post-edit'));
const HistoryPage = lazy(() => import('@/pages/history'));
const PlaylistPage = lazy(() => import('@/pages/playlist'));
const CreatePlaylistPage = lazy(() => import('@/pages/playlist-create'));
const SubscriptionPage = lazy(() => import('@/pages/subscriptions'));
const ChannelPage = lazy(() => import('@/pages/channel'));
const CreateBlogPage = lazy(() => import('@/pages/blog-create'));
const LikedPage = lazy(() => import('@/pages/liked'));
const OAuthCallbackPage = lazy(() => import('@/pages/callback'));
const TextPostEditorPage = lazy(() => import('@/pages/text-post-editor'));

const PrivateRoute = () => {
  const isAuthenticated = JwtTokenService.isAuth();

  useEffect(() => {
    if (!isAuthenticated) {
      void JwtTokenService.redirectToAuth(window.location.href).catch(() => undefined);
    }
  }, [isAuthenticated]);

  return isAuthenticated ? <Outlet /> : <div className="app-loader">Перенаправление на авторизацию...</div>;
};

export const AppRouter = () => (
  <Suspense fallback={<div className="app-loader">Загрузка...</div>}>
    <AppHeader />
    <Routes>
      <Route path="/" element={<MainPage />} />
      <Route path="/videoPage/:postId" element={<VideoPage />} />
      <Route path="/channel/:channelId" element={<ChannelPage />} />
      <Route path="/callback" element={<OAuthCallbackPage />} />
      <Route path="/playlist/:playlistId" element={<PlaylistPage />} />
      <Route element={<PrivateRoute />}>
        <Route path="/conference/:id" element={<ConferencePage />} />
        <Route path="/history" element={<HistoryPage />} />
        <Route path="/subscriptions" element={<SubscriptionPage />} />
        <Route path="/liked" element={<LikedPage />} />
        <Route path="/profile">
          <Route index element={<ProfilePage />} />
          <Route path="post/create" element={<CreatePostPage />} />
          <Route path="post/edit/:id" element={<EditPostPage />} />
          <Route path="blog/create" element={<CreateBlogPage />} />
          <Route path="history" element={<HistoryPage />} />
          <Route path="playList/create" element={<CreatePlaylistPage />} />
          <Route path="textPost/create" element={<TextPostEditorPage />} />
          <Route path="textPost/edit/:id" element={<TextPostEditorPage />} />
        </Route>
      </Route>
    </Routes>
  </Suspense>
);
