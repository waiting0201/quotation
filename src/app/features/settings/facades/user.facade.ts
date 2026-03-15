import { Injectable, inject } from '@angular/core';
import { Observable, catchError, finalize, map, of } from 'rxjs';
import { UserApiService } from '../services/user-api.service';
import { UserStore } from '../stores/user.store';
import { GroupApiService } from '../services/group-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import {
  UserCreate,
  UserUpdate,
  UserPasswordChange,
  UserDetail,
} from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserFacade {
  private readonly api = inject(UserApiService);
  private readonly groupApi = inject(GroupApiService);
  private readonly store = inject(UserStore);
  private readonly notification = inject(NotificationService);

  // ─── Exposed store signals ───────────────────────────────────────────────
  readonly users = this.store.users;
  readonly loading = this.store.loading;
  readonly saving = this.store.saving;
  readonly selectedUser = this.store.selectedUser;
  readonly permissionTree = this.store.permissionTree;
  readonly totalCount = this.store.totalCount;
  readonly totalPages = this.store.totalPages;

  // ─── Load users list ─────────────────────────────────────────────────────
  loadUsers(page = 1, pageSize = 20, search?: string): void {
    this.store.setLoading(true);
    this.api
      .getList(page, pageSize, search)
      .pipe(finalize(() => this.store.setLoading(false)))
      .subscribe({
        next: (res) => {
          this.store.setUsers(res.data);
          this.store.setTotalCount(res.pagination.totalCount);
          this.store.setTotalPages(res.pagination.totalPages);
        },
        error: () => this.notification.error('載入使用者列表失敗'),
      });
  }

  // ─── Load permission tree ────────────────────────────────────────────────
  loadPermissionTree(): void {
    if (this.store.permissionTree().length > 0) return; // 已載入過則略過
    this.groupApi.getPermissionTree().subscribe({
      next: (res) => this.store.setPermissionTree(res.data),
      error: () => this.notification.error('載入權限設定失敗'),
    });
  }

  // ─── Load user detail ────────────────────────────────────────────────────
  loadUserDetail(id: string): Observable<UserDetail | null> {
    this.store.setLoading(true);
    return this.api.getById(id).pipe(
      map((res) => {
        this.store.setSelectedUser(res.data);
        return res.data;
      }),
      catchError(() => {
        this.notification.error('載入使用者詳情失敗');
        return of(null);
      }),
      finalize(() => this.store.setLoading(false))
    );
  }

  // ─── Create user ─────────────────────────────────────────────────────────
  createUser(dto: UserCreate, page: number, pageSize: number, search?: string): Observable<boolean> {
    this.store.setSaving(true);
    return this.api.create(dto).pipe(
      map(() => {
        this.notification.success('使用者新增成功');
        this.loadUsers(page, pageSize, search); // 重新載入列表
        return true;
      }),
      catchError((err) => {
        const msg = err?.error?.error?.message ?? '新增使用者失敗';
        this.notification.error(msg);
        return of(false);
      }),
      finalize(() => this.store.setSaving(false))
    );
  }

  // ─── Update user ─────────────────────────────────────────────────────────
  updateUser(id: string, dto: UserUpdate, page: number, pageSize: number, search?: string): Observable<boolean> {
    this.store.setSaving(true);
    return this.api.update(id, dto).pipe(
      map(() => {
        this.notification.success('使用者更新成功');
        this.loadUsers(page, pageSize, search); // 重新載入列表
        return true;
      }),
      catchError((err) => {
        const msg = err?.error?.error?.message ?? '更新使用者失敗';
        this.notification.error(msg);
        return of(false);
      }),
      finalize(() => this.store.setSaving(false))
    );
  }

  // ─── Change password ─────────────────────────────────────────────────────
  changePassword(id: string, dto: UserPasswordChange): Observable<boolean> {
    this.store.setSaving(true);
    return this.api.changePassword(id, dto).pipe(
      map(() => {
        this.notification.success('密碼變更成功');
        return true;
      }),
      catchError((err) => {
        const msg = err?.error?.error?.message ?? '密碼變更失敗';
        this.notification.error(msg);
        return of(false);
      }),
      finalize(() => this.store.setSaving(false))
    );
  }

  // ─── Delete user ─────────────────────────────────────────────────────────
  deleteUser(id: string, page: number, pageSize: number, search?: string): Observable<boolean> {
    this.store.setSaving(true);
    return this.api.delete(id).pipe(
      map(() => {
        this.notification.success('使用者刪除成功');
        // 刪除成功後重新從 API 載入當前頁
        this.loadUsers(page, pageSize, search);
        return true;
      }),
      catchError((err) => {
        const msg = err?.error?.error?.message ?? '刪除使用者失敗';
        this.notification.error(msg);
        return of(false);
      }),
      finalize(() => this.store.setSaving(false))
    );
  }

  // ─── Clear selected ──────────────────────────────────────────────────────
  clearSelectedUser(): void {
    this.store.setSelectedUser(null);
  }
}
