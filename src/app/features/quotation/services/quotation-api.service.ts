import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiResponse, ApiListResponse } from '../../../core/models/api-response.model';
import {
  QuotationListItem,
  QuotationDetailResponse,
  QuotationCreateUpdateDto,
  CustomerLookup,
  ContactLookup,
} from '../models/quotation.model';

@Injectable({ providedIn: 'root' })
export class QuotationApiService {
  private readonly http = inject(HttpClient);

  getList(page = 1, pageSize = 20, search?: string): Observable<ApiListResponse<QuotationListItem>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (search?.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<ApiListResponse<QuotationListItem>>('/api/quotations', { params });
  }

  getById(id: string): Observable<ApiResponse<QuotationDetailResponse>> {
    return this.http.get<ApiResponse<QuotationDetailResponse>>(`/api/quotations/${id}`);
  }

  create(dto: QuotationCreateUpdateDto): Observable<ApiResponse<QuotationDetailResponse>> {
    return this.http.post<ApiResponse<QuotationDetailResponse>>('/api/quotations', dto);
  }

  update(id: string, dto: QuotationCreateUpdateDto): Observable<ApiResponse<QuotationDetailResponse>> {
    return this.http.put<ApiResponse<QuotationDetailResponse>>(`/api/quotations/${id}`, dto);
  }

  delete(id: string): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`/api/quotations/${id}`);
  }

  downloadPdf(id: string): Observable<Blob> {
    return this.http.get(`/api/quotations/${id}/pdf`, { responseType: 'blob' });
  }

  getCustomers(): Observable<ApiListResponse<CustomerLookup>> {
    const params = new HttpParams()
      .set('page', 1)
      .set('pageSize', 9999);
    return this.http.get<ApiListResponse<CustomerLookup>>('/api/customers', { params });
  }

  getPaymentTemplates(): Observable<ApiListResponse<{ paymentId: number; remark: string }>> {
    const params = new HttpParams().set('page', 1).set('pageSize', 9999);
    return this.http.get<ApiListResponse<{ paymentId: number; remark: string }>>('/api/payments', { params });
  }

  getContactsByCustomer(customerId: number): Observable<ContactLookup[]> {
    return this.http.get<ApiResponse<any>>(`/api/customers/${customerId}`).pipe(
      map((res) => {
        const contacts = res.data?.contacts ?? [];
        return contacts.map((c: any) => ({
          customerDetailId: c.contactId,
          name: c.name ?? '',
          email: c.email ?? '',
        })) as ContactLookup[];
      })
    );
  }
}
