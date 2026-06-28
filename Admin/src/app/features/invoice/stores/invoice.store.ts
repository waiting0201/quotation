import { Injectable, signal } from '@angular/core';
import { InvoiceListItem } from '../models/invoice.model';

@Injectable({ providedIn: 'root' })
export class InvoiceStore {
  // ─── Private writable signals ───────────────────────────────────────────
  private readonly _invoices = signal<InvoiceListItem[]>([]);
  private readonly _loading = signal(false);
  private readonly _totalCount = signal(0);
  private readonly _totalPages = signal(1);

  // ─── Public readonly signals ─────────────────────────────────────────────
  readonly invoices = this._invoices.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly totalPages = this._totalPages.asReadonly();

  // ─── Mutations ───────────────────────────────────────────────────────────
  setInvoices(invoices: InvoiceListItem[]): void {
    this._invoices.set(invoices);
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
