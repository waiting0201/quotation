import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse } from '../../../core/models/api-response.model';
import {
  UserListItem,
  UserDetail,
  UserCreate,
  UserUpdate,
  UserPasswordChange,
} from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserApiService {
  private readonly http = inject(HttpClient);

  getList(): Observable<ApiResponse<UserListItem[]>> {
    return this.http.get<ApiResponse<UserListItem[]>>('/api/users');
  }

  getById(id: string): Observable<ApiResponse<UserDetail>> {
    return this.http.get<ApiResponse<UserDetail>>(`/api/users/${id}`);
  }

  create(dto: UserCreate): Observable<ApiResponse<unknown>> {
    return this.http.post<ApiResponse<unknown>>('/api/users', dto);
  }

  update(id: string, dto: UserUpdate): Observable<ApiResponse<unknown>> {
    return this.http.put<ApiResponse<unknown>>(`/api/users/${id}`, dto);
  }

  changePassword(id: string, dto: UserPasswordChange): Observable<ApiResponse<unknown>> {
    return this.http.put<ApiResponse<unknown>>(`/api/users/${id}/password`, dto);
  }

  delete(id: string): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`/api/users/${id}`);
  }
}
