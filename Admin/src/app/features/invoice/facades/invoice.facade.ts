import { Injectable, inject } from '@angular/core';
import { Observable, catchError, finalize, map, of } from 'rxjs';
import { InvoiceApiService } from '../services/invoice-api.service';
import { InvoiceStore } from '../stores/invoice.store';
import { NotificationService } from '../../../core/services/notification.service';

@Injectable({ providedIn: 'root' })
export class InvoiceFacade {
  private readonly api = inject(InvoiceApiService);
  private readonly store = inject(InvoiceStore);
  private readonly notification = inject(NotificationService);

  // ─── Exposed store signals ───────────────────────────────────────────────
  readonly invoices = this.store.invoices;
  readonly loading = this.store.loading;
  readonly totalCount = this.store.totalCount;
  readonly totalPages = this.store.totalPages;

  // ─── Load list ───────────────────────────────────────────────────────────
  loadInvoices(page = 1, pageSize = 20, search?: string): void {
    this.store.setLoading(true);
    this.api
      .getList(page, pageSize, search)
      .pipe(finalize(() => this.store.setLoading(false)))
      .subscribe({
        next: (res) => {
          this.store.setInvoices(res.data);
          this.store.setTotalCount(res.pagination.totalCount);
          this.store.setTotalPages(res.pagination.totalPages);
        },
        error: () => this.notification.error('載入請款列表失敗'),
      });
  }

  // ─── Delete ──────────────────────────────────────────────────────────────
  deleteInvoice(id: string, page: number, pageSize: number, search?: string): Observable<boolean> {
    this.store.setLoading(true);
    return this.api.delete(id).pipe(
      map(() => {
        this.notification.success('請款刪除成功');
        // 刪除成功後重新從 API 載入當前頁
        this.loadInvoices(page, pageSize, search);
        return true;
      }),
      catchError((err) => {
        const msg =
          err?.error?.error?.message ?? '刪除請款失敗，此請款下仍有收款紀錄';
        this.notification.error(msg);
        this.store.setLoading(false);
        return of(false);
      })
    );
  }
}
