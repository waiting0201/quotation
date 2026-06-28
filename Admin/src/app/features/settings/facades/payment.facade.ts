import { Injectable, inject } from '@angular/core';
import { Observable, catchError, finalize, map, of } from 'rxjs';
import { PaymentApiService } from '../services/payment-api.service';
import { PaymentStore } from '../stores/payment.store';
import { NotificationService } from '../../../core/services/notification.service';
import { PaymentCreateUpdate } from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class PaymentFacade {
  private readonly api = inject(PaymentApiService);
  private readonly store = inject(PaymentStore);
  private readonly notification = inject(NotificationService);

  // ─── Exposed store signals ───────────────────────────────────────────────
  readonly payments = this.store.payments;
  readonly loading = this.store.loading;
  readonly saving = this.store.saving;
  readonly totalCount = this.store.totalCount;
  readonly totalPages = this.store.totalPages;

  // ─── Load list ───────────────────────────────────────────────────────────
  loadPayments(page = 1, pageSize = 20): void {
    this.store.setLoading(true);
    this.api
      .getList(page, pageSize)
      .pipe(finalize(() => this.store.setLoading(false)))
      .subscribe({
        next: (res) => {
          this.store.setPayments(res.data);
          this.store.setTotalCount(res.pagination.totalCount);
          this.store.setTotalPages(res.pagination.totalPages);
        },
        error: () => this.notification.error('載入付款條件列表失敗'),
      });
  }

  // ─── Create ──────────────────────────────────────────────────────────────
  createPayment(dto: PaymentCreateUpdate, page: number, pageSize: number): Observable<boolean> {
    this.store.setSaving(true);
    return this.api.create(dto).pipe(
      map(() => {
        this.notification.success('付款條件新增成功');
        this.loadPayments(page, pageSize);
        return true;
      }),
      catchError(() => {
        this.notification.error('新增付款條件失敗');
        return of(false);
      }),
      finalize(() => this.store.setSaving(false))
    );
  }

  // ─── Update ──────────────────────────────────────────────────────────────
  updatePayment(id: number, dto: PaymentCreateUpdate, page: number, pageSize: number): Observable<boolean> {
    this.store.setSaving(true);
    return this.api.update(id, dto).pipe(
      map(() => {
        this.notification.success('付款條件更新成功');
        this.loadPayments(page, pageSize);
        return true;
      }),
      catchError(() => {
        this.notification.error('更新付款條件失敗');
        return of(false);
      }),
      finalize(() => this.store.setSaving(false))
    );
  }

  // ─── Delete ──────────────────────────────────────────────────────────────
  deletePayment(id: number, page: number, pageSize: number): Observable<boolean> {
    this.store.setLoading(true);
    return this.api.delete(id).pipe(
      map(() => {
        this.notification.success('付款條件刪除成功');
        this.loadPayments(page, pageSize);
        return true;
      }),
      catchError((err) => {
        const msg = err?.error?.error?.message ?? '刪除付款條件失敗';
        this.notification.error(msg);
        this.store.setLoading(false);
        return of(false);
      })
    );
  }
}
