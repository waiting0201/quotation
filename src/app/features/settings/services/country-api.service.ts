import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse, ApiListResponse } from '../../../core/models/api-response.model';
import { CountryListItem, CountryCreateUpdate } from '../models/country.model';

@Injectable({ providedIn: 'root' })
export class CountryApiService {
  private readonly http = inject(HttpClient);

  getList(page = 1, pageSize = 20): Observable<ApiListResponse<CountryListItem>> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    return this.http.get<ApiListResponse<CountryListItem>>('/api/countries', { params });
  }

  /** 取得全部國家（不分頁，用於下拉選單） */
  getAll(): Observable<ApiListResponse<CountryListItem>> {
    const params = new HttpParams()
      .set('page', 1)
      .set('pageSize', 9999);
    return this.http.get<ApiListResponse<CountryListItem>>('/api/countries', { params });
  }

  create(dto: CountryCreateUpdate): Observable<ApiResponse<unknown>> {
    return this.http.post<ApiResponse<unknown>>('/api/countries', dto);
  }

  update(id: number, dto: CountryCreateUpdate): Observable<ApiResponse<unknown>> {
    return this.http.put<ApiResponse<unknown>>(`/api/countries/${id}`, dto);
  }

  delete(id: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`/api/countries/${id}`);
  }
}
