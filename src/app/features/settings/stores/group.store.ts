import { Injectable, signal, computed } from '@angular/core';
import { GroupListItem, GroupDetail, PermissionNode } from '../models/group.model';

@Injectable({ providedIn: 'root' })
export class GroupStore {
  // ─── Private writable signals ───────────────────────────────────────────
  private readonly _groups = signal<GroupListItem[]>([]);
  private readonly _loading = signal(false);
  private readonly _saving = signal(false);
  private readonly _selectedGroup = signal<GroupDetail | null>(null);
  private readonly _permissionTree = signal<PermissionNode[]>([]);

  // ─── Public readonly signals ─────────────────────────────────────────────
  readonly groups = this._groups.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly saving = this._saving.asReadonly();
  readonly selectedGroup = this._selectedGroup.asReadonly();
  readonly permissionTree = this._permissionTree.asReadonly();

  // ─── Derived signals ─────────────────────────────────────────────────────
  readonly totalCount = computed(() => this._groups().length);

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

  addGroup(group: GroupListItem): void {
    this._groups.update((list) => [...list, group]);
  }

  updateGroup(updated: GroupListItem): void {
    this._groups.update((list) =>
      list.map((g) => (g.groupId === updated.groupId ? updated : g))
    );
  }

  removeGroup(groupId: string): void {
    this._groups.update((list) => list.filter((g) => g.groupId !== groupId));
  }
}
