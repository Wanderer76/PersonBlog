import { BrowserRouter, Route, Routes } from 'react-router-dom';
import './App.css';
import AuthPage from './pages/auth/AuthPage';
import { MainPage } from './pages/main/MainPage';
import Header from './components/header/Header';
import ProfilePage from './pages/profile/ProfilePage';
import { VideoPage } from './pages/post/PostPage';
import { useEffect, useState } from 'react';
import API from './scripts/apiMethod';


const Session = function ({ children }) {
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    API.get("auth/api/Auth/session", { withCredentials: true }) .then(response => {
      setLoading(false);
    })
  }, []);

  console.log(children);
  if (loading) {
    return <></>;
  }

  return <>{children}</>;
}

function App() {
  return (
    <div className="App">
      <Session>
        <BrowserRouter>
          <Header />
          <Routes>
            <Route path="/" element={<MainPage />} />
            <Route path="/video/:postId" element={<VideoPage />} />
            <Route path="/auth" element={<AuthPage />} />
            <Route path="/profile" element={<ProfilePage />} />
          </Routes>
        </BrowserRouter>
      </Session>
    </div>
  );
}

export default App;
