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
  ValidationErrors,
} from '@angular/forms';
import { PermissionMatrixComponent } from '../permission-matrix/permission-matrix.component';
import {
  UserDetail,
  UserCreate,
  UserUpdate,
  UserPermission,
} from '../../models/user.model';
import { GroupListItem, PermissionNode, GroupPermission } from '../../models/group.model';

/** GroupPermission 與 UserPermission 結構相同，直接相容 */
type AnyPermission = GroupPermission;

@Component({
  selector: 'app-user-form-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, PermissionMatrixComponent],
  templateUrl: './user-form-dialog.component.html',
  styleUrl: './user-form-dialog.component.scss',
})
export class UserFormDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);

  // ─── Inputs ───────────────────────────────────────────────────────────────
  /** null = 新增模式，有值 = 編輯模式 */
  readonly user = input<UserDetail | null>(null);
  readonly groups = input<GroupListItem[]>([]);
  readonly permissionTree = input<PermissionNode[]>([]);
  readonly saving = input<boolean>(false);

  // ─── Outputs ──────────────────────────────────────────────────────────────
  readonly saved = output<UserCreate | UserUpdate>();
  readonly cancelled = output<void>();

  // ─── Form ─────────────────────────────────────────────────────────────────
  form!: FormGroup;

  // ─── Internal state ───────────────────────────────────────────────────────
  readonly currentPermissions = signal<UserPermission[]>([]);

  readonly isEditMode = computed(() => this.user() !== null);
  readonly dialogTitle = computed(() =>
    this.isEditMode() ? '編輯使用者' : '新增使用者'
  );

  constructor() {
    // 當 user input 變動時，重設表單
    effect(() => {
      const u = this.user();
      if (this.form) {
        this.form.patchValue({
          name: u?.name ?? '',
          email: u?.email ?? '',
          groupId: u?.groupId ?? '',
          status: u?.status ?? true,
        });
        // 編輯模式下密碼非必填
        const passwordCtrl = this.form.get('password');
        if (u !== null) {
          passwordCtrl?.clearValidators();
        } else {
          passwordCtrl?.setValidators([Validators.required, Validators.minLength(4)]);
        }
        passwordCtrl?.updateValueAndValidity();
      }
      this.currentPermissions.set(u?.permissions ?? []);
    });
  }

  ngOnInit(): void {
    const u = this.user();
    const isEdit = u !== null;

    this.form = this.fb.group({
      name: [u?.name ?? '', [Validators.required, Validators.maxLength(50)]],
      email: [u?.email ?? '', [Validators.required, Validators.email, Validators.maxLength(100)]],
      password: [
        '',
        isEdit
          ? []
          : [Validators.required, Validators.minLength(4)],
      ],
      groupId: [u?.groupId ?? ''],
      status: [u?.status ?? true],
    });
    this.currentPermissions.set(u?.permissions ?? []);
  }

  // ─── Handlers ─────────────────────────────────────────────────────────────

  onPermissionsChange(perms: AnyPermission[]): void {
    // AnyPermission 與 UserPermission 結構相同
    this.currentPermissions.set(perms as UserPermission[]);
  }

  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, email, password, groupId, status } = this.form.value;

    if (this.isEditMode()) {
      const dto: UserUpdate = {
        name: name.trim(),
        email: email.trim(),
        groupId: groupId || null,
        status,
        permissions: this.currentPermissions(),
      };
      this.saved.emit(dto);
    } else {
      const dto: UserCreate = {
        name: name.trim(),
        email: email.trim(),
        password,
        groupId: groupId || null,
        status,
        permissions: this.currentPermissions(),
      };
      this.saved.emit(dto);
    }
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────
  get nameControl(): AbstractControl {
    return this.form.get('name')!;
  }

  get emailControl(): AbstractControl {
    return this.form.get('email')!;
  }

  get passwordControl(): AbstractControl {
    return this.form.get('password')!;
  }

  get nameInvalid(): boolean {
    return this.nameControl.invalid && this.nameControl.touched;
  }

  get emailInvalid(): boolean {
    return this.emailControl.invalid && this.emailControl.touched;
  }

  get passwordInvalid(): boolean {
    return this.passwordControl.invalid && this.passwordControl.touched;
  }
}
