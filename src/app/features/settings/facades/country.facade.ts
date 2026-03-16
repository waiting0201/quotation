import { Injectable, inject } from '@angular/core';
import { Observable, catchError, finalize, map, of } from 'rxjs';
import { CountryApiService } from '../services/country-api.service';
import { CountryStore } from '../stores/country.store';
import { NotificationService } from '../../../core/services/notification.service';
import { CountryCreateUpdate } from '../models/country.model';

@Injectable({ providedIn: 'root' })
export class CountryFacade {
  private readonly api = inject(CountryApiService);
  private readonly store = inject(CountryStore);
  private readonly notification = inject(NotificationService);

  // ─── Exposed store signals ───────────────────────────────────────────────
  readonly countries = this.store.countries;
  readonly loading = this.store.loading;
  readonly saving = this.store.saving;
  readonly totalCount = this.store.totalCount;
  readonly totalPages = this.store.totalPages;

  // ─── Load list ───────────────────────────────────────────────────────────
  loadCountries(page = 1, pageSize = 20): void {
    this.store.setLoading(true);
    this.api
      .getList(page, pageSize)
      .pipe(finalize(() => this.store.setLoading(false)))
      .subscribe({
        next: (res) => {
          this.store.setCountries(res.data);
          this.store.setTotalCount(res.pagination.totalCount);
          this.store.setTotalPages(res.pagination.totalPages);
        },
        error: () => this.notification.error('載入國家列表失敗'),
      });
  }

  // ─── Create ──────────────────────────────────────────────────────────────
  createCountry(dto: CountryCreateUpdate, page: number, pageSize: number): Observable<boolean> {
    this.store.setSaving(true);
    return this.api.create(dto).pipe(
      map(() => {
        this.notification.success('國家新增成功');
        this.loadCountries(page, pageSize);
        return true;
      }),
      catchError(() => {
        this.notification.error('新增國家失敗');
        return of(false);
      }),
      finalize(() => this.store.setSaving(false))
    );
  }

  // ─── Update ──────────────────────────────────────────────────────────────
  updateCountry(id: number, dto: CountryCreateUpdate, page: number, pageSize: number): Observable<boolean> {
    this.store.setSaving(true);
    return this.api.update(id, dto).pipe(
      map(() => {
        this.notification.success('國家更新成功');
        this.loadCountries(page, pageSize);
        return true;
      }),
      catchError(() => {
        this.notification.error('更新國家失敗');
        return of(false);
      }),
      finalize(() => this.store.setSaving(false))
    );
  }

  // ─── Delete ──────────────────────────────────────────────────────────────
  deleteCountry(id: number, page: number, pageSize: number): Observable<boolean> {
    this.store.setLoading(true);
    return this.api.delete(id).pipe(
      map(() => {
        this.notification.success('國家刪除成功');
        this.loadCountries(page, pageSize);
        return true;
      }),
      catchError((err) => {
        const msg =
          err?.error?.error?.message ?? '刪除國家失敗，此國家下仍有客戶';
        this.notification.error(msg);
        this.store.setLoading(false);
        return of(false);
      })
    );
  }
}
