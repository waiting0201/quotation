import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { UserFacade } from '../../facades/user.facade';
import { GroupFacade } from '../../facades/group.facade';
import { UserListItem, UserCreate, UserUpdate, UserPasswordChange } from '../../models/user.model';
import { UserFormDialogComponent } from '../../components/user-form-dialog/user-form-dialog.component';

/** 刪除對話框狀態 */
interface DeleteDialogState {
  open: boolean;
  userId: string;
  userName: string;
}

/** 密碼變更對話框狀態 */
interface PasswordDialogState {
  open: boolean;
  userId: string;
  userName: string;
}

/** 分頁設定 */
const PAGE_SIZE = 20;

@Component({
  selector: 'app-users-page',
  standalone: true,
  imports: [UserFormDialogComponent, ReactiveFormsModule],
  templateUrl: './users-page.component.html',
  styleUrl: './users-page.component.scss',
})
export class UsersPageComponent implements OnInit {
  private readonly facade = inject(UserFacade);
  private readonly groupFacade = inject(GroupFacade);
  private readonly fb = inject(FormBuilder);

  // ─── Facade signals ───────────────────────────────────────────────────────
  readonly users = this.facade.users;
  readonly loading = this.facade.loading;
  readonly saving = this.facade.saving;
  readonly selectedUser = this.facade.selectedUser;
  readonly permissionTree = this.facade.permissionTree;
  readonly groups = this.groupFacade.groups;

  // ─── Dialog state ─────────────────────────────────────────────────────────
  readonly formDialogOpen = signal(false);
  readonly editingUserId = signal<string | null>(null);

  readonly deleteDialog = signal<DeleteDialogState>({
    open: false,
    userId: '',
    userName: '',
  });

  readonly passwordDialog = signal<PasswordDialogState>({
    open: false,
    userId: '',
    userName: '',
  });

  // ─── Password form ────────────────────────────────────────────────────────
  readonly passwordForm = this.fb.group({
    newPassword: ['', [Validators.required, Validators.minLength(4)]],
    confirmPassword: ['', [Validators.required]],
  });

  // ─── Pagination ───────────────────────────────────────────────────────────
  readonly currentPage = signal(1);
  readonly pageSize = PAGE_SIZE;

  readonly totalCount = computed(() => this.users().length);
  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize))
  );
  readonly paginatedUsers = computed(() => {
    const page = this.currentPage();
    const start = (page - 1) * this.pageSize;
    return this.users().slice(start, start + this.pageSize);
  });
  readonly pageNumbers = computed(() =>
    Array.from({ length: this.totalPages() }, (_, i) => i + 1)
  );

  // ─── Lifecycle ───────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.facade.loadUsers();
    this.facade.loadPermissionTree();
    this.groupFacade.loadGroups();
  }

  // ─── Create / Edit ────────────────────────────────────────────────────────
  openCreateDialog(): void {
    this.editingUserId.set(null);
    this.facade.clearSelectedUser();
    this.formDialogOpen.set(true);
  }

  openEditDialog(user: UserListItem): void {
    this.editingUserId.set(user.userId);
    this.facade.loadUserDetail(user.userId).subscribe((detail) => {
      if (detail) {
        this.formDialogOpen.set(true);
      }
    });
  }

  closeFormDialog(): void {
    this.formDialogOpen.set(false);
    this.editingUserId.set(null);
    this.facade.clearSelectedUser();
  }

  onFormSaved(dto: UserCreate | UserUpdate): void {
    const id = this.editingUserId();
    const obs = id
      ? this.facade.updateUser(id, dto as UserUpdate)
      : this.facade.createUser(dto as UserCreate);

    obs.subscribe((ok) => {
      if (ok) {
        this.closeFormDialog();
        if (this.currentPage() > this.totalPages()) {
          this.currentPage.set(1);
        }
      }
    });
  }

  // ─── Delete ───────────────────────────────────────────────────────────────
  openDeleteDialog(user: UserListItem): void {
    this.deleteDialog.set({
      open: true,
      userId: user.userId,
      userName: user.name,
    });
  }

  closeDeleteDialog(): void {
    this.deleteDialog.update((d) => ({ ...d, open: false }));
  }

  confirmDelete(): void {
    const { userId } = this.deleteDialog();
    this.facade.deleteUser(userId).subscribe((ok) => {
      if (ok) {
        this.closeDeleteDialog();
        if (this.currentPage() > this.totalPages()) {
          this.currentPage.set(1);
        }
      }
    });
  }

  // ─── Password change ─────────────────────────────────────────────────────
  openPasswordDialog(user: UserListItem): void {
    this.passwordForm.reset();
    this.passwordDialog.set({
      open: true,
      userId: user.userId,
      userName: user.name,
    });
  }

  closePasswordDialog(): void {
    this.passwordDialog.update((d) => ({ ...d, open: false }));
    this.passwordForm.reset();
  }

  confirmPasswordChange(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    const { newPassword, confirmPassword } = this.passwordForm.value;
    if (newPassword !== confirmPassword) {
      // 以訊息提示密碼不一致（不寫入 store，只作 UI 提示）
      this.passwordForm.get('confirmPassword')?.setErrors({ mismatch: true });
      return;
    }

    const { userId } = this.passwordDialog();
    const dto: UserPasswordChange = { newPassword: newPassword! };

    this.facade.changePassword(userId, dto).subscribe((ok) => {
      if (ok) {
        this.closePasswordDialog();
      }
    });
  }

  // ─── Pagination ───────────────────────────────────────────────────────────
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────
  formatDate(dateStr: string | null): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return '—';
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    const hh = String(d.getHours()).padStart(2, '0');
    const mm = String(d.getMinutes()).padStart(2, '0');
    return `${y}/${m}/${day} ${hh}:${mm}`;
  }
}
