import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse, ApiListResponse } from '../../../core/models/api-response.model';
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

  getList(page = 1, pageSize = 20, search?: string): Observable<ApiListResponse<UserListItem>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (search?.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<ApiListResponse<UserListItem>>('/api/users', { params });
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
