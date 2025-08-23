import { BrowserRouter, Navigate, Outlet, Route, Routes } from 'react-router-dom'
import './App.css'
import AdminDashboard from './pages/dashboard/AdminDashboard'
import LoginPage from './pages/login/LoginPage'
import Header from './components/header/Header'
import { JwtTokenService } from './scripts/TokenStrorage'

const PrivateRoute = ({ redirectPath = '/auth' }) => {
  const isAuthenticated = JwtTokenService.isAuth();
  return isAuthenticated ? <Outlet /> : <Navigate to={redirectPath} />;
};

function App() {
  return (
    <div className="App">
      <BrowserRouter>
        <Header />
        <Routes>
          <Route path="/auth" element={<LoginPage />} />
          <Route path="/" element={<Navigate to="/admin" replace />} />
          <Route element={<PrivateRoute />}>
            <Route path="/admin" element={<AdminDashboard />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </div>
  )
}

export default App
