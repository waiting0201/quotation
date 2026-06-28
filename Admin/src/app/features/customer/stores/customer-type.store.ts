import { Injectable, signal } from '@angular/core';
import { CustomerTypeListItem } from '../models/customer-type.model';

@Injectable({ providedIn: 'root' })
export class CustomerTypeStore {
  // ─── Private writable signals ───────────────────────────────────────────
  private readonly _types = signal<CustomerTypeListItem[]>([]);
  private readonly _loading = signal(false);
  private readonly _saving = signal(false);
  private readonly _totalCount = signal(0);
  private readonly _totalPages = signal(1);

  // ─── Public readonly signals ─────────────────────────────────────────────
  readonly types = this._types.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly saving = this._saving.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly totalPages = this._totalPages.asReadonly();

  // ─── Mutations ───────────────────────────────────────────────────────────
  setTypes(types: CustomerTypeListItem[]): void {
    this._types.set(types);
  }

  setLoading(loading: boolean): void {
    this._loading.set(loading);
  }

  setSaving(saving: boolean): void {
    this._saving.set(saving);
  }

  setTotalCount(count: number): void {
    this._totalCount.set(count);
  }

  setTotalPages(pages: number): void {
    this._totalPages.set(pages);
  }
}
