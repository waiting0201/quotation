export interface GroupListItem {
  groupId: string;
  title: string;
  userCount: number;
}

export interface GroupDetail {
  groupId: string;
  title: string;
  permissions: GroupPermission[];
}

export interface GroupPermission {
  limId: number;
  isQuery: boolean;
  isInsert: boolean;
  isUpdate: boolean;
  isDelete: boolean;
}

export interface GroupCreateUpdate {
  title: string;
  permissions: GroupPermission[];
}

export interface PermissionNode {
  limId: number;
  key: string;
  value: string;
  parentId: number;
  children?: PermissionNode[];
}
