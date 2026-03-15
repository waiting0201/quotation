import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse, ApiListResponse } from '../../../core/models/api-response.model';
import { CustomerListItem, CustomerDetail, CustomerCreateUpdate } from '../models/customer.model';

@Injectable({ providedIn: 'root' })
export class CustomerApiService {
  private readonly http = inject(HttpClient);

  getList(page = 1, pageSize = 20, search?: string): Observable<ApiListResponse<CustomerListItem>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (search?.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<ApiListResponse<CustomerListItem>>('/api/customers', { params });
  }

  getById(id: number): Observable<ApiResponse<CustomerDetail>> {
    return this.http.get<ApiResponse<CustomerDetail>>(`/api/customers/${id}`);
  }

  create(dto: CustomerCreateUpdate): Observable<ApiResponse<CustomerDetail>> {
    return this.http.post<ApiResponse<CustomerDetail>>('/api/customers', dto);
  }

  update(id: number, dto: CustomerCreateUpdate): Observable<ApiResponse<CustomerDetail>> {
    return this.http.put<ApiResponse<CustomerDetail>>(`/api/customers/${id}`, dto);
  }

  delete(id: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`/api/customers/${id}`);
  }
}
