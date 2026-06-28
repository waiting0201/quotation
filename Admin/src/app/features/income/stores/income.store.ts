import { Injectable, signal } from '@angular/core';
import { IncomeListItem } from '../models/income.model';

@Injectable({ providedIn: 'root' })
export class IncomeStore {
  // ─── Private writable signals ───────────────────────────────────────────
  private readonly _incomes = signal<IncomeListItem[]>([]);
  private readonly _loading = signal(false);
  private readonly _totalCount = signal(0);
  private readonly _totalPages = signal(1);

  // ─── Public readonly signals ─────────────────────────────────────────────
  readonly incomes = this._incomes.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly totalPages = this._totalPages.asReadonly();

  // ─── Mutations ───────────────────────────────────────────────────────────
  setIncomes(incomes: IncomeListItem[]): void {
    this._incomes.set(incomes);
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
