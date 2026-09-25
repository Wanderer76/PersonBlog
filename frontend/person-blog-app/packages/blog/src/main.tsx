import { createRoot } from 'react-dom/client'
import '@/app/styles/index.css'
import App from '@/app/App'
import { JwtTokenService } from '@/shared/auth/tokenStorage'
import { isServiceWorkerResponse } from '@/shared/lib/service-worker/messages'
import { configureUploadWorker } from '@/shared/lib/upload/backgroundUpload'

const root = createRoot(document.getElementById('root')!);
if ('serviceWorker' in navigator) {
    const configure = () => {
        void configureUploadWorker(true).catch(error => console.error('[SW] Configuration failed:', error));
    };
    configure();
    window.addEventListener('online', configure);
    window.addEventListener('storage', configure);
    window.addEventListener('auth-state-changed', configure);
    navigator.serviceWorker.addEventListener('controllerchange', configure);
    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') configure();
    });
    navigator.serviceWorker.addEventListener('message', event => {
        if (isServiceWorkerResponse(event.data) && event.data.type === 'UPLOAD_AUTH_REQUIRED') {
            void JwtTokenService.refreshToken().then(status => {
                if (status === 200) configure();
            }).catch(error => console.error('[SW] Token refresh failed:', error));
        }
    });
}

root.render(<App />)
