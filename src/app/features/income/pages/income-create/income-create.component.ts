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
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { IncomeApiService } from '../../services/income-api.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { CustomerLookup } from '../../models/income.model';

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
  imports: [ReactiveFormsModule],
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
  }

  clearCustomer(): void {
    this.form.get('customerId')!.setValue(null);
    this.selectedCustomerName.set('');
    this.customerSearch.set('');
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
    const dto = {
      customerId: v.customerId,
      amount: v.amount ?? undefined,
      fee: v.fee ?? undefined,
      incomeDate: v.incomeDate || undefined,
      remark: v.remark?.trim() || undefined,
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
}
