import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { getAccessToken } from './shared/TokenStrorage.ts'
import type { ServiceWorkerRequest } from './serviceWorker/messages.ts'

const root = createRoot(document.getElementById('root')!);
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        navigator.serviceWorker.register('/sw.js').then((reg) => {
            console.log('[SW] Registered!', reg);
            const configure = (worker: ServiceWorker | null) => {
                if (!worker) return;
                const message: ServiceWorkerRequest = {
                    type: 'CONFIGURE',
                    payload: {
                        apiBaseUrl: import.meta.env.VITE_API_BASE_URL || window.location.origin,
                        authToken: getAccessToken(),
                    },
                };
                worker.postMessage(message);
            };

            configure(navigator.serviceWorker.controller || reg.active);
            void navigator.serviceWorker.ready.then(ready => configure(ready.active));
            navigator.serviceWorker.addEventListener('controllerchange', () => {
                configure(navigator.serviceWorker.controller);
            });
        }).catch(err => {
            console.error('[SW] Registration failed:', err);
        });
    });
}

root.render(<App />)
