import { BrowserRouter, Route, Routes } from 'react-router-dom';
import './App.css';
import AuthPage from './pages/auth/AuthPage';
import { MainPage } from './pages/main/MainPage';
import { Header } from './components/header/Header';
import ProfilePage from './pages/profile/ProfilePage';
import { VideoPage } from './pages/post/PostPage';




function App() {
  return (
    <div className="App">
      <Header />
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<MainPage />} />
          <Route path="/video/:postId/:videoId" element={<VideoPage />} />
          <Route path="/auth" element={<AuthPage />} />
          <Route path="/profile" element={<ProfilePage />} />
        </Routes>
      </BrowserRouter>
    </div>
  );
}

export default App;
