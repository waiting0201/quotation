export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  user: UserInfo;
}

export interface UserInfo {
  userid: string;
  email: string;
  name: string;
  permissions: Permission[];
}

export interface Permission {
  limid: number;
  key: string;
  value: string;
  isQuery: boolean;
  isInsert: boolean;
  isUpdate: boolean;
  isDelete: boolean;
}
