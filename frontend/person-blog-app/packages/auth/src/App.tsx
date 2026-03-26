// src/App.tsx
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import AuthPage from './pages/Auth/AuthPage';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/auth" element={<AuthPage />} />
        <Route path="/" element={<AuthPage />} />
        {/* Другие роуты */}
      </Routes>
    </BrowserRouter>
  );
}

export default App;