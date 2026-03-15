import { PermissionNode } from './group.model';

export interface UserListItem {
  userId: string;
  name: string;
  email: string;
  groupId: string | null;
  groupTitle: string | null;
  status: boolean;
  updateTime: string | null;
}

export interface UserDetail {
  userId: string;
  name: string;
  email: string;
  groupId: string | null;
  status: boolean;
  updateTime: string | null;
  permissions: UserPermission[];
}

export interface UserPermission {
  limId: number;
  isQuery: boolean;
  isInsert: boolean;
  isUpdate: boolean;
  isDelete: boolean;
}

export interface UserCreate {
  name: string;
  email: string;
  password: string;
  groupId: string | null;
  status: boolean;
  permissions: UserPermission[];
}

export interface UserUpdate {
  name: string;
  email: string;
  groupId: string | null;
  status: boolean;
  permissions: UserPermission[];
}

export interface UserPasswordChange {
  newPassword: string;
}

// 重新匯出 PermissionNode 供使用者相關元件使用
export type { PermissionNode };
