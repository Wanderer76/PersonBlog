import { BrowserRouter } from 'react-router-dom';
import { AppRouter } from '@/app/router/AppRouter';
import { SessionProvider } from '@/app/providers/SessionProvider';
import { NotificationProvider } from '@/app/providers/NotificationProvider';
import '@/app/styles/app.css';

const App = () => (
  <div className="app">
    <SessionProvider>
      <BrowserRouter>
        <NotificationProvider>
          <AppRouter />
        </NotificationProvider>
      </BrowserRouter>
    </SessionProvider>
  </div>
);

export default App;
