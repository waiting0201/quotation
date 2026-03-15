import {
  Component,
  input,
  output,
  signal,
  computed,
  effect,
  inject,
  OnInit,
} from '@angular/core';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
  AbstractControl,
} from '@angular/forms';
import { Host, HostCreateUpdate } from '../../models/host.model';

@Component({
  selector: 'app-host-form-dialog',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './host-form-dialog.component.html',
  styleUrl: './host-form-dialog.component.scss',
})
export class HostFormDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);

  // ─── Inputs ───────────────────────────────────────────────────────────────
  /** null = 新增模式，有值 = 編輯模式 */
  readonly host = input<Host | null>(null);
  readonly saving = input<boolean>(false);

  // ─── Outputs ──────────────────────────────────────────────────────────────
  readonly saved = output<HostCreateUpdate>();
  readonly cancelled = output<void>();

  // ─── Form ─────────────────────────────────────────────────────────────────
  form!: FormGroup;

  // ─── Computed ─────────────────────────────────────────────────────────────
  readonly isEditMode = computed(() => this.host() !== null);
  readonly dialogTitle = computed(() =>
    this.isEditMode() ? '編輯維護項目' : '新增維護項目'
  );

  constructor() {
    // 當 host input 變動時，重設表單
    effect(() => {
      const h = this.host();
      if (this.form) {
        this.form.patchValue({
          item: h?.item ?? '',
          url: h?.url ?? '',
          startDate: this._toInputDate(h?.startDate ?? null),
          expireDate: this._toInputDate(h?.expireDate ?? null),
        });
      }
    });
  }

  ngOnInit(): void {
    const h = this.host();
    this.form = this.fb.group({
      item: [h?.item ?? '', [Validators.required, Validators.maxLength(200)]],
      url: [h?.url ?? '', [Validators.maxLength(500)]],
      startDate: [this._toInputDate(h?.startDate ?? null)],
      expireDate: [this._toInputDate(h?.expireDate ?? null)],
    });
  }

  // ─── Handlers ─────────────────────────────────────────────────────────────
  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { item, url, startDate, expireDate } = this.form.value;

    const dto: HostCreateUpdate = {
      item: item.trim(),
      url: url?.trim() || null,
      startDate: startDate || null,
      expireDate: expireDate || null,
    };
    this.saved.emit(dto);
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  // ─── Getters ──────────────────────────────────────────────────────────────
  get itemControl(): AbstractControl {
    return this.form.get('item')!;
  }

  get urlControl(): AbstractControl {
    return this.form.get('url')!;
  }

  get itemInvalid(): boolean {
    return this.itemControl.invalid && this.itemControl.touched;
  }

  get urlInvalid(): boolean {
    return this.urlControl.invalid && this.urlControl.touched;
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────
  /** 將 ISO 日期字串轉換為 input[type=date] 所需的 YYYY-MM-DD 格式 */
  private _toInputDate(value: string | null): string {
    if (!value) return '';
    // 取前 10 個字元（YYYY-MM-DD）
    return value.substring(0, 10);
  }
}
