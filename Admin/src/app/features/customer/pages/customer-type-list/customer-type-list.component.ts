import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CustomerTypeFacade } from '../../facades/customer-type.facade';
import { CustomerTypeListItem, CustomerTypeCreateUpdate } from '../../models/customer-type.model';
import { CustomerTypeFormDialogComponent } from '../../components/customer-type-form-dialog/customer-type-form-dialog.component';

/** 刪除對話框的狀態 */
interface DeleteDialogState {
  open: boolean;
  typeId: number;
  typeTitle: string;
  hasCustomers: boolean;
  customerCount: number;
}

/** 分頁設定 */
const PAGE_SIZE = 20;

@Component({
  selector: 'app-customer-type-list',
  standalone: true,
  imports: [CustomerTypeFormDialogComponent],
  templateUrl: './customer-type-list.component.html',
  styleUrl: './customer-type-list.component.scss',
})
export class CustomerTypeListComponent implements OnInit {
  private readonly facade = inject(CustomerTypeFacade);

  // ─── Facade signals ───────────────────────────────────────────────────────
  readonly types = this.facade.types;
  readonly loading = this.facade.loading;
  readonly saving = this.facade.saving;

  // ─── Dialog state ─────────────────────────────────────────────────────────
  readonly formDialogOpen = signal(false);
  readonly editingType = signal<CustomerTypeListItem | null>(null);

  readonly deleteDialog = signal<DeleteDialogState>({
    open: false,
    typeId: 0,
    typeTitle: '',
    hasCustomers: false,
    customerCount: 0,
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
    this.facade.loadTypes(1, this.pageSize);
  }

  // ─── Create / Edit ────────────────────────────────────────────────────────
  openCreateDialog(): void {
    this.editingType.set(null);
    this.formDialogOpen.set(true);
  }

  openEditDialog(type: CustomerTypeListItem): void {
    this.editingType.set(type);
    this.formDialogOpen.set(true);
  }

  closeFormDialog(): void {
    this.formDialogOpen.set(false);
    this.editingType.set(null);
  }

  onFormSaved(dto: CustomerTypeCreateUpdate): void {
    const editing = this.editingType();
    const obs = editing
      ? this.facade.updateType(editing.customerTypeId, dto, this.currentPage(), this.pageSize)
      : this.facade.createType(dto, this.currentPage(), this.pageSize);

    obs.subscribe((ok) => {
      if (ok) {
        this.closeFormDialog();
      }
    });
  }

  // ─── Delete ───────────────────────────────────────────────────────────────
  openDeleteDialog(type: CustomerTypeListItem): void {
    this.deleteDialog.set({
      open: true,
      typeId: type.customerTypeId,
      typeTitle: type.title,
      hasCustomers: type.customerCount > 0,
      customerCount: type.customerCount,
    });
  }

  closeDeleteDialog(): void {
    this.deleteDialog.update((d) => ({ ...d, open: false }));
  }

  confirmDelete(): void {
    const { typeId } = this.deleteDialog();
    this.facade.deleteType(typeId, this.currentPage(), this.pageSize).subscribe((ok) => {
      if (ok) {
        this.closeDeleteDialog();
      }
    });
  }

  // ─── Pagination ───────────────────────────────────────────────────────────
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.facade.loadTypes(page, this.pageSize);
  }
}
