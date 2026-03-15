import { Injectable, signal } from '@angular/core';
import { CustomerListItem } from '../models/customer.model';

@Injectable({ providedIn: 'root' })
export class CustomerStore {
  // ─── Private writable signals ───────────────────────────────────────────
  private readonly _customers = signal<CustomerListItem[]>([]);
  private readonly _loading = signal(false);
  private readonly _totalCount = signal(0);
  private readonly _totalPages = signal(1);

  // ─── Public readonly signals ─────────────────────────────────────────────
  readonly customers = this._customers.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly totalPages = this._totalPages.asReadonly();

  // ─── Mutations ───────────────────────────────────────────────────────────
  setCustomers(customers: CustomerListItem[]): void {
    this._customers.set(customers);
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
