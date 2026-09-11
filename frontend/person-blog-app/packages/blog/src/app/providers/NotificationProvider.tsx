import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { type PropsWithChildren, useCallback, useEffect, useMemo, useState } from 'react';
import { notificationApi, type NotificationDelivery } from '@/shared/api/notifications';
import { getAccessToken, JwtTokenService, subscribeToAuthState } from '@/shared/auth/tokenStorage';
import { NotificationContext, NOTIFICATION_CREATED_EVENT } from '@/app/providers/notificationContext';

export const NotificationProvider = ({ children }: PropsWithChildren) => {
  const [authRevision, setAuthRevision] = useState(0);
  const [unreadCount, setUnreadCount] = useState(0);

  const refreshUnreadCount = useCallback(async () => {
    if (!JwtTokenService.isAuth()) {
      setUnreadCount(0);
      return;
    }

    try {
      setUnreadCount(await notificationApi.unreadCount());
    } catch {
      // The notifications page owns visible errors; the header stays unobtrusive.
    }
  }, []);

  const decrementUnreadCount = useCallback((amount = 1) => {
    setUnreadCount((current) => Math.max(0, current - amount));
  }, []);

  useEffect(() => subscribeToAuthState(() => setAuthRevision((revision) => revision + 1)), []);

  useEffect(() => {
    const refreshTimer = window.setTimeout(() => void refreshUnreadCount(), 0);
    if (!JwtTokenService.isAuth()) return undefined;

    const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || window.location.origin;
    const hubUrl = import.meta.env.VITE_NOTIFICATION_HUB_URL
      || `${apiBaseUrl.replace(/\/$/, '')}/hubs/notifications`;
    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => getAccessToken() || '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('NotificationCreated', (notification: NotificationDelivery) => {
      setUnreadCount((current) => current + 1);
      window.dispatchEvent(new CustomEvent<NotificationDelivery>(NOTIFICATION_CREATED_EVENT, {
        detail: notification,
      }));
    });
    connection.onreconnected(() => void refreshUnreadCount());
    void connection.start().catch(() => undefined);

    return () => {
      window.clearTimeout(refreshTimer);
      void connection.stop();
    };
  }, [authRevision, refreshUnreadCount]);

  const value = useMemo(() => ({ unreadCount, refreshUnreadCount, decrementUnreadCount }),
    [unreadCount, refreshUnreadCount, decrementUnreadCount]);

  return <NotificationContext.Provider value={value}>{children}</NotificationContext.Provider>;
};
