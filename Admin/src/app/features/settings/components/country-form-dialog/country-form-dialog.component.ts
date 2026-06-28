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
import { CountryListItem, CountryCreateUpdate } from '../../models/country.model';

@Component({
  selector: 'app-country-form-dialog',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './country-form-dialog.component.html',
  styleUrl: './country-form-dialog.component.scss',
})
export class CountryFormDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);

  // ─── Inputs ───────────────────────────────────────────────────────────────
  readonly country = input<CountryListItem | null>(null);
  readonly saving = input<boolean>(false);

  // ─── Outputs ──────────────────────────────────────────────────────────────
  readonly saved = output<CountryCreateUpdate>();
  readonly cancelled = output<void>();

  // ─── Form ─────────────────────────────────────────────────────────────────
  form!: FormGroup;

  readonly isEditMode = computed(() => this.country() !== null);
  readonly dialogTitle = computed(() =>
    this.isEditMode() ? '編輯國家' : '新增國家'
  );

  constructor() {
    effect(() => {
      const c = this.country();
      if (this.form) {
        this.form.patchValue({ title: c?.title ?? '' });
      }
    });
  }

  ngOnInit(): void {
    const c = this.country();
    this.form = this.fb.group({
      title: [c?.title ?? '', [Validators.required, Validators.maxLength(50)]],
    });
  }

  // ─── Handlers ─────────────────────────────────────────────────────────────
  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const dto: CountryCreateUpdate = {
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
