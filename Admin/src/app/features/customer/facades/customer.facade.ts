import { Injectable, inject } from '@angular/core';
import { Observable, catchError, finalize, map, of } from 'rxjs';
import { CustomerApiService } from '../services/customer-api.service';
import { CustomerStore } from '../stores/customer.store';
import { NotificationService } from '../../../core/services/notification.service';

@Injectable({ providedIn: 'root' })
export class CustomerFacade {
  private readonly api = inject(CustomerApiService);
  private readonly store = inject(CustomerStore);
  private readonly notification = inject(NotificationService);

  // ─── Exposed store signals ───────────────────────────────────────────────
  readonly customers = this.store.customers;
  readonly loading = this.store.loading;
  readonly totalCount = this.store.totalCount;
  readonly totalPages = this.store.totalPages;

  // ─── Load list ───────────────────────────────────────────────────────────
  loadCustomers(page = 1, pageSize = 20, search?: string): void {
    this.store.setLoading(true);
    this.api
      .getList(page, pageSize, search)
      .pipe(finalize(() => this.store.setLoading(false)))
      .subscribe({
        next: (res) => {
          this.store.setCustomers(res.data);
          this.store.setTotalCount(res.pagination.totalCount);
          this.store.setTotalPages(res.pagination.totalPages);
        },
        error: () => this.notification.error('載入客戶列表失敗'),
      });
  }

  // ─── Delete ──────────────────────────────────────────────────────────────
  deleteCustomer(id: number, page: number, pageSize: number, search?: string): Observable<boolean> {
    this.store.setLoading(true);
    return this.api.delete(id).pipe(
      map(() => {
        this.notification.success('客戶刪除成功');
        // 刪除成功後重新從 API 載入當前頁
        this.loadCustomers(page, pageSize, search);
        return true;
      }),
      catchError((err) => {
        const msg =
          err?.error?.error?.message ?? '刪除客戶失敗，此客戶下仍有報價單';
        this.notification.error(msg);
        this.store.setLoading(false);
        return of(false);
      })
    );
  }
}
