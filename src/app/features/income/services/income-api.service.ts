import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiListResponse, ApiResponse } from '../../../core/models/api-response.model';
import { IncomeListItem, IncomeCreateDto, CustomerLookup } from '../models/income.model';

@Injectable({ providedIn: 'root' })
export class IncomeApiService {
  private readonly http = inject(HttpClient);

  getList(page = 1, pageSize = 20, search?: string): Observable<ApiListResponse<IncomeListItem>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (search?.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<ApiListResponse<IncomeListItem>>('/api/incomes', { params });
  }

  create(dto: IncomeCreateDto): Observable<ApiResponse<IncomeListItem>> {
    return this.http.post<ApiResponse<IncomeListItem>>('/api/incomes', dto);
  }

  delete(id: string): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`/api/incomes/${id}`);
  }

  getCustomers(): Observable<ApiListResponse<CustomerLookup>> {
    const params = new HttpParams()
      .set('page', 1)
      .set('pageSize', 9999);
    return this.http.get<ApiListResponse<CustomerLookup>>('/api/customers', { params });
  }
}
