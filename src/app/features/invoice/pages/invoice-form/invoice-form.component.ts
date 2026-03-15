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
import { ActivatedRoute, Router } from '@angular/router';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  FormArray,
  Validators,
} from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { InvoiceApiService } from '../../services/invoice-api.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { QuotationLookup, CustomerLookup } from '../../models/invoice.model';

// 取得今天的 YYYY-MM-DD（Asia/Taipei）
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
  selector: 'app-invoice-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './invoice-form.component.html',
  styleUrl: './invoice-form.component.scss',
})
export class InvoiceFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly invoiceApi = inject(InvoiceApiService);
  private readonly notify = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  // ─── State ────────────────────────────────────────────────────────────────
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly invoiceId = signal<string | null>(null);
  readonly customers = signal<CustomerLookup[]>([]);
  readonly quotations = signal<QuotationLookup[]>([]);
  readonly quotationsLoading = signal(false);

  readonly isEditMode = computed(() => this.invoiceId() !== null);
  readonly pageTitle = computed(() => this.isEditMode() ? '編輯請款' : '新增請款');

  // ─── Customer searchable dropdown ───────────────────────────────────────
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
    const today = getTodayString();

    this.form = this.fb.group({
      customerId: [null as number | null, Validators.required],
      requestDate: [today, Validators.required],
      status: [0],
      remark: [''],
      details: this.fb.array([]),
    });

    // 先載入客戶下拉，完成後再載入發票資料（編輯模式）
    this.loading.set(true);
    this._loadCustomers(() => {
      const idParam = this.route.snapshot.paramMap.get('id');
      if (idParam) {
        this.invoiceId.set(idParam);
        this._loadInvoice(idParam);
        return;
      }
      this.loading.set(false);
    });
  }

  // ─── Details FormArray ────────────────────────────────────────────────────
  get details(): FormArray {
    return this.form.get('details') as FormArray;
  }

  addDetail(): void {
    const today = getTodayString();
    this.details.push(
      this.fb.group({
        invoiceDetailId: [null],
        itemId: [null as string | null],
        invoiceType: [0],
        invoiceDate: [today],
        invoiceNumber: [''],
        price: [null as number | null, Validators.required],
        remark: [''],
      })
    );
  }

  removeDetail(index: number): void {
    this.details.removeAt(index);
  }

  // ─── Customer searchable dropdown handlers ──────────────────────────────
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
    this._onCustomerChanged(customer.customerId);
  }

  clearCustomer(): void {
    this.form.get('customerId')!.setValue(null);
    this.selectedCustomerName.set('');
    this.customerSearch.set('');
    this._onCustomerChanged(null);
  }

  private _onCustomerChanged(customerId: number | null): void {
    // 清除現有明細的報價單選擇
    for (let i = 0; i < this.details.length; i++) {
      this.details.at(i).patchValue({ itemId: null });
    }
    this.quotations.set([]);

    if (customerId) {
      this.quotationsLoading.set(true);
      this.invoiceApi
        .getCustomerQuotations(customerId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => {
            this.quotations.set(res.data);
            this.quotationsLoading.set(false);
          },
          error: () => {
            this.notify.error('載入報價單失敗');
            this.quotationsLoading.set(false);
          },
        });
    }
  }

  // ─── Load data ────────────────────────────────────────────────────────────
  private _loadCustomers(onComplete: () => void): void {
    this.invoiceApi
      .getCustomers()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.customers.set(res.data);
          onComplete();
        },
        error: () => {
          this.notify.error('載入客戶資料失敗');
          this.loading.set(false);
        },
      });
  }

  private _loadInvoice(id: string): void {
    this.invoiceApi
      .getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          const inv = res.data;

          this.form.patchValue({
            customerId: inv.customerId,
            requestDate: inv.requestDate
              ? inv.requestDate.substring(0, 10)
              : getTodayString(),
            status: inv.status,
            remark: inv.remark ?? '',
          });
          this.selectedCustomerName.set(inv.customerName ?? '');

          // 載入客戶的報價單後再填入明細
          if (inv.customerId) {
            this.invoiceApi
              .getCustomerQuotations(inv.customerId)
              .pipe(takeUntilDestroyed(this.destroyRef))
              .subscribe({
                next: (quotRes) => {
                  this.quotations.set(quotRes.data);
                  this._populateDetails(inv.details);
                  this.loading.set(false);
                },
                error: () => {
                  // 即使報價單載入失敗，仍填入明細
                  this._populateDetails(inv.details);
                  this.loading.set(false);
                },
              });
          } else {
            this._populateDetails(inv.details);
            this.loading.set(false);
          }
        },
        error: () => {
          this.notify.error('載入請款資料失敗');
          this.loading.set(false);
          this.router.navigate(['/invoice']);
        },
      });
  }

  private _populateDetails(
    details: {
      invoiceDetailId: string | null;
      itemId: string | null;
      invoiceType: number | null;
      invoiceDate: string | null;
      invoiceNumber: string;
      price: number | null;
      remark: string;
    }[]
  ): void {
    this.details.clear();
    for (const d of details) {
      this.details.push(
        this.fb.group({
          invoiceDetailId: [d.invoiceDetailId],
          itemId: [d.itemId],
          invoiceType: [d.invoiceType ?? 0],
          invoiceDate: [
            d.invoiceDate ? d.invoiceDate.substring(0, 10) : getTodayString(),
          ],
          invoiceNumber: [d.invoiceNumber ?? ''],
          price: [d.price, Validators.required],
          remark: [d.remark ?? ''],
        })
      );
    }
  }

  // ─── Computed totals ──────────────────────────────────────────────────────
  get subtotal(): number {
    return this.details.controls.reduce((sum, ctrl) => {
      const price = ctrl.get('price')?.value;
      return sum + (Number(price) || 0);
    }, 0);
  }

  // ─── Save ─────────────────────────────────────────────────────────────────
  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.value;
    const dto = {
      customerId: v.customerId ? Number(v.customerId) : null,
      requestDate: v.requestDate || null,
      remark: v.remark?.trim() || '',
      status: Number(v.status ?? 0),
      details: (v.details ?? []).map((d: any) => ({
        invoiceDetailId: d.invoiceDetailId || null,
        itemId: d.itemId || null,
        invoiceType: d.invoiceType !== null ? Number(d.invoiceType) : null,
        invoiceDate: d.invoiceDate || null,
        invoiceNumber: d.invoiceNumber?.trim() || '',
        price: d.price !== null && d.price !== '' ? Number(d.price) : null,
        remark: d.remark?.trim() || '',
      })),
    };

    this.saving.set(true);
    const id = this.invoiceId();
    const request$ = id
      ? this.invoiceApi.update(id, dto)
      : this.invoiceApi.create(dto);

    request$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notify.success(id ? '請款更新成功' : '請款新增成功');
          this.saving.set(false);
          this.router.navigate(['/invoice']);
        },
        error: () => {
          this.notify.error(id ? '更新請款失敗' : '新增請款失敗');
          this.saving.set(false);
        },
      });
  }

  onCancel(): void {
    this.router.navigate(['/invoice']);
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────
  get requestDateControl() {
    return this.form.get('requestDate')!;
  }

  get customerIdInvalid(): boolean {
    const ctrl = this.form.get('customerId')!;
    return ctrl.invalid && ctrl.touched;
  }

  get requestDateInvalid(): boolean {
    return this.requestDateControl.invalid && this.requestDateControl.touched;
  }

  isDetailPriceInvalid(index: number): boolean {
    const ctrl = this.details.at(index).get('price')!;
    return ctrl.invalid && ctrl.touched;
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('zh-TW').format(value);
  }
}
