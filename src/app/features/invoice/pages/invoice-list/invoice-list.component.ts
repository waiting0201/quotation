import {
  Component,
  inject,
  signal,
  computed,
  OnInit,
  DestroyRef,
} from '@angular/core';
import { Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { InvoiceFacade } from '../../facades/invoice.facade';
import { InvoiceListItem } from '../../models/invoice.model';

/** 稅別顯示名稱 */
const TAX_LABELS: Record<number, string> = {
  0: '稅外加',
  1: '稅內含',
  2: '免稅',
};

/** 狀態顯示設定 */
const STATUS_CONFIG: Record<number, { label: string; cssClass: string }> = {
  0: { label: '已開',   cssClass: 'status-issued' },
  1: { label: '已寄出', cssClass: 'status-sent' },
  2: { label: '已入帳', cssClass: 'status-settled' },
  3: { label: '作廢',   cssClass: 'status-void' },
};

/** 刪除對話框狀態 */
interface DeleteDialogState {
  open: boolean;
  invoiceId: string;
  invoiceCode: string;
  hasIncomes: boolean;
}

/** 分頁設定 */
const PAGE_SIZE = 20;

@Component({
  selector: 'app-invoice-list',
  standalone: true,
  imports: [FormsModule, NgClass],
  templateUrl: './invoice-list.component.html',
  styleUrl: './invoice-list.component.scss',
})
export class InvoiceListComponent implements OnInit {
  private readonly facade = inject(InvoiceFacade);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  // ─── State ────────────────────────────────────────────────────────────────
  readonly invoices = this.facade.invoices;
  readonly loading = this.facade.loading;

  // ─── Search ───────────────────────────────────────────────────────────────
  readonly searchQuery = signal('');
  private readonly _searchInput$ = new Subject<string>();

  // ─── Dialog state ─────────────────────────────────────────────────────────
  readonly deleteDialog = signal<DeleteDialogState>({
    open: false,
    invoiceId: '',
    invoiceCode: '',
    hasIncomes: false,
  });

  // ─── Pagination ───────────────────────────────────────────────────────────
  readonly currentPage = signal(1);
  readonly pageSize = PAGE_SIZE;

  readonly totalCount = this.facade.totalCount;
  readonly totalPages = this.facade.totalPages;
  readonly pageNumbers = computed(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const maxVisible = 5;
    let start = Math.max(1, current - Math.floor(maxVisible / 2));
    let end = start + maxVisible - 1;
    if (end > total) {
      end = total;
      start = Math.max(1, end - maxVisible + 1);
    }
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  });

  // ─── Lifecycle ───────────────────────────────────────────────────────────
  ngOnInit(): void {
    this._searchInput$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((query) => {
        this.searchQuery.set(query);
        this.currentPage.set(1);
        this.facade.loadInvoices(1, this.pageSize, query);
      });

    this.facade.loadInvoices(1, this.pageSize);
  }

  // ─── Search handler ───────────────────────────────────────────────────────
  onSearchInput(value: string): void {
    this._searchInput$.next(value);
  }

  // ─── Navigation ───────────────────────────────────────────────────────────
  goToCreate(): void {
    this.router.navigate(['/invoice/create']);
  }

  goToEdit(invoice: InvoiceListItem): void {
    this.router.navigate(['/invoice', invoice.invoiceId]);
  }

  // ─── Delete ───────────────────────────────────────────────────────────────
  openDeleteDialog(invoice: InvoiceListItem): void {
    this.deleteDialog.set({
      open: true,
      invoiceId: invoice.invoiceId,
      invoiceCode: invoice.invoiceCode,
      hasIncomes: invoice.hasIncomes,
    });
  }

  closeDeleteDialog(): void {
    this.deleteDialog.update((d) => ({ ...d, open: false }));
  }

  confirmDelete(): void {
    const { invoiceId } = this.deleteDialog();
    this.facade.deleteInvoice(invoiceId, this.currentPage(), this.pageSize, this.searchQuery())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ok) => {
        if (ok) {
          this.closeDeleteDialog();
        }
      });
  }

  // ─── Pagination ───────────────────────────────────────────────────────────
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.facade.loadInvoices(page, this.pageSize, this.searchQuery());
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────
  formatDate(dateStr: string | null): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return '—';
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}/${m}/${day}`;
  }

  formatAmount(amount: number): string {
    return `NT$${amount.toLocaleString()}`;
  }

  getTaxLabel(tax: number): string {
    return TAX_LABELS[tax] ?? '—';
  }

  getStatusConfig(status: number): { label: string; cssClass: string } {
    return STATUS_CONFIG[status] ?? { label: '未知', cssClass: '' };
  }
}
