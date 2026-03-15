import {
  Component,
  inject,
  signal,
  computed,
  OnInit,
  DestroyRef,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { HostApiService } from '../../services/host-api.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { Host, HostCreateUpdate } from '../../models/host.model';
import { HostFormDialogComponent } from '../../components/host-form-dialog/host-form-dialog.component';

/** 刪除對話框狀態 */
interface DeleteDialogState {
  open: boolean;
  hostId: number;
  itemName: string;
}

/** 到期狀態類型 */
export type ExpiryStatus = 'normal' | 'warning' | 'expired' | 'unset';

/** 分頁設定 */
const PAGE_SIZE = 20;

@Component({
  selector: 'app-host-list',
  standalone: true,
  imports: [FormsModule, HostFormDialogComponent],
  templateUrl: './host-list.component.html',
  styleUrl: './host-list.component.scss',
})
export class HostListComponent implements OnInit {
  private readonly api = inject(HostApiService);
  private readonly notify = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  // ─── State ────────────────────────────────────────────────────────────────
  readonly hosts = signal<Host[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly selectedHost = signal<Host | null>(null);

  // ─── Search ───────────────────────────────────────────────────────────────
  readonly searchQuery = signal('');
  private readonly _searchInput$ = new Subject<string>();

  // ─── Dialog state ─────────────────────────────────────────────────────────
  readonly formDialogOpen = signal(false);
  readonly editingHostId = signal<number | null>(null);

  readonly deleteDialog = signal<DeleteDialogState>({
    open: false,
    hostId: 0,
    itemName: '',
  });

  // ─── Pagination ───────────────────────────────────────────────────────────
  readonly currentPage = signal(1);
  readonly pageSize = PAGE_SIZE;

  readonly totalCount = signal(0);
  readonly totalPages = signal(1);
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
    // 設置搜尋 debounce
    this._searchInput$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((query) => {
        this.searchQuery.set(query);
        this.currentPage.set(1);
        this._loadHosts(query);
      });

    // 初始載入
    this._loadHosts();
  }

  // ─── Load ─────────────────────────────────────────────────────────────────
  private _loadHosts(search?: string): void {
    this.loading.set(true);
    this.api.getList(this.currentPage(), this.pageSize, search)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.hosts.set(res.data);
          this.totalCount.set(res.pagination.totalCount);
          this.totalPages.set(res.pagination.totalPages);
          this.loading.set(false);
        },
        error: () => {
          this.notify.error('載入維護清單失敗');
          this.loading.set(false);
        },
      });
  }

  // ─── Search handler ───────────────────────────────────────────────────────
  onSearchInput(value: string): void {
    this._searchInput$.next(value);
  }

  // ─── Create / Edit ────────────────────────────────────────────────────────
  openCreateDialog(): void {
    this.editingHostId.set(null);
    this.selectedHost.set(null);
    this.formDialogOpen.set(true);
  }

  openEditDialog(host: Host): void {
    this.editingHostId.set(host.hostId);
    this.selectedHost.set(host);
    this.formDialogOpen.set(true);
  }

  closeFormDialog(): void {
    this.formDialogOpen.set(false);
    this.editingHostId.set(null);
    this.selectedHost.set(null);
  }

  onFormSaved(dto: HostCreateUpdate): void {
    const id = this.editingHostId();
    this.saving.set(true);

    const request$ = id
      ? this.api.update(id, dto)
      : this.api.create(dto);

    request$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notify.success(id ? '維護項目已更新' : '維護項目已新增');
          this.saving.set(false);
          this.closeFormDialog();
          this._loadHosts(this.searchQuery());
        },
        error: () => {
          this.notify.error(id ? '更新失敗，請稍後再試' : '新增失敗，請稍後再試');
          this.saving.set(false);
        },
      });
  }

  // ─── Delete ───────────────────────────────────────────────────────────────
  openDeleteDialog(host: Host): void {
    this.deleteDialog.set({
      open: true,
      hostId: host.hostId,
      itemName: host.item,
    });
  }

  closeDeleteDialog(): void {
    this.deleteDialog.update((d) => ({ ...d, open: false }));
  }

  confirmDelete(): void {
    const { hostId } = this.deleteDialog();
    this.loading.set(true);
    this.api.delete(hostId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notify.success('維護項目已刪除');
          this.closeDeleteDialog();
          this._loadHosts(this.searchQuery());
        },
        error: () => {
          this.notify.error('刪除失敗，請稍後再試');
          this.loading.set(false);
          this.closeDeleteDialog();
        },
      });
  }

  // ─── Pagination ───────────────────────────────────────────────────────────
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this._loadHosts(this.searchQuery());
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────

  /**
   * 計算到期狀態
   * - normal：到期日距今超過 30 天
   * - warning：到期日在 30 天以內
   * - expired：已過期
   * - unset：未設定到期日
   */
  getExpiryStatus(expireDate: string | null): ExpiryStatus {
    if (!expireDate) return 'unset';
    const expire = new Date(expireDate);
    if (isNaN(expire.getTime())) return 'unset';
    const now = new Date();
    const diffMs = expire.getTime() - now.getTime();
    const diffDays = diffMs / (1000 * 60 * 60 * 24);
    if (diffDays < 0) return 'expired';
    if (diffDays <= 30) return 'warning';
    return 'normal';
  }

  /** 取得到期狀態的顯示文字 */
  getExpiryLabel(status: ExpiryStatus): string {
    const labels: Record<ExpiryStatus, string> = {
      normal: '正常',
      warning: '即將到期',
      expired: '已過期',
      unset: '未設定',
    };
    return labels[status];
  }

  /** 格式化日期（僅顯示 YYYY/MM/DD） */
  formatDate(dateStr: string | null): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return '—';
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}/${m}/${day}`;
  }

  /** 縮短 URL 顯示（超過指定長度則截斷） */
  shortenUrl(url: string | null, maxLen = 40): string {
    if (!url) return '—';
    // 移除通訊協定前綴方便顯示
    const display = url.replace(/^https?:\/\//, '');
    return display.length > maxLen ? display.substring(0, maxLen) + '...' : display;
  }
}
