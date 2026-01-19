import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

var root = createRoot(document.getElementById('root')!);
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        navigator.serviceWorker.register('/sw.js').then((reg) => {
            console.log('[SW] Registered!', reg);
        }).catch(err => {
            console.error('[SW] Registration failed:', err);
        });
    });
}

root.render(<App />)
