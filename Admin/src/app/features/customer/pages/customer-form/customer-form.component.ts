import {
  Component,
  inject,
  signal,
  computed,
  OnInit,
  DestroyRef,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { CustomerApiService } from '../../services/customer-api.service';
import { CustomerTypeApiService } from '../../services/customer-type-api.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { ApiResponse } from '../../../../core/models/api-response.model';
import { CustomerTypeListItem } from '../../models/customer-type.model';

interface LookupItem {
  id: number;
  title: string;
}

@Component({
  selector: 'app-customer-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './customer-form.component.html',
  styleUrl: './customer-form.component.scss',
})
export class CustomerFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);
  private readonly customerApi = inject(CustomerApiService);
  private readonly customerTypeApi = inject(CustomerTypeApiService);
  private readonly notify = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  // ─── State ────────────────────────────────────────────────────────────────
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly customerId = signal<number | null>(null);
  readonly customerTypes = signal<CustomerTypeListItem[]>([]);
  readonly countries = signal<LookupItem[]>([]);

  readonly isEditMode = computed(() => this.customerId() !== null);
  readonly pageTitle = computed(() => this.isEditMode() ? '編輯客戶' : '新增客戶');

  // ─── Form ─────────────────────────────────────────────────────────────────
  form!: FormGroup;

  ngOnInit(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      customerTypeId: [null as number | null],
      countryId: [null as number | null],
      address: ['', [Validators.maxLength(500)]],
      phone: ['', [Validators.maxLength(50)]],
      fax: ['', [Validators.maxLength(50)]],
      vatNumber: ['', [Validators.maxLength(50)]],
      contacts: this.fb.array([]),
    });

    // 先載入所有下拉選單資料，完成後再載入客戶資料（編輯模式）
    // 確保 patchValue 時 <select> 的 <option> 已存在
    this.loading.set(true);
    this._loadLookups(() => {
      const idParam = this.route.snapshot.paramMap.get('id');
      if (idParam) {
        const id = Number(idParam);
        if (!isNaN(id) && id > 0) {
          this.customerId.set(id);
          this._loadCustomer(id);
          return;
        }
      }
      this.loading.set(false);
    });
  }

  // ─── Contacts FormArray ───────────────────────────────────────────────────
  get contacts(): FormArray {
    return this.form.get('contacts') as FormArray;
  }

  addContact(): void {
    this.contacts.push(this.fb.group({
      contactId: [null],
      name: [''],
      email: [''],
      phone: [''],
      ext: [''],
    }));
  }

  removeContact(index: number): void {
    this.contacts.removeAt(index);
  }

  // ─── Load data ────────────────────────────────────────────────────────────
  private _loadLookups(onComplete: () => void): void {
    forkJoin({
      types: this.customerTypeApi.getAll(),
      countries: this.http.get<ApiResponse<LookupItem[]>>('/api/lookups/countries'),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ types, countries }) => {
          this.customerTypes.set(types.data);
          this.countries.set(countries.data);
          onComplete();
        },
        error: () => {
          this.notify.error('載入選單資料失敗');
          this.loading.set(false);
        },
      });
  }

  private _loadCustomer(id: number): void {
    this.loading.set(true);
    this.customerApi.getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          const c = res.data;
          this.form.patchValue({
            name: c.name,
            customerTypeId: c.customerTypeId,
            countryId: c.countryId,
            address: c.address ?? '',
            phone: c.phone ?? '',
            fax: c.fax ?? '',
            vatNumber: c.vatNumber ?? '',
          });

          this.contacts.clear();
          for (const contact of c.contacts) {
            this.contacts.push(this.fb.group({
              contactId: [contact.contactId],
              name: [contact.name ?? ''],
              email: [contact.email ?? ''],
              phone: [contact.phone ?? ''],
              ext: [contact.ext ?? ''],
            }));
          }

          this.loading.set(false);
        },
        error: () => {
          this.notify.error('載入客戶資料失敗');
          this.loading.set(false);
          this.router.navigate(['/customer']);
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
      name: v.name.trim(),
      address: v.address?.trim() || null,
      customerTypeId: v.customerTypeId || null,
      countryId: v.countryId || null,
      phone: v.phone?.trim() || null,
      fax: v.fax?.trim() || null,
      vatNumber: v.vatNumber?.trim() || null,
      contacts: v.contacts?.map((c: any) => ({
        contactId: c.contactId || null,
        name: c.name?.trim() || null,
        email: c.email?.trim() || null,
        phone: c.phone?.trim() || null,
        ext: c.ext?.trim() || null,
      })) ?? null,
    };

    this.saving.set(true);
    const id = this.customerId();
    const request$ = id
      ? this.customerApi.update(id, dto)
      : this.customerApi.create(dto);

    request$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notify.success(id ? '客戶更新成功' : '客戶新增成功');
          this.saving.set(false);
          this.router.navigate(['/customer']);
        },
        error: () => {
          this.notify.error(id ? '更新客戶失敗' : '新增客戶失敗');
          this.saving.set(false);
        },
      });
  }

  onCancel(): void {
    this.router.navigate(['/customer']);
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────
  get nameControl() {
    return this.form.get('name')!;
  }

  get nameInvalid(): boolean {
    return this.nameControl.invalid && this.nameControl.touched;
  }
}
