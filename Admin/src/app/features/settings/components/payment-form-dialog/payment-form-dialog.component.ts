import {
  Component,
  input,
  output,
  computed,
  effect,
  inject,
  OnInit,
} from '@angular/core';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { PaymentListItem, PaymentCreateUpdate } from '../../models/payment.model';

@Component({
  selector: 'app-payment-form-dialog',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './payment-form-dialog.component.html',
  styleUrl: './payment-form-dialog.component.scss',
})
export class PaymentFormDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);

  // ─── Inputs ───────────────────────────────────────────────────────────────
  readonly payment = input<PaymentListItem | null>(null);
  readonly saving = input<boolean>(false);

  // ─── Outputs ──────────────────────────────────────────────────────────────
  readonly saved = output<PaymentCreateUpdate>();
  readonly cancelled = output<void>();

  // ─── Form ─────────────────────────────────────────────────────────────────
  form!: FormGroup;

  readonly isEditMode = computed(() => this.payment() !== null);
  readonly dialogTitle = computed(() =>
    this.isEditMode() ? '編輯付款條件' : '新增付款條件'
  );

  constructor() {
    effect(() => {
      const p = this.payment();
      if (this.form) {
        this.form.patchValue({ remark: p?.remark ?? '' });
      }
    });
  }

  ngOnInit(): void {
    const p = this.payment();
    this.form = this.fb.group({
      remark: [p?.remark ?? '', [Validators.required, Validators.maxLength(500)]],
    });
  }

  // ─── Handlers ─────────────────────────────────────────────────────────────
  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const dto: PaymentCreateUpdate = {
      remark: this.form.value.remark.trim(),
    };
    this.saved.emit(dto);
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────
  get remarkControl() {
    return this.form.get('remark')!;
  }

  get remarkInvalid(): boolean {
    return this.remarkControl.invalid && this.remarkControl.touched;
  }
}
