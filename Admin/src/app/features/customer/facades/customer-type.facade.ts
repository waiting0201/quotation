import { Injectable, inject } from '@angular/core';
import { Observable, catchError, finalize, map, of } from 'rxjs';
import { CustomerTypeApiService } from '../services/customer-type-api.service';
import { CustomerTypeStore } from '../stores/customer-type.store';
import { NotificationService } from '../../../core/services/notification.service';
import { CustomerTypeCreateUpdate } from '../models/customer-type.model';

@Injectable({ providedIn: 'root' })
export class CustomerTypeFacade {
  private readonly api = inject(CustomerTypeApiService);
  private readonly store = inject(CustomerTypeStore);
  private readonly notification = inject(NotificationService);

  // ─── Exposed store signals ───────────────────────────────────────────────
  readonly types = this.store.types;
  readonly loading = this.store.loading;
  readonly saving = this.store.saving;
  readonly totalCount = this.store.totalCount;
  readonly totalPages = this.store.totalPages;

  // ─── Load list ───────────────────────────────────────────────────────────
  loadTypes(page = 1, pageSize = 20): void {
    this.store.setLoading(true);
    this.api
      .getList(page, pageSize)
      .pipe(finalize(() => this.store.setLoading(false)))
      .subscribe({
        next: (res) => {
          this.store.setTypes(res.data);
          this.store.setTotalCount(res.pagination.totalCount);
          this.store.setTotalPages(res.pagination.totalPages);
        },
        error: () => this.notification.error('載入客戶分類列表失敗'),
      });
  }

  // ─── Create ──────────────────────────────────────────────────────────────
  createType(dto: CustomerTypeCreateUpdate, page: number, pageSize: number): Observable<boolean> {
    this.store.setSaving(true);
    return this.api.create(dto).pipe(
      map(() => {
        this.notification.success('客戶分類新增成功');
        this.loadTypes(page, pageSize);
        return true;
      }),
      catchError(() => {
        this.notification.error('新增客戶分類失敗');
        return of(false);
      }),
      finalize(() => this.store.setSaving(false))
    );
  }

  // ─── Update ──────────────────────────────────────────────────────────────
  updateType(id: number, dto: CustomerTypeCreateUpdate, page: number, pageSize: number): Observable<boolean> {
    this.store.setSaving(true);
    return this.api.update(id, dto).pipe(
      map(() => {
        this.notification.success('客戶分類更新成功');
        this.loadTypes(page, pageSize);
        return true;
      }),
      catchError(() => {
        this.notification.error('更新客戶分類失敗');
        return of(false);
      }),
      finalize(() => this.store.setSaving(false))
    );
  }

  // ─── Delete ──────────────────────────────────────────────────────────────
  deleteType(id: number, page: number, pageSize: number): Observable<boolean> {
    this.store.setLoading(true);
    return this.api.delete(id).pipe(
      map(() => {
        this.notification.success('客戶分類刪除成功');
        // 刪除成功後重新從 API 載入當前頁
        this.loadTypes(page, pageSize);
        return true;
      }),
      catchError((err) => {
        const msg =
          err?.error?.error?.message ?? '刪除客戶分類失敗，此分類下仍有客戶';
        this.notification.error(msg);
        this.store.setLoading(false);
        return of(false);
      })
    );
  }
}
