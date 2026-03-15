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
import { CustomerTypeListItem, CustomerTypeCreateUpdate } from '../../models/customer-type.model';

@Component({
  selector: 'app-customer-type-form-dialog',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './customer-type-form-dialog.component.html',
  styleUrl: './customer-type-form-dialog.component.scss',
})
export class CustomerTypeFormDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);

  // ─── Inputs ───────────────────────────────────────────────────────────────
  readonly type = input<CustomerTypeListItem | null>(null);
  readonly saving = input<boolean>(false);

  // ─── Outputs ──────────────────────────────────────────────────────────────
  readonly saved = output<CustomerTypeCreateUpdate>();
  readonly cancelled = output<void>();

  // ─── Form ─────────────────────────────────────────────────────────────────
  form!: FormGroup;

  readonly isEditMode = computed(() => this.type() !== null);
  readonly dialogTitle = computed(() =>
    this.isEditMode() ? '編輯客戶分類' : '新增客戶分類'
  );

  constructor() {
    effect(() => {
      const t = this.type();
      if (this.form) {
        this.form.patchValue({ title: t?.title ?? '' });
      }
    });
  }

  ngOnInit(): void {
    const t = this.type();
    this.form = this.fb.group({
      title: [t?.title ?? '', [Validators.required, Validators.maxLength(50)]],
    });
  }

  // ─── Handlers ─────────────────────────────────────────────────────────────
  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const dto: CustomerTypeCreateUpdate = {
      title: this.form.value.title.trim(),
    };
    this.saved.emit(dto);
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────
  get titleControl() {
    return this.form.get('title')!;
  }

  get titleInvalid(): boolean {
    return this.titleControl.invalid && this.titleControl.touched;
  }
}
