import { Injectable, inject, computed } from '@angular/core';
import { AuthService } from '../auth/auth.service';

export type PermissionAction = 'query' | 'insert' | 'update' | 'delete';

@Injectable({ providedIn: 'root' })
export class PermissionService {
  private readonly auth = inject(AuthService);

  private readonly _permissionMap = computed(() => {
    const permissions = this.auth.currentUser()?.permissions ?? [];
    return new Map(permissions.map((p) => [p.key, p]));
  });

  hasPermission(key: string, action: PermissionAction): boolean {
    const perm = this._permissionMap().get(key);
    if (!perm) return false;
    switch (action) {
      case 'query':  return perm.isQuery;
      case 'insert': return perm.isInsert;
      case 'update': return perm.isUpdate;
      case 'delete': return perm.isDelete;
    }
  }
}
