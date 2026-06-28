import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs';
import { ApiResponse } from '../../../core/models/api-response.model';

export interface DashboardData {
  stats: DashboardStats;
  recentQuotations: RecentQuotationDto[];
  monthlyTrend: MonthlyTrendDto[];
  invoiceStatusCounts: StatusCountDto[];
  calendarEvents: CalendarEventDto[];
}

export interface DashboardStats {
  activeQuotations: number;
  quotedCount: number;
  signedCount: number;
  pendingInvoices: number;
  issuedCount: number;
  sentCount: number;
  totalCustomers: number;
  newCustomersThisMonth: number;
  totalIncome: number;
  totalIncomeRecords: number;
}

export interface RecentQuotationDto {
  code: string;
  customer: string;
  amount: number;
  status: number;
  date: string;
}

export interface MonthlyTrendDto {
  label: string;
  count: number;
  amount: number;
}

export interface StatusCountDto {
  status: number;
  label: string;
  count: number;
}

export interface CalendarEventDto {
  customer: string;
  name: string;
  startDate: string;
  endDate: string;
  status: number;
}

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly http = inject(HttpClient);

  getDashboard() {
    return this.http
      .get<ApiResponse<DashboardData>>('/api/dashboard')
      .pipe(map((res) => res.data));
  }
}
