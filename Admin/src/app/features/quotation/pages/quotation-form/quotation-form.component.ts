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
import { QuotationApiService } from '../../services/quotation-api.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { CustomerLookup, ContactLookup } from '../../models/quotation.model';

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

/** 稅務計算：taxType 0=稅外加, 1=稅內含, 2=免稅 */
function calcTax(pretaxTotal: number, taxType: number): { tax: number; grandTotal: number } {
  switch (taxType) {
    case 0: { // 稅外加：total = 未稅 + 稅（分開捨入，與後端 subtotal + round(subtotal * 0.05) 一致）
      const tax = Math.round(pretaxTotal * 0.05);
      return { tax, grandTotal: pretaxTotal + tax };
    }
    case 1: // 稅內含：已含稅，反推稅額
      return {
        tax: Math.round(pretaxTotal - pretaxTotal / 1.05),
        grandTotal: pretaxTotal,
      };
    case 2: // 免稅
      return { tax: 0, grandTotal: pretaxTotal };
    default:
      return { tax: 0, grandTotal: pretaxTotal };
  }
}

@Component({
  selector: 'app-quotation-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './quotation-form.component.html',
  styleUrl: './quotation-form.component.scss',
})
export class QuotationFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly quotationApi = inject(QuotationApiService);
  private readonly notify = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  // ─── State ────────────────────────────────────────────────────────────────
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly itemId = signal<string | null>(null);
  readonly customers = signal<CustomerLookup[]>([]);
  readonly contacts = signal<ContactLookup[]>([]);
  readonly contactsLoading = signal(false);

  readonly isEditMode = computed(() => this.itemId() !== null);
  readonly pageTitle = computed(() => this.isEditMode() ? '編輯報價' : '新增報價');

  // ─── 付款範本 ────────────────────────────────────────────────────────────
  readonly paymentTemplates = signal<{ paymentId: number; remark: string }[]>([]);
  readonly paymentPanelOpen = signal(false);

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

  // ─── 即時金額計算 — 用手動 signal 同步（不可用 computed() 追蹤 FormControl.value）
  // 每次 form 異動後手動呼叫 _recalcTotals()
  readonly detailsSubtotal = signal(0);
  readonly discountPercent = signal(0);
  readonly discountAmount = signal(0);
  /** 折後小計（未稅） */
  readonly pretaxTotal = computed(() => this.detailsSubtotal() - this.discountAmount());
  readonly taxAmount = signal(0);
  readonly grandTotal = signal(0);

  // ─── Form ─────────────────────────────────────────────────────────────────
  form!: FormGroup;

  ngOnInit(): void {
    const today = getTodayString();

    this.form = this.fb.group({
      customerId: [null as number | null, Validators.required],
      customerDetailId: [null as string | null, Validators.required],
      name: ['', Validators.required],
      quotationDate: [today, Validators.required],
      expireDate: [null as string | null, Validators.required],
      taxType: [0],
      discount: [0, [Validators.min(0), Validators.max(100), Validators.pattern(/^\d+$/)]],
      workdays: [null as number | null],
      status: [0],
      payment: ['', Validators.required],
      remark: [''],
      details: this.fb.array([]),
    });

    // 監聽 taxType / discount 變動即時重算
    this.form.get('taxType')!.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this._recalcTotals());
    this.form.get('discount')!.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this._recalcTotals());

    // 載入付款範本（非阻塞）
    this._loadPaymentTemplates();

    // 先載入客戶下拉，完成後再載入報價資料（編輯模式）
    this.loading.set(true);
    this._loadCustomers(() => {
      const idParam = this.route.snapshot.paramMap.get('id');
      if (idParam) {
        this.itemId.set(idParam);
        this._loadQuotation(idParam);
        return;
      }
      this.loading.set(false);
      // 新增模式預設一個空白明細
      this.addDetail();
    });
  }

  // ─── Details FormArray ────────────────────────────────────────────────────
  get details(): FormArray {
    return this.form.get('details') as FormArray;
  }

  addDetail(): void {
    const group = this.fb.group({
      itemDetailId: [null as string | null],
      title: [''],
      description: [''],
      quantity: [1],
      price: [null as number | null],
      freq: [0],
    });
    group.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this._recalcTotals());
    this.details.push(group);
    this._recalcTotals();
  }

  removeDetail(index: number): void {
    this.details.removeAt(index);
    this._recalcTotals();
  }

  // ─── 重算總金額 ───────────────────────────────────────────────────────────
  private _recalcTotals(): void {
    // 與後端一致：小計 = Σ(數量 × 單價)
    const detailsSum = this.details.controls.reduce((sum, ctrl) => {
      const qty = Number(ctrl.get('quantity')?.value) || 1;
      const price = Number(ctrl.get('price')?.value) || 0;
      return sum + qty * price;
    }, 0);

    const discount = Math.min(100, Math.max(0, Math.trunc(Number(this.form.get('discount')?.value) || 0)));
    const discountAmount = Math.round(detailsSum * discount / 100);
    const taxType = Number(this.form.get('taxType')?.value ?? 0);
    const { tax, grandTotal } = calcTax(detailsSum - discountAmount, taxType);

    this.detailsSubtotal.set(detailsSum);
    this.discountPercent.set(discount);
    this.discountAmount.set(discountAmount);
    this.taxAmount.set(tax);
    this.grandTotal.set(grandTotal);
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
    this.form.get('customerDetailId')!.setValue(null);
    this.selectedCustomerName.set(customer.name);
    this.customerSearch.set('');
    this.customerDropdownOpen.set(false);
    this._loadContacts(customer.customerId);
  }

  clearCustomer(): void {
    this.form.get('customerId')!.setValue(null);
    this.form.get('customerDetailId')!.setValue(null);
    this.selectedCustomerName.set('');
    this.customerSearch.set('');
    this.contacts.set([]);
  }

  // ─── 付款範本 ────────────────────────────────────────────────────────────
  togglePaymentPanel(): void {
    this.paymentPanelOpen.update((v) => !v);
  }

  selectPaymentTemplate(tpl: { paymentId: number; remark: string }): void {
    this.form.get('payment')!.setValue(tpl.remark);
    this.paymentPanelOpen.set(false);
  }

  // ─── Load data ────────────────────────────────────────────────────────────
  private _loadCustomers(onComplete: () => void): void {
    this.quotationApi
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

  private _loadPaymentTemplates(): void {
    this.quotationApi
      .getPaymentTemplates()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.paymentTemplates.set(res.data ?? []),
      });
  }

  private _loadContacts(customerId: number): void {
    this.contactsLoading.set(true);
    this.quotationApi
      .getContactsByCustomer(customerId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (contacts) => {
          this.contacts.set(contacts);
          this.contactsLoading.set(false);
        },
        error: () => {
          this.contacts.set([]);
          this.contactsLoading.set(false);
        },
      });
  }

  private _loadQuotation(id: string): void {
    this.quotationApi
      .getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          const q = res.data;

          this.form.patchValue({
            customerId: q.customerId,
            customerDetailId: q.customerDetailId ?? null,
            name: q.name ?? '',
            quotationDate: q.quotationDate ? q.quotationDate.substring(0, 10) : getTodayString(),
            expireDate: q.expireDate ? q.expireDate.substring(0, 10) : null,
            taxType: q.taxType ?? 0,
            discount: q.discount ?? 0,
            workdays: q.workdays ?? null,
            status: q.status ?? 0,
            payment: q.payment ?? '',
            remark: q.remark ?? '',
          });
          this.selectedCustomerName.set(q.customerName ?? '');

          // 合併 details + contents 為統一明細
          // 舊系統資料可能存在 itemcontents，需併入顯示
          const mergedDetails = [
            ...(q.details ?? []).map((d: any) => ({
              itemDetailId: d.itemDetailId ?? null,
              title: d.title ?? '',
              description: d.description ?? '',
              quantity: d.quantity ?? 1,
              price: d.price ?? 0,
              freq: d.freq ?? 0,
            })),
            ...(q.contents ?? []).map((c: any) => ({
              itemDetailId: null,
              title: c.title ?? '',
              description: c.remark ?? '',
              quantity: 1,
              price: c.price ?? 0,
              freq: c.freq ?? 0,
            })),
          ];
          this._populateDetails(mergedDetails);

          // 載入聯絡人
          if (q.customerId) {
            this._loadContacts(q.customerId);
          }

          this._recalcTotals();
          this.loading.set(false);
        },
        error: () => {
          this.notify.error('載入報價資料失敗');
          this.loading.set(false);
          this.router.navigate(['/quotation']);
        },
      });
  }

  private _populateDetails(
    items: {
      itemDetailId: string | null;
      title: string;
      description: string;
      quantity: number;
      price: number;
      freq: number;
    }[]
  ): void {
    this.details.clear();
    for (const d of items) {
      const group = this.fb.group({
        itemDetailId: [d.itemDetailId],
        title: [d.title ?? ''],
        description: [d.description ?? ''],
        quantity: [d.quantity ?? 1],
        price: [d.price ?? null],
        freq: [d.freq ?? 0],
      });
      group.valueChanges
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(() => this._recalcTotals());
      this.details.push(group);
    }
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
      customerDetailId: v.customerDetailId || null,
      name: v.name?.trim() || '',
      quotationDate: v.quotationDate || getTodayString(),
      expireDate: v.expireDate || null,
      taxType: Number(v.taxType ?? 0),
      discount: Math.min(100, Math.max(0, Math.trunc(Number(v.discount) || 0))),
      payment: v.payment?.trim() || '',
      remark: v.remark?.trim() || '',
      workdays: v.workdays !== null && v.workdays !== '' ? Number(v.workdays) : null,
      status: Number(v.status ?? 0),
      details: (v.details ?? []).map((d: any) => ({
        itemDetailId: d.itemDetailId || null,
        title: d.title?.trim() || '',
        description: d.description?.trim() || '',
        quantity: Number(d.quantity) || 1,
        price: d.price !== null && d.price !== '' ? Number(d.price) : 0,
        freq: Number(d.freq ?? 0),
      })),
      contents: [],
    };

    this.saving.set(true);
    const id = this.itemId();
    const request$ = id
      ? this.quotationApi.update(id, dto)
      : this.quotationApi.create(dto);

    request$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notify.success(id ? '報價更新成功' : '報價新增成功');
          this.saving.set(false);
          this.router.navigate(['/quotation']);
        },
        error: () => {
          this.notify.error(id ? '更新報價失敗' : '新增報價失敗');
          this.saving.set(false);
        },
      });
  }

  onCancel(): void {
    this.router.navigate(['/quotation']);
  }

  // ─── Validation helpers ──────────────────────────────────────────────────
  get customerIdInvalid(): boolean {
    const ctrl = this.form.get('customerId')!;
    return ctrl.invalid && ctrl.touched;
  }

  get nameInvalid(): boolean {
    const ctrl = this.form.get('name')!;
    return ctrl.invalid && ctrl.touched;
  }

  get customerDetailIdInvalid(): boolean {
    const ctrl = this.form.get('customerDetailId')!;
    return ctrl.invalid && ctrl.touched;
  }

  get quotationDateInvalid(): boolean {
    const ctrl = this.form.get('quotationDate')!;
    return ctrl.invalid && ctrl.touched;
  }

  get expireDateInvalid(): boolean {
    const ctrl = this.form.get('expireDate')!;
    return ctrl.invalid && ctrl.touched;
  }

  get paymentInvalid(): boolean {
    const ctrl = this.form.get('payment')!;
    return ctrl.invalid && ctrl.touched;
  }

  get discountInvalid(): boolean {
    const ctrl = this.form.get('discount')!;
    return ctrl.invalid && ctrl.touched;
  }

  // ─── Format helpers ───────────────────────────────────────────────────────
  formatCurrency(value: number): string {
    return new Intl.NumberFormat('zh-TW').format(value);
  }
}
