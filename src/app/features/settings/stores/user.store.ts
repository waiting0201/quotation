import { Injectable, signal, computed } from '@angular/core';
import { UserListItem, UserDetail } from '../models/user.model';
import { PermissionNode } from '../models/group.model';

@Injectable({ providedIn: 'root' })
export class UserStore {
  // ─── Private writable signals ───────────────────────────────────────────
  private readonly _users = signal<UserListItem[]>([]);
  private readonly _loading = signal(false);
  private readonly _saving = signal(false);
  private readonly _selectedUser = signal<UserDetail | null>(null);
  private readonly _permissionTree = signal<PermissionNode[]>([]);

  // ─── Public readonly signals ─────────────────────────────────────────────
  readonly users = this._users.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly saving = this._saving.asReadonly();
  readonly selectedUser = this._selectedUser.asReadonly();
  readonly permissionTree = this._permissionTree.asReadonly();

  // ─── Derived signals ─────────────────────────────────────────────────────
  readonly totalCount = computed(() => this._users().length);

  // ─── Mutations ───────────────────────────────────────────────────────────
  setUsers(users: UserListItem[]): void {
    this._users.set(users);
  }

  setLoading(loading: boolean): void {
    this._loading.set(loading);
  }

  setSaving(saving: boolean): void {
    this._saving.set(saving);
  }

  setSelectedUser(user: UserDetail | null): void {
    this._selectedUser.set(user);
  }

  setPermissionTree(tree: PermissionNode[]): void {
    this._permissionTree.set(tree);
  }

  removeUser(userId: string): void {
    this._users.update((list) => list.filter((u) => u.userId !== userId));
  }
}
