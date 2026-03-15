import { Component, inject } from '@angular/core';
import { NotificationService, NotificationType } from '../../../core/services/notification.service';

@Component({
  selector: 'app-notification-toast',
  standalone: true,
  templateUrl: './notification-toast.component.html',
})
export class NotificationToastComponent {
  private readonly notify = inject(NotificationService);
  readonly notifications = this.notify.notifications;

  dismiss(id: number): void {
    this.notify.dismiss(id);
  }

  toastClasses(type: NotificationType): string {
    const base = 'bg-white border';
    const map: Record<NotificationType, string> = {
      success: 'border-emerald-200',
      error:   'border-red-200',
      warning: 'border-amber-200',
    };
    return `${base} ${map[type]}`;
  }
}
