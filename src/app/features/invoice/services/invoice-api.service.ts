import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse, ApiListResponse } from '../../../core/models/api-response.model';
import {
  InvoiceListItem,
  InvoiceDetailResponse,
  InvoiceCreateUpdateDto,
  QuotationLookup,
  CustomerLookup,
} from '../models/invoice.model';

@Injectable({ providedIn: 'root' })
export class InvoiceApiService {
  private readonly http = inject(HttpClient);

  getList(page = 1, pageSize = 20, search?: string): Observable<ApiListResponse<InvoiceListItem>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (search?.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<ApiListResponse<InvoiceListItem>>('/api/invoices', { params });
  }

  getById(id: string): Observable<ApiResponse<InvoiceDetailResponse>> {
    return this.http.get<ApiResponse<InvoiceDetailResponse>>(`/api/invoices/${id}`);
  }

  create(dto: InvoiceCreateUpdateDto): Observable<ApiResponse<InvoiceDetailResponse>> {
    return this.http.post<ApiResponse<InvoiceDetailResponse>>('/api/invoices', dto);
  }

  update(id: string, dto: InvoiceCreateUpdateDto): Observable<ApiResponse<InvoiceDetailResponse>> {
    return this.http.put<ApiResponse<InvoiceDetailResponse>>(`/api/invoices/${id}`, dto);
  }

  delete(id: string): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`/api/invoices/${id}`);
  }

  downloadPdf(id: string): Observable<Blob> {
    return this.http.get(`/api/invoices/${id}/pdf`, { responseType: 'blob' });
  }

  getCustomerQuotations(customerId: number): Observable<ApiResponse<QuotationLookup[]>> {
    return this.http.get<ApiResponse<QuotationLookup[]>>(`/api/invoices/quotations/${customerId}`);
  }

  getCustomers(): Observable<ApiListResponse<CustomerLookup>> {
    const params = new HttpParams()
      .set('page', 1)
      .set('pageSize', 9999);
    return this.http.get<ApiListResponse<CustomerLookup>>('/api/customers', { params });
  }
}
