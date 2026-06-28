import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { PaymentFacade } from '../../facades/payment.facade';
import { PaymentListItem, PaymentCreateUpdate } from '../../models/payment.model';
import { PaymentFormDialogComponent } from '../../components/payment-form-dialog/payment-form-dialog.component';

/** 刪除對話框的狀態 */
interface DeleteDialogState {
  open: boolean;
  paymentId: number;
  paymentRemark: string;
}

/** 分頁設定 */
const PAGE_SIZE = 20;

@Component({
  selector: 'app-payments-page',
  standalone: true,
  imports: [PaymentFormDialogComponent],
  templateUrl: './payments-page.component.html',
  styleUrl: './payments-page.component.scss',
})
export class PaymentsPageComponent implements OnInit {
  private readonly facade = inject(PaymentFacade);

  // ─── Facade signals ───────────────────────────────────────────────────────
  readonly payments = this.facade.payments;
  readonly loading = this.facade.loading;
  readonly saving = this.facade.saving;

  // ─── Dialog state ─────────────────────────────────────────────────────────
  readonly formDialogOpen = signal(false);
  readonly editingPayment = signal<PaymentListItem | null>(null);

  readonly deleteDialog = signal<DeleteDialogState>({
    open: false,
    paymentId: 0,
    paymentRemark: '',
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
    this.facade.loadPayments(1, this.pageSize);
  }

  // ─── Create / Edit ────────────────────────────────────────────────────────
  openCreateDialog(): void {
    this.editingPayment.set(null);
    this.formDialogOpen.set(true);
  }

  openEditDialog(payment: PaymentListItem): void {
    this.editingPayment.set(payment);
    this.formDialogOpen.set(true);
  }

  closeFormDialog(): void {
    this.formDialogOpen.set(false);
    this.editingPayment.set(null);
  }

  onFormSaved(dto: PaymentCreateUpdate): void {
    const editing = this.editingPayment();
    const obs = editing
      ? this.facade.updatePayment(editing.paymentId, dto, this.currentPage(), this.pageSize)
      : this.facade.createPayment(dto, this.currentPage(), this.pageSize);

    obs.subscribe((ok) => {
      if (ok) {
        this.closeFormDialog();
      }
    });
  }

  // ─── Delete ───────────────────────────────────────────────────────────────
  openDeleteDialog(payment: PaymentListItem): void {
    this.deleteDialog.set({
      open: true,
      paymentId: payment.paymentId,
      paymentRemark: payment.remark,
    });
  }

  closeDeleteDialog(): void {
    this.deleteDialog.update((d) => ({ ...d, open: false }));
  }

  confirmDelete(): void {
    const { paymentId } = this.deleteDialog();
    this.facade.deletePayment(paymentId, this.currentPage(), this.pageSize).subscribe((ok) => {
      if (ok) {
        this.closeDeleteDialog();
      }
    });
  }

  // ─── Pagination ───────────────────────────────────────────────────────────
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.facade.loadPayments(page, this.pageSize);
  }
}
