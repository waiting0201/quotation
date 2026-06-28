import {
  Component,
  inject,
  signal,
  computed,
  OnInit,
  DestroyRef,
  ElementRef,
  HostListener,
} from '@angular/core';
import { Router } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { IncomeApiService } from '../../services/income-api.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { CustomerLookup, IncomeInvoiceOption } from '../../models/income.model';

function getTodayString(): string {
  const now = new Date(
    new Date().toLocaleString('en-US', { timeZone: 'Asia/Taipei' })
  );
  const y = now.getFullYear();
  const m = String(now.getMonth() + 1).padStart(2, '0');
  const d = String(now.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

@Component({
  selector: 'app-income-create',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe, DecimalPipe],
  templateUrl: './income-create.component.html',
  styleUrl: './income-create.component.scss',
})
export class IncomeCreateComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly api = inject(IncomeApiService);
  private readonly notify = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  // ─── State ────────────────────────────────────────────────────────────────
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly customers = signal<CustomerLookup[]>([]);

  // ─── 可核銷請款單（發票）選擇 ──────────────────────────────────────────────
  readonly invoices = signal<IncomeInvoiceOption[]>([]);
  readonly loadingInvoices = signal(false);
  readonly selectedInvoiceIds = signal<Set<string>>(new Set());
  readonly selectedInvoiceTotal = computed(() => {
    const selected = this.selectedInvoiceIds();
    return this.invoices()
      .filter((inv) => selected.has(inv.invoiceId))
      .reduce((sum, inv) => sum + (inv.total ?? 0), 0);
  });

  // ─── Customer searchable dropdown ─────────────────────────────────────────
  private readonly elRef = inject(ElementRef);
  readonly customerSearch = signal('');
  readonly customerDropdownOpen = signal(false);
  readonly filteredCustomers = computed(() => {
    const keyword = this.customerSearch().trim().toLowerCase();
    const all = this.customers();
    if (!keyword) return all;
    return all.filter(
      (c) =>
        c.name.toLowerCase().includes(keyword) ||
        c.code.toLowerCase().includes(keyword)
    );
  });
  readonly selectedCustomerName = signal('');

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const dropdown = this.elRef.nativeElement.querySelector('.customer-dropdown');
    if (dropdown && !dropdown.contains(event.target as Node)) {
      this.customerDropdownOpen.set(false);
    }
  }

  // ─── Form ─────────────────────────────────────────────────────────────────
  form!: FormGroup;

  ngOnInit(): void {
    this.form = this.fb.group({
      customerId: [null as number | null, [Validators.required]],
      incomeDate: [getTodayString()],
      amount: [null as number | null, [Validators.min(0)]],
      fee: [null as number | null, [Validators.min(0)]],
      remark: ['', [Validators.maxLength(500)]],
    });

    this._loadCustomers();
  }

  // ─── Customer searchable dropdown handlers ────────────────────────────────
  onCustomerSearchInput(value: string): void {
    this.customerSearch.set(value);
    this.customerDropdownOpen.set(true);
  }

  onCustomerSearchFocus(): void {
    this.customerDropdownOpen.set(true);
  }

  selectCustomer(customer: CustomerLookup): void {
    this.form.get('customerId')!.setValue(customer.customerId);
    this.selectedCustomerName.set(customer.name);
    this.customerSearch.set('');
    this.customerDropdownOpen.set(false);
    this._loadInvoices(customer.customerId);
  }

  clearCustomer(): void {
    this.form.get('customerId')!.setValue(null);
    this.selectedCustomerName.set('');
    this.customerSearch.set('');
    this.invoices.set([]);
    this.selectedInvoiceIds.set(new Set());
    this.form.get('amount')!.setValue(null);
  }

  // ─── 請款單勾選 ──────────────────────────────────────────────────────────
  toggleInvoice(invoiceId: string): void {
    const next = new Set(this.selectedInvoiceIds());
    if (next.has(invoiceId)) {
      next.delete(invoiceId);
    } else {
      next.add(invoiceId);
    }
    this.selectedInvoiceIds.set(next);
    this._syncAmountFromSelection();
  }

  isInvoiceSelected(invoiceId: string): boolean {
    return this.selectedInvoiceIds().has(invoiceId);
  }

  /** 勾選的請款單金額自動加總帶入「實收金額」（使用者仍可手動調整） */
  private _syncAmountFromSelection(): void {
    this.form.get('amount')!.setValue(this.selectedInvoiceTotal());
  }

  private _loadInvoices(customerId: number): void {
    this.invoices.set([]);
    this.selectedInvoiceIds.set(new Set());
    // 重置金額，避免殘留前一位客戶勾選的加總值
    this.form.get('amount')!.setValue(null);
    this.loadingInvoices.set(true);
    this.api.getSelectableInvoices(customerId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.invoices.set(res.data);
          this.loadingInvoices.set(false);
        },
        error: () => {
          this.notify.error('載入請款單資料失敗');
          this.loadingInvoices.set(false);
        },
      });
  }

  // ─── Load customers ─────────────────────────────────────────────────────
  private _loadCustomers(): void {
    this.loading.set(true);
    this.api.getCustomers()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.customers.set(res.data);
          this.loading.set(false);
        },
        error: () => {
          this.notify.error('載入客戶資料失敗');
          this.loading.set(false);
        },
      });
  }

  // ─── Save ─────────────────────────────────────────────────────────────────
  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.value;
    const invoiceIds = Array.from(this.selectedInvoiceIds());
    const dto = {
      customerId: v.customerId,
      amount: v.amount ?? undefined,
      fee: v.fee ?? undefined,
      incomeDate: v.incomeDate || undefined,
      remark: v.remark?.trim() || undefined,
      invoiceIds: invoiceIds.length > 0 ? invoiceIds : undefined,
    };

    this.saving.set(true);
    this.api.create(dto)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notify.success('入帳新增成功');
          this.saving.set(false);
          this.router.navigate(['/income']);
        },
        error: () => {
          this.notify.error('新增入帳失敗');
          this.saving.set(false);
        },
      });
  }

  onCancel(): void {
    this.router.navigate(['/income']);
  }

  // ─── Helpers ────────────────────────────────────────────────────────────
  get customerIdInvalid(): boolean {
    const ctrl = this.form.get('customerId')!;
    return ctrl.invalid && ctrl.touched;
  }

  invoiceStatusLabel(status: number | null): string {
    switch (status) {
      case 0: return '已開';
      case 1: return '已寄出';
      case 2: return '已入帳';
      case 3: return '作廢';
      default: return '—';
    }
  }
}
