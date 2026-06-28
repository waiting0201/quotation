import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse, ApiListResponse } from '../../../core/models/api-response.model';
import { CustomerTypeListItem, CustomerTypeCreateUpdate } from '../models/customer-type.model';

@Injectable({ providedIn: 'root' })
export class CustomerTypeApiService {
  private readonly http = inject(HttpClient);

  getList(page = 1, pageSize = 20): Observable<ApiListResponse<CustomerTypeListItem>> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    return this.http.get<ApiListResponse<CustomerTypeListItem>>('/api/customer-types', { params });
  }

  /** 取得全部分類（不分頁，用於下拉選單） */
  getAll(): Observable<ApiListResponse<CustomerTypeListItem>> {
    const params = new HttpParams()
      .set('page', 1)
      .set('pageSize', 9999);
    return this.http.get<ApiListResponse<CustomerTypeListItem>>('/api/customer-types', { params });
  }

  create(dto: CustomerTypeCreateUpdate): Observable<ApiResponse<unknown>> {
    return this.http.post<ApiResponse<unknown>>('/api/customer-types', dto);
  }

  update(id: number, dto: CustomerTypeCreateUpdate): Observable<ApiResponse<unknown>> {
    return this.http.put<ApiResponse<unknown>>(`/api/customer-types/${id}`, dto);
  }

  delete(id: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`/api/customer-types/${id}`);
  }
}
