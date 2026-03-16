import { Injectable, signal } from '@angular/core';
import { QuotationListItem } from '../models/quotation.model';

@Injectable({ providedIn: 'root' })
export class QuotationStore {
  // ─── Private writable signals ───────────────────────────────────────────
  private readonly _quotations = signal<QuotationListItem[]>([]);
  private readonly _loading = signal(false);
  private readonly _totalCount = signal(0);
  private readonly _totalPages = signal(1);

  // ─── Public readonly signals ─────────────────────────────────────────────
  readonly quotations = this._quotations.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly totalPages = this._totalPages.asReadonly();

  // ─── Mutations ───────────────────────────────────────────────────────────
  setQuotations(quotations: QuotationListItem[]): void {
    this._quotations.set(quotations);
  }

  setLoading(loading: boolean): void {
    this._loading.set(loading);
  }

  setTotalCount(count: number): void {
    this._totalCount.set(count);
  }

  setTotalPages(pages: number): void {
    this._totalPages.set(pages);
  }
}
