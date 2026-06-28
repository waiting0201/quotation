import { Injectable, inject } from '@angular/core';
import { Observable, catchError, finalize, map, of } from 'rxjs';
import { GroupApiService } from '../services/group-api.service';
import { GroupStore } from '../stores/group.store';
import { NotificationService } from '../../../core/services/notification.service';
import { GroupCreateUpdate, GroupDetail } from '../models/group.model';

@Injectable({ providedIn: 'root' })
export class GroupFacade {
  private readonly api = inject(GroupApiService);
  private readonly store = inject(GroupStore);
  private readonly notification = inject(NotificationService);

  // ─── Exposed store signals ───────────────────────────────────────────────
  readonly groups = this.store.groups;
  readonly loading = this.store.loading;
  readonly saving = this.store.saving;
  readonly selectedGroup = this.store.selectedGroup;
  readonly permissionTree = this.store.permissionTree;
  readonly totalCount = this.store.totalCount;
  readonly totalPages = this.store.totalPages;

  // ─── Load groups list ────────────────────────────────────────────────────
  loadGroups(page = 1, pageSize = 20): void {
    this.store.setLoading(true);
    this.api
      .getList(page, pageSize)
      .pipe(finalize(() => this.store.setLoading(false)))
      .subscribe({
        next: (res) => {
          this.store.setGroups(res.data);
          this.store.setTotalCount(res.pagination.totalCount);
          this.store.setTotalPages(res.pagination.totalPages);
        },
        error: () => this.notification.error('載入群組列表失敗'),
      });
  }

  // ─── Load permission tree ────────────────────────────────────────────────
  loadPermissionTree(): void {
    if (this.store.permissionTree().length > 0) return; // 已載入過則略過
    this.api.getPermissionTree().subscribe({
      next: (res) => this.store.setPermissionTree(res.data),
      error: () => this.notification.error('載入權限設定失敗'),
    });
  }

  // ─── Load group detail ───────────────────────────────────────────────────
  loadGroupDetail(id: string): Observable<GroupDetail | null> {
    this.store.setLoading(true);
    return this.api.getById(id).pipe(
      map((res) => {
        this.store.setSelectedGroup(res.data);
        return res.data;
      }),
      catchError(() => {
        this.notification.error('載入群組詳情失敗');
        return of(null);
      }),
      finalize(() => this.store.setLoading(false))
    );
  }

  // ─── Create group ────────────────────────────────────────────────────────
  createGroup(dto: GroupCreateUpdate, page: number, pageSize: number): Observable<boolean> {
    this.store.setSaving(true);
    return this.api.create(dto).pipe(
      map(() => {
        this.notification.success('群組新增成功');
        this.loadGroups(page, pageSize); // 重新載入列表以取得正確的 userCount
        return true;
      }),
      catchError(() => {
        this.notification.error('新增群組失敗');
        return of(false);
      }),
      finalize(() => this.store.setSaving(false))
    );
  }

  // ─── Update group ────────────────────────────────────────────────────────
  updateGroup(id: string, dto: GroupCreateUpdate, page: number, pageSize: number): Observable<boolean> {
    this.store.setSaving(true);
    return this.api.update(id, dto).pipe(
      map(() => {
        this.notification.success('群組更新成功');
        this.loadGroups(page, pageSize); // 重新載入列表
        return true;
      }),
      catchError(() => {
        this.notification.error('更新群組失敗');
        return of(false);
      }),
      finalize(() => this.store.setSaving(false))
    );
  }

  // ─── Delete group ────────────────────────────────────────────────────────
  deleteGroup(id: string, page: number, pageSize: number): Observable<boolean> {
    this.store.setLoading(true);
    return this.api.delete(id).pipe(
      map(() => {
        this.notification.success('群組刪除成功');
        // 刪除成功後重新從 API 載入當前頁
        this.loadGroups(page, pageSize);
        return true;
      }),
      catchError((err) => {
        const msg =
          err?.error?.error?.message ?? '刪除群組失敗，請確認群組內無使用者';
        this.notification.error(msg);
        this.store.setLoading(false);
        return of(false);
      })
    );
  }

  // ─── Clear selected ──────────────────────────────────────────────────────
  clearSelectedGroup(): void {
    this.store.setSelectedGroup(null);
  }
}
