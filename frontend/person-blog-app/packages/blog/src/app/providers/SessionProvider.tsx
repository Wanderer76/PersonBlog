import { useEffect, type PropsWithChildren } from 'react';
import { postServiceWorkerMessage } from '@/shared/lib/service-worker/messages';
import { getAccessToken, JwtTokenService } from '@/shared/auth';

export const SessionProvider = ({ children }: PropsWithChildren) => {
  useEffect(() => {
    if (!JwtTokenService.isAuth()) return;
    postServiceWorkerMessage({
      type: 'CONFIGURE',
      payload: {
        apiBaseUrl: import.meta.env.VITE_API_BASE_URL || window.location.origin,
        authToken: getAccessToken(),
      },
    });
    postServiceWorkerMessage({ type: 'UPLOAD_ALL_CHUNKS' });
  }, []);

  return children;
};
