import {
  Component,
  input,
  output,
  signal,
  computed,
  effect,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { GroupPermission, PermissionNode } from '../../models/group.model';

/** 類別列展開狀態的 key 為 limId */
type ExpandedMap = Record<number, boolean>;

/** 方便模板存取的 checkbox 欄位名稱 */
type PermField = 'isQuery' | 'isInsert' | 'isUpdate' | 'isDelete';

const PERM_FIELDS: PermField[] = ['isQuery', 'isInsert', 'isUpdate', 'isDelete'];

@Component({
  selector: 'app-permission-matrix',
  standalone: true,
  imports: [NgClass],
  templateUrl: './permission-matrix.component.html',
  styleUrl: './permission-matrix.component.scss',
})
export class PermissionMatrixComponent {
  // ─── Inputs ───────────────────────────────────────────────────────────────
  readonly permissionTree = input<PermissionNode[]>([]);
  readonly permissions = input<GroupPermission[]>([]);

  // ─── Output ───────────────────────────────────────────────────────────────
  readonly permissionsChange = output<GroupPermission[]>();

  // ─── Internal state ───────────────────────────────────────────────────────
  /** 目前可編輯的權限 map，key 為 limId */
  readonly permMap = signal<Record<number, GroupPermission>>({});

  /** 類別展開狀態 */
  readonly expanded = signal<ExpandedMap>({});

  /** 展示用的頂層分類（parentId === 0） */
  readonly categories = computed(() =>
    this.permissionTree().filter((n) => n.parentId === 0)
  );

  readonly permFields = PERM_FIELDS;
  readonly fieldLabels: Record<PermField, string> = {
    isQuery: '查詢',
    isInsert: '新增',
    isUpdate: '修改',
    isDelete: '刪除',
  };

  constructor() {
    // 當 permissions input 變動時，重建內部 map
    effect(() => {
      const perms = this.permissions();
      const map: Record<number, GroupPermission> = {};
      for (const p of perms) {
        map[p.limId] = { ...p };
      }
      this.permMap.set(map);
    });

    // 預設展開所有頂層分類
    effect(() => {
      const cats = this.categories();
      if (cats.length === 0) return;
      const initial: ExpandedMap = {};
      for (const c of cats) {
        initial[c.limId] = true;
      }
      this.expanded.set(initial);
    });
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────

  /** 取得或建立一個 limId 的權限物件 */
  private getOrCreate(limId: number): GroupPermission {
    const map = this.permMap();
    return (
      map[limId] ?? {
        limId,
        isQuery: false,
        isInsert: false,
        isUpdate: false,
        isDelete: false,
      }
    );
  }

  getPermValue(limId: number, field: PermField): boolean {
    return this.permMap()[limId]?.[field] ?? false;
  }

  /** 取得分類下所有子項目 */
  getChildren(category: PermissionNode): PermissionNode[] {
    return category.children ?? [];
  }

  /** 是否展開 */
  isExpanded(limId: number): boolean {
    return this.expanded()[limId] ?? true;
  }

  // ─── 分類層級 checkbox 狀態 ───────────────────────────────────────────────

  /**
   * 分類 header checkbox 的狀態：
   * - true: 所有子項目都勾選
   * - false: 無子項目勾選（或分類無子項時看自身）
   * - 'indeterminate': 部分勾選
   */
  getCategoryCheckState(
    category: PermissionNode,
    field: PermField
  ): boolean | 'indeterminate' {
    const children = this.getChildren(category);
    if (children.length === 0) {
      return this.getPermValue(category.limId, field);
    }
    const checkedCount = children.filter((c) =>
      this.getPermValue(c.limId, field)
    ).length;
    if (checkedCount === 0) return false;
    if (checkedCount === children.length) return true;
    return 'indeterminate';
  }

  // ─── Event handlers ───────────────────────────────────────────────────────

  toggleExpand(limId: number): void {
    this.expanded.update((map) => ({ ...map, [limId]: !map[limId] }));
  }

  /** 子項目 checkbox 變更 */
  onItemChange(limId: number, field: PermField, value: boolean): void {
    this.permMap.update((map) => {
      const perm = { ...this.getOrCreate(limId), [field]: value };
      return { ...map, [limId]: perm };
    });
    this.emitChange();
  }

  /** 分類 header checkbox 變更 → 套用到所有子項目 */
  onCategoryChange(category: PermissionNode, field: PermField, value: boolean): void {
    const children = this.getChildren(category);
    if (children.length === 0) {
      // 無子項目時操作分類本身
      this.onItemChange(category.limId, field, value);
      return;
    }
    this.permMap.update((map) => {
      const next = { ...map };
      for (const child of children) {
        next[child.limId] = { ...this.getOrCreate(child.limId), [field]: value };
      }
      return next;
    });
    this.emitChange();
  }

  private emitChange(): void {
    const map = this.permMap();
    const result = Object.values(map);
    this.permissionsChange.emit(result);
  }
}
