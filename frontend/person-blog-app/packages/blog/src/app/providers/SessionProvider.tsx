import { type PropsWithChildren } from 'react';

export const SessionProvider = ({ children }: PropsWithChildren) => {
  // Worker configuration and reconnect/auth lifecycle are managed in main.tsx.
  return children;
};
