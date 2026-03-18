import { useEffect } from 'react';

type MessageHandler = (event: MessageEvent) => void;

export const useServiceWorkerMessage = (
  handler: MessageHandler,
  deps: React.DependencyList = []
) => {
  useEffect(() => {
    const listener = (event: MessageEvent) => {
      handler(event);
    };

    navigator.serviceWorker?.addEventListener('message', listener);
    
    return () => {
      navigator.serviceWorker?.removeEventListener('message', listener);
    };
  }, deps); // eslint-disable-line react-hooks/exhaustive-deps
};