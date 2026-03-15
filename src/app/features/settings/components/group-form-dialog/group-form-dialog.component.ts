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
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { PermissionMatrixComponent } from '../permission-matrix/permission-matrix.component';
import { GroupDetail, GroupCreateUpdate, GroupPermission, PermissionNode } from '../../models/group.model';

@Component({
  selector: 'app-group-form-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, PermissionMatrixComponent],
  templateUrl: './group-form-dialog.component.html',
  styleUrl: './group-form-dialog.component.scss',
})
export class GroupFormDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);

  // ─── Inputs ───────────────────────────────────────────────────────────────
  /** null = 新增模式，有值 = 編輯模式 */
  readonly group = input<GroupDetail | null>(null);
  readonly permissionTree = input<PermissionNode[]>([]);
  readonly saving = input<boolean>(false);

  // ─── Outputs ──────────────────────────────────────────────────────────────
  readonly saved = output<GroupCreateUpdate>();
  readonly cancelled = output<void>();

  // ─── Form ─────────────────────────────────────────────────────────────────
  form!: FormGroup;

  // ─── Internal state ───────────────────────────────────────────────────────
  readonly currentPermissions = signal<GroupPermission[]>([]);

  readonly isEditMode = computed(() => this.group() !== null);
  readonly dialogTitle = computed(() =>
    this.isEditMode() ? '編輯群組' : '新增群組'
  );

  constructor() {
    // 當 group input 變動時，重設表單
    effect(() => {
      const g = this.group();
      if (this.form) {
        this.form.patchValue({ title: g?.title ?? '' });
      }
      this.currentPermissions.set(g?.permissions ?? []);
    });
  }

  ngOnInit(): void {
    const g = this.group();
    this.form = this.fb.group({
      title: [g?.title ?? '', [Validators.required, Validators.maxLength(50)]],
    });
    this.currentPermissions.set(g?.permissions ?? []);
  }

  // ─── Handlers ─────────────────────────────────────────────────────────────

  onPermissionsChange(perms: GroupPermission[]): void {
    this.currentPermissions.set(perms);
  }

  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const dto: GroupCreateUpdate = {
      title: this.form.value.title.trim(),
      permissions: this.currentPermissions(),
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
