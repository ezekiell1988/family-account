import { Injectable, computed, signal } from '@angular/core';

export type NotificationType = 'info' | 'success' | 'warning' | 'error';

export interface CampaignResult {
  campaignId?: number | string;
  summary?: string;
  success?: boolean;
}

export interface ExportResult {
  fileName?: string;
  exportedCount?: number;
  success?: boolean;
}

export interface AppNotification {
  id: string;
  type: NotificationType;
  title: string;
  message: string;
  createdAt: Date;
  read: boolean;
  progress?: number;
  actionLabel?: string;
  actionData?: CampaignResult | ExportResult | unknown;
}

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  readonly notifications = signal<AppNotification[]>([]);
  readonly unreadCount = computed(() => this.notifications().filter(notification => !notification.read).length);

  addNotification(notification: AppNotification): void {
    this.notifications.update(current => [notification, ...current].slice(0, 50));
  }

  markAllAsRead(): void {
    this.notifications.update(current => current.map(notification => ({ ...notification, read: true })));
  }

  clearAll(): void {
    this.notifications.set([]);
  }

  removeNotification(id: string): void {
    this.notifications.update(current => current.filter(notification => notification.id !== id));
  }
}