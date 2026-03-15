import { Injectable, signal } from '@angular/core';
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
  private readonly _totalCount = signal(0);
  private readonly _totalPages = signal(1);

  // ─── Public readonly signals ─────────────────────────────────────────────
  readonly users = this._users.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly saving = this._saving.asReadonly();
  readonly selectedUser = this._selectedUser.asReadonly();
  readonly permissionTree = this._permissionTree.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly totalPages = this._totalPages.asReadonly();

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

  setTotalCount(count: number): void {
    this._totalCount.set(count);
  }

  setTotalPages(pages: number): void {
    this._totalPages.set(pages);
  }
}
