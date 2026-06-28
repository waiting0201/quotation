import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PermissionService, PermissionAction } from '../services/permission.service';
import { NotificationService } from '../services/notification.service';

export const permissionGuard: CanActivateFn = (route) => {
  const permService = inject(PermissionService);
  const notify = inject(NotificationService);
  const router = inject(Router);

  const key: string = route.data['permissionKey'];
  const action: PermissionAction = route.data['permissionAction'] ?? 'query';

  if (permService.hasPermission(key, action)) {
    return true;
  }

  notify.error('您沒有存取此頁面的權限');
  return router.createUrlTree(['/dashboard']);
};
