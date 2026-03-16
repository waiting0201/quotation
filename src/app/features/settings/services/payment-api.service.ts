import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse, ApiListResponse } from '../../../core/models/api-response.model';
import { PaymentListItem, PaymentCreateUpdate } from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class PaymentApiService {
  private readonly http = inject(HttpClient);

  getList(page = 1, pageSize = 20): Observable<ApiListResponse<PaymentListItem>> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    return this.http.get<ApiListResponse<PaymentListItem>>('/api/payments', { params });
  }

  /** 取得全部付款條件（不分頁，用於下拉選單） */
  getAll(): Observable<ApiListResponse<PaymentListItem>> {
    const params = new HttpParams()
      .set('page', 1)
      .set('pageSize', 9999);
    return this.http.get<ApiListResponse<PaymentListItem>>('/api/payments', { params });
  }

  create(dto: PaymentCreateUpdate): Observable<ApiResponse<unknown>> {
    return this.http.post<ApiResponse<unknown>>('/api/payments', dto);
  }

  update(id: number, dto: PaymentCreateUpdate): Observable<ApiResponse<unknown>> {
    return this.http.put<ApiResponse<unknown>>(`/api/payments/${id}`, dto);
  }

  delete(id: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`/api/payments/${id}`);
  }
}
