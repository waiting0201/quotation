import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { GroupFacade } from '../../facades/group.facade';
import { GroupListItem, GroupCreateUpdate } from '../../models/group.model';
import { GroupFormDialogComponent } from '../../components/group-form-dialog/group-form-dialog.component';

/** 刪除對話框的狀態 */
interface DeleteDialogState {
  open: boolean;
  groupId: string;
  groupTitle: string;
  hasUsers: boolean;
  userCount: number;
}

/** 分頁設定 */
const PAGE_SIZE = 20;

@Component({
  selector: 'app-groups-page',
  standalone: true,
  imports: [GroupFormDialogComponent],
  templateUrl: './groups-page.component.html',
  styleUrl: './groups-page.component.scss',
})
export class GroupsPageComponent implements OnInit {
  private readonly facade = inject(GroupFacade);

  // ─── Facade signals ───────────────────────────────────────────────────────
  readonly groups = this.facade.groups;
  readonly loading = this.facade.loading;
  readonly saving = this.facade.saving;
  readonly selectedGroup = this.facade.selectedGroup;
  readonly permissionTree = this.facade.permissionTree;

  // ─── Dialog state ─────────────────────────────────────────────────────────
  readonly formDialogOpen = signal(false);
  readonly editingGroupId = signal<string | null>(null);

  readonly deleteDialog = signal<DeleteDialogState>({
    open: false,
    groupId: '',
    groupTitle: '',
    hasUsers: false,
    userCount: 0,
  });

  // ─── Pagination ───────────────────────────────────────────────────────────
  readonly currentPage = signal(1);
  readonly pageSize = PAGE_SIZE;

  readonly totalCount = this.facade.totalCount;
  readonly totalPages = this.facade.totalPages;
  readonly pageNumbers = computed(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const maxVisible = 5;
    let start = Math.max(1, current - Math.floor(maxVisible / 2));
    let end = start + maxVisible - 1;
    if (end > total) {
      end = total;
      start = Math.max(1, end - maxVisible + 1);
    }
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  });

  // ─── Lifecycle ───────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.facade.loadGroups(1, this.pageSize);
    this.facade.loadPermissionTree();
  }

  // ─── Create / Edit ────────────────────────────────────────────────────────
  openCreateDialog(): void {
    this.editingGroupId.set(null);
    this.facade.clearSelectedGroup();
    this.formDialogOpen.set(true);
  }

  openEditDialog(group: GroupListItem): void {
    this.editingGroupId.set(group.groupId);
    this.facade.loadGroupDetail(group.groupId).subscribe((detail) => {
      if (detail) {
        this.formDialogOpen.set(true);
      }
    });
  }

  closeFormDialog(): void {
    this.formDialogOpen.set(false);
    this.editingGroupId.set(null);
    this.facade.clearSelectedGroup();
  }

  onFormSaved(dto: GroupCreateUpdate): void {
    const id = this.editingGroupId();
    const obs = id
      ? this.facade.updateGroup(id, dto, this.currentPage(), this.pageSize)
      : this.facade.createGroup(dto, this.currentPage(), this.pageSize);

    obs.subscribe((ok) => {
      if (ok) {
        this.closeFormDialog();
      }
    });
  }

  // ─── Delete ───────────────────────────────────────────────────────────────
  openDeleteDialog(group: GroupListItem): void {
    this.deleteDialog.set({
      open: true,
      groupId: group.groupId,
      groupTitle: group.title,
      hasUsers: group.userCount > 0,
      userCount: group.userCount,
    });
  }

  closeDeleteDialog(): void {
    this.deleteDialog.update((d) => ({ ...d, open: false }));
  }

  confirmDelete(): void {
    const { groupId } = this.deleteDialog();
    this.facade.deleteGroup(groupId, this.currentPage(), this.pageSize).subscribe((ok) => {
      if (ok) {
        this.closeDeleteDialog();
      }
    });
  }

  // ─── Pagination ───────────────────────────────────────────────────────────
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.facade.loadGroups(page, this.pageSize);
  }
}
