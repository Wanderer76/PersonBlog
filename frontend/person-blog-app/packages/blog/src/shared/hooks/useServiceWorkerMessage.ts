import { useEffect } from 'react';
import { isServiceWorkerResponse, type ServiceWorkerResponse } from '@/shared/lib/service-worker/messages';

type MessageHandler = (message: ServiceWorkerResponse) => void;

export const useServiceWorkerMessage = (
  handler: MessageHandler,
  deps: React.DependencyList = []
) => {
  useEffect(() => {
    const listener = (event: MessageEvent<unknown>) => {
      if (isServiceWorkerResponse(event.data)) handler(event.data);
    };

    navigator.serviceWorker?.addEventListener('message', listener);
    
    return () => {
      navigator.serviceWorker?.removeEventListener('message', listener);
    };
  }, deps); // eslint-disable-line react-hooks/exhaustive-deps
};
