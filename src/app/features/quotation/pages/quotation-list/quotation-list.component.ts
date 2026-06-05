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
import { QuotationFacade } from '../../facades/quotation.facade';
import { QuotationApiService } from '../../services/quotation-api.service';
import { QuotationListItem } from '../../models/quotation.model';

/** 稅別顯示名稱 */
const TAX_LABELS: Record<number, string> = {
  0: '稅外加',
  1: '稅內含',
  2: '免稅',
};

/** 狀態顯示設定 */
const STATUS_CONFIG: Record<number, { label: string; cssClass: string }> = {
  0: { label: '已報價', cssClass: 'status-quoted' },
  1: { label: '已簽約', cssClass: 'status-signed' },
  2: { label: '已結案', cssClass: 'status-closed' },
  3: { label: '已取消', cssClass: 'status-cancelled' },
};

/** 刪除對話框狀態 */
interface DeleteDialogState {
  open: boolean;
  itemId: string;
  itemCode: string;
  hasInvoices: boolean;
}

/** 分頁設定 */
const PAGE_SIZE = 20;

@Component({
  selector: 'app-quotation-list',
  standalone: true,
  imports: [FormsModule, NgClass],
  templateUrl: './quotation-list.component.html',
  styleUrl: './quotation-list.component.scss',
})
export class QuotationListComponent implements OnInit {
  private readonly facade = inject(QuotationFacade);
  private readonly quotationApi = inject(QuotationApiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  // ─── State ────────────────────────────────────────────────────────────────
  readonly quotations = this.facade.quotations;
  readonly loading = this.facade.loading;

  // ─── Search ───────────────────────────────────────────────────────────────
  readonly searchQuery = signal('');
  private readonly _searchInput$ = new Subject<string>();

  // ─── Dialog state ─────────────────────────────────────────────────────────
  readonly deleteDialog = signal<DeleteDialogState>({
    open: false,
    itemId: '',
    itemCode: '',
    hasInvoices: false,
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
        this.facade.loadQuotations(1, this.pageSize, query);
      });

    this.facade.loadQuotations(1, this.pageSize);
  }

  // ─── Search handler ───────────────────────────────────────────────────────
  onSearchInput(value: string): void {
    this._searchInput$.next(value);
  }

  // ─── Navigation ───────────────────────────────────────────────────────────
  goToCreate(): void {
    this.router.navigate(['/quotation/create']);
  }

  goToDetail(quotation: QuotationListItem): void {
    const url = this.router.serializeUrl(
      this.router.createUrlTree(['/quotation', quotation.itemId, 'detail'])
    );
    window.open(url, '_blank');
  }

  goToEdit(quotation: QuotationListItem): void {
    this.router.navigate(['/quotation', quotation.itemId]);
  }

  // ─── Delete ───────────────────────────────────────────────────────────────
  openDeleteDialog(quotation: QuotationListItem): void {
    this.deleteDialog.set({
      open: true,
      itemId: quotation.itemId,
      itemCode: quotation.itemCode,
      hasInvoices: quotation.hasInvoices,
    });
  }

  closeDeleteDialog(): void {
    this.deleteDialog.update((d) => ({ ...d, open: false }));
  }

  confirmDelete(): void {
    const { itemId } = this.deleteDialog();
    this.facade.deleteQuotation(itemId, this.currentPage(), this.pageSize, this.searchQuery())
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
    this.facade.loadQuotations(page, this.pageSize, this.searchQuery());
  }

  // ─── Download PDF ─────────────────────────────────────────────────────────
  downloadPdf(quotation: QuotationListItem): void {
    this.quotationApi.downloadPdf(quotation.itemId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `${quotation.itemCode}.pdf`;
          a.click();
          URL.revokeObjectURL(url);
        },
      });
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

  getTaxLabel(taxType: number): string {
    return TAX_LABELS[taxType] ?? '—';
  }

  getStatusConfig(status: number): { label: string; cssClass: string } {
    return STATUS_CONFIG[status] ?? { label: '未知', cssClass: '' };
  }
}
