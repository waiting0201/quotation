import { Injectable, inject } from '@angular/core';
import { Observable, catchError, finalize, map, of } from 'rxjs';
import { QuotationApiService } from '../services/quotation-api.service';
import { QuotationStore } from '../stores/quotation.store';
import { NotificationService } from '../../../core/services/notification.service';

@Injectable({ providedIn: 'root' })
export class QuotationFacade {
  private readonly api = inject(QuotationApiService);
  private readonly store = inject(QuotationStore);
  private readonly notification = inject(NotificationService);

  // ─── Exposed store signals ───────────────────────────────────────────────
  readonly quotations = this.store.quotations;
  readonly loading = this.store.loading;
  readonly totalCount = this.store.totalCount;
  readonly totalPages = this.store.totalPages;

  // ─── Load list ───────────────────────────────────────────────────────────
  loadQuotations(page = 1, pageSize = 20, search?: string): void {
    this.store.setLoading(true);
    this.api
      .getList(page, pageSize, search)
      .pipe(finalize(() => this.store.setLoading(false)))
      .subscribe({
        next: (res) => {
          this.store.setQuotations(res.data);
          this.store.setTotalCount(res.pagination.totalCount);
          this.store.setTotalPages(res.pagination.totalPages);
        },
        error: () => this.notification.error('載入報價清單失敗'),
      });
  }

  // ─── Delete ──────────────────────────────────────────────────────────────
  deleteQuotation(id: string, page: number, pageSize: number, search?: string): Observable<boolean> {
    this.store.setLoading(true);
    return this.api.delete(id).pipe(
      map(() => {
        this.notification.success('報價單刪除成功');
        // 刪除成功後重新從 API 載入當前頁
        this.loadQuotations(page, pageSize, search);
        return true;
      }),
      catchError((err) => {
        const msg =
          err?.error?.error?.message ?? '刪除報價單失敗，此報價單下仍有請款紀錄';
        this.notification.error(msg);
        this.store.setLoading(false);
        return of(false);
      })
    );
  }
}
