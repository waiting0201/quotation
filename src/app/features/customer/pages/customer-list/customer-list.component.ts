import {
  Component,
  inject,
  signal,
  computed,
  OnInit,
  DestroyRef,
} from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { CustomerFacade } from '../../facades/customer.facade';
import { CustomerListItem } from '../../models/customer.model';

/** 刪除對話框狀態 */
interface DeleteDialogState {
  open: boolean;
  customerId: number;
  customerName: string;
  hasQuotations: boolean;
}

/** 分頁設定 */
const PAGE_SIZE = 20;

@Component({
  selector: 'app-customer-list',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './customer-list.component.html',
  styleUrl: './customer-list.component.scss',
})
export class CustomerListComponent implements OnInit {
  private readonly facade = inject(CustomerFacade);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  // ─── State ────────────────────────────────────────────────────────────────
  readonly customers = this.facade.customers;
  readonly loading = this.facade.loading;

  // ─── Search ───────────────────────────────────────────────────────────────
  readonly searchQuery = signal('');
  private readonly _searchInput$ = new Subject<string>();

  // ─── Dialog state ─────────────────────────────────────────────────────────
  readonly deleteDialog = signal<DeleteDialogState>({
    open: false,
    customerId: 0,
    customerName: '',
    hasQuotations: false,
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
        this.facade.loadCustomers(1, this.pageSize, query);
      });

    this.facade.loadCustomers(1, this.pageSize);
  }

  // ─── Search handler ───────────────────────────────────────────────────────
  onSearchInput(value: string): void {
    this._searchInput$.next(value);
  }

  // ─── Navigation ───────────────────────────────────────────────────────────
  goToCreate(): void {
    this.router.navigate(['/customer/create']);
  }

  goToEdit(customer: CustomerListItem): void {
    this.router.navigate(['/customer', customer.customerId]);
  }

  // ─── Delete ───────────────────────────────────────────────────────────────
  openDeleteDialog(customer: CustomerListItem): void {
    this.deleteDialog.set({
      open: true,
      customerId: customer.customerId,
      customerName: customer.name,
      hasQuotations: customer.hasQuotations,
    });
  }

  closeDeleteDialog(): void {
    this.deleteDialog.update((d) => ({ ...d, open: false }));
  }

  confirmDelete(): void {
    const { customerId } = this.deleteDialog();
    this.facade.deleteCustomer(customerId, this.currentPage(), this.pageSize, this.searchQuery())
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
    this.facade.loadCustomers(page, this.pageSize, this.searchQuery());
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
}
