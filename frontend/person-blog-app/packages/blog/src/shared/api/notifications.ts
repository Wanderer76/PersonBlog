import { API } from '@/shared/api/client';

const notificationsPath = '/video/api/notifications';

export type NotificationTarget = {
  type: string;
  id: string;
};

export type NotificationItem = {
  id: string;
  kind: string;
  businessId: string;
  actorUserId?: string | null;
  target: NotificationTarget;
  templateKey: string;
  templateVersion: number;
  data: Record<string, string>;
  createdAt: string;
  readAt?: string | null;
  expiresAt?: string | null;
};

export type NotificationPage = {
  items: NotificationItem[];
  nextCursor?: string | null;
  snapshot: string;
};

export type NotificationDelivery = {
  notificationId: string;
  kind: string | number;
  businessId: string;
  actorUserId?: string | null;
  target: Omit<NotificationTarget, 'type'> & { type: string | number };
  templateKey: string;
  templateVersion: number;
  data: Record<string, string>;
  createdAt: string;
  expiresAt?: string | null;
};

export const notificationApi = {
  async list(cursor?: string | null, unreadOnly = false): Promise<NotificationPage> {
    const response = await API.get<NotificationPage>(notificationsPath, {
      params: { cursor: cursor || undefined, limit: 30, unreadOnly },
    });
    return response.data;
  },

  async unreadCount(): Promise<number> {
    const response = await API.get<{ count: number }>(`${notificationsPath}/unread-count`);
    return response.data.count;
  },

  async markRead(id: string): Promise<void> {
    await API.put(`${notificationsPath}/${id}/read`);
  },

  async markAllRead(snapshot: string): Promise<number> {
    const response = await API.put<{ count: number }>(`${notificationsPath}/read-all`, { snapshot });
    return response.data.count;
  },
};
