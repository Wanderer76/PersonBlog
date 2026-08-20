import { BrowserRouter } from 'react-router-dom';
import { AppRouter } from '@/app/router/AppRouter';
import { SessionProvider } from '@/app/providers/SessionProvider';
import '@/app/styles/app.css';

const App = () => (
  <div className="app">
    <SessionProvider>
      <BrowserRouter>
        <AppRouter />
      </BrowserRouter>
    </SessionProvider>
  </div>
);

export default App;
