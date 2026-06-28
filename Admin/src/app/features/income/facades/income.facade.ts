import { Injectable, inject } from '@angular/core';
import { Observable, catchError, finalize, map, of } from 'rxjs';
import { IncomeApiService } from '../services/income-api.service';
import { IncomeStore } from '../stores/income.store';
import { NotificationService } from '../../../core/services/notification.service';

@Injectable({ providedIn: 'root' })
export class IncomeFacade {
  private readonly api = inject(IncomeApiService);
  private readonly store = inject(IncomeStore);
  private readonly notification = inject(NotificationService);

  // ─── Exposed store signals ───────────────────────────────────────────────
  readonly incomes = this.store.incomes;
  readonly loading = this.store.loading;
  readonly totalCount = this.store.totalCount;
  readonly totalPages = this.store.totalPages;

  // ─── Load list ───────────────────────────────────────────────────────────
  loadIncomes(page = 1, pageSize = 20, search?: string): void {
    this.store.setLoading(true);
    this.api
      .getList(page, pageSize, search)
      .pipe(finalize(() => this.store.setLoading(false)))
      .subscribe({
        next: (res) => {
          this.store.setIncomes(res.data);
          this.store.setTotalCount(res.pagination.totalCount);
          this.store.setTotalPages(res.pagination.totalPages);
        },
        error: () => this.notification.error('載入入帳列表失敗'),
      });
  }

  // ─── Delete ──────────────────────────────────────────────────────────────
  deleteIncome(id: string, page: number, pageSize: number, search?: string): Observable<boolean> {
    this.store.setLoading(true);
    return this.api.delete(id).pipe(
      map(() => {
        this.notification.success('入帳紀錄刪除成功');
        // 刪除成功後重新從 API 載入當前頁
        this.loadIncomes(page, pageSize, search);
        return true;
      }),
      catchError((err) => {
        const msg =
          err?.error?.error?.message ?? '刪除入帳紀錄失敗';
        this.notification.error(msg);
        this.store.setLoading(false);
        return of(false);
      })
    );
  }
}
