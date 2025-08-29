// App.tsx
import React, { useEffect } from 'react';
import { ThemeProvider, CssBaseline } from '@mui/material';
import { theme } from './theme';
import { BrowserRouter, Route, Routes, useNavigate, useSearchParams } from "react-router-dom";
import HomePage from './pages/HomePage';
import { JwtTokenService, saveRefreshToken } from './scripts/TokenStrorage';
import Header from './components/header/Header';
import TrackCreator from './components/trackCreator/TrackCreator';
import ProfilePage from './pages/profile/ProfilePage';
import Sidebar from './components/sidebar/Sidebar';
import PlaylistPage from './pages/playlist/PlaylistPage';

const Session = function ({ children }: any) {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  useEffect(() => {
    const authCode = searchParams.get("authCode");
    if (!JwtTokenService.isAuth() && authCode != null) {
      saveRefreshToken(authCode);
      JwtTokenService.refreshToken()
        .then(x => {
          if (x == 200) {
            searchParams.delete("authCode")
            navigate({ search: searchParams.toString() }, { replace: true });
          }
        })
    }
  }, [searchParams, navigate]);
  return children;
}

function App() {
  return (
    <BrowserRouter>
      <Session>
        <Header />
        <ThemeProvider theme={theme}>
          <CssBaseline />
          <div style={{ display: 'flex' }}>
            <Sidebar />
            <main style={{ flex: 1, padding: '20px', overflowY: 'auto' }}>
              <Routes>
                <Route path='/' element={<HomePage />} />
                <Route path='/profile' element={<ProfilePage />} />
                <Route path='/track/create' element={<TrackCreator />} />
                <Route path='/playlist/:id' element={<PlaylistPage />} />
              </Routes>
            </main>
          </div>
        </ThemeProvider>
      </Session >
    </BrowserRouter >
  );
}

export default App;