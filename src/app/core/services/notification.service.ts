import { Injectable, signal } from '@angular/core';

export type NotificationType = 'success' | 'error' | 'warning';

export interface Notification {
  id: number;
  type: NotificationType;
  message: string;
}

let _nextId = 0;

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly _notifications = signal<Notification[]>([]);
  readonly notifications = this._notifications.asReadonly();

  success(message: string, duration = 3000): void {
    this._add('success', message, duration);
  }

  error(message: string, duration = 5000): void {
    this._add('error', message, duration);
  }

  warning(message: string, duration = 4000): void {
    this._add('warning', message, duration);
  }

  dismiss(id: number): void {
    this._notifications.update((list) => list.filter((n) => n.id !== id));
  }

  private _add(type: NotificationType, message: string, duration: number): void {
    const id = ++_nextId;
    this._notifications.update((list) => [...list, { id, type, message }]);
    setTimeout(() => this.dismiss(id), duration);
  }
}
