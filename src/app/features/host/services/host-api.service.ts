import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Host, HostCreateUpdate } from '../models/host.model';

@Injectable({ providedIn: 'root' })
export class HostApiService {
  private readonly http = inject(HttpClient);

  getList(search?: string): Observable<ApiResponse<Host[]>> {
    let params = new HttpParams();
    if (search?.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<ApiResponse<Host[]>>('/api/hosts', { params });
  }

  getById(id: number): Observable<ApiResponse<Host>> {
    return this.http.get<ApiResponse<Host>>(`/api/hosts/${id}`);
  }

  create(dto: HostCreateUpdate): Observable<ApiResponse<Host>> {
    return this.http.post<ApiResponse<Host>>('/api/hosts', dto);
  }

  update(id: number, dto: HostCreateUpdate): Observable<ApiResponse<Host>> {
    return this.http.put<ApiResponse<Host>>(`/api/hosts/${id}`, dto);
  }

  delete(id: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`/api/hosts/${id}`);
  }
}
