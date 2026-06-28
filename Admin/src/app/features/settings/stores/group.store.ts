import { Injectable, signal } from '@angular/core';
import { GroupListItem, GroupDetail, PermissionNode } from '../models/group.model';

@Injectable({ providedIn: 'root' })
export class GroupStore {
  // ─── Private writable signals ───────────────────────────────────────────
  private readonly _groups = signal<GroupListItem[]>([]);
  private readonly _loading = signal(false);
  private readonly _saving = signal(false);
  private readonly _selectedGroup = signal<GroupDetail | null>(null);
  private readonly _permissionTree = signal<PermissionNode[]>([]);
  private readonly _totalCount = signal(0);
  private readonly _totalPages = signal(1);

  // ─── Public readonly signals ─────────────────────────────────────────────
  readonly groups = this._groups.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly saving = this._saving.asReadonly();
  readonly selectedGroup = this._selectedGroup.asReadonly();
  readonly permissionTree = this._permissionTree.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly totalPages = this._totalPages.asReadonly();

  // ─── Mutations ───────────────────────────────────────────────────────────
  setGroups(groups: GroupListItem[]): void {
    this._groups.set(groups);
  }

  setLoading(loading: boolean): void {
    this._loading.set(loading);
  }

  setSaving(saving: boolean): void {
    this._saving.set(saving);
  }

  setSelectedGroup(group: GroupDetail | null): void {
    this._selectedGroup.set(group);
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
