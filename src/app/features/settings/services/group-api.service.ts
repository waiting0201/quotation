import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse } from '../../../core/models/api-response.model';
import {
  GroupListItem,
  GroupDetail,
  GroupCreateUpdate,
  PermissionNode,
} from '../models/group.model';

@Injectable({ providedIn: 'root' })
export class GroupApiService {
  private readonly http = inject(HttpClient);

  getList(): Observable<ApiResponse<GroupListItem[]>> {
    return this.http.get<ApiResponse<GroupListItem[]>>('/api/groups');
  }

  getById(id: string): Observable<ApiResponse<GroupDetail>> {
    return this.http.get<ApiResponse<GroupDetail>>(`/api/groups/${id}`);
  }

  create(dto: GroupCreateUpdate): Observable<ApiResponse<unknown>> {
    return this.http.post<ApiResponse<unknown>>('/api/groups', dto);
  }

  update(id: string, dto: GroupCreateUpdate): Observable<ApiResponse<unknown>> {
    return this.http.put<ApiResponse<unknown>>(`/api/groups/${id}`, dto);
  }

  delete(id: string): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`/api/groups/${id}`);
  }

  getPermissionTree(): Observable<ApiResponse<PermissionNode[]>> {
    return this.http.get<ApiResponse<PermissionNode[]>>('/api/lookups/permissions');
  }
}
