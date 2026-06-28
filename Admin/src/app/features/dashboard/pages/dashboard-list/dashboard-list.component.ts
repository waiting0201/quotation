import { Component, inject, signal, computed, OnInit, ViewChild } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { NgApexchartsModule, ChartComponent } from 'ng-apexcharts';
import { AuthService } from '../../../../core/auth/auth.service';
import {
  DashboardApiService,
  DashboardData,
  RecentQuotationDto,
  MonthlyTrendDto,
} from '../../services/dashboard-api.service';
import {
  ApexChart,
  ApexDataLabels,
  ApexFill,
  ApexGrid,
  ApexLegend,
  ApexNonAxisChartSeries,
  ApexPlotOptions,
  ApexResponsive,
  ApexStroke,
  ApexTooltip,
  ApexXAxis,
  ApexYAxis,
} from 'ng-apexcharts';

// ─── View Interfaces ──────────────────────────────────────────
interface StatCard {
  label: string;
  value: string;
  sub: string;
  icon: string;
  accent: string;
}

interface CalendarEvent {
  customer: string;
  name: string;
  startDate: Date;
  endDate: Date;
  color: string;
}

const EVENT_COLORS = ['#00D4FF', '#06FFF4', '#10FFB0', '#A78BFA', '#FFB300', '#FF4466'];

@Component({
  selector: 'app-dashboard-list',
  standalone: true,
  imports: [NgApexchartsModule],
  templateUrl: './dashboard-list.component.html',
  styleUrl: './dashboard-list.component.scss',
})
export class DashboardListComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly dashboardApi = inject(DashboardApiService);
  readonly currentUser = this.auth.currentUser;

  // ─── Loading State ──────────────────────────────────────────
  readonly loading = signal(true);

  // ─── Stat Cards ──────────────────────────────────────────────
  readonly stats = signal<StatCard[]>([]);

  // ─── Calendar ────────────────────────────────────────────────
  readonly calendarMonth = signal(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  readonly weekDays = ['日', '一', '二', '三', '四', '五', '六'];
  readonly calendarEvents = signal<CalendarEvent[]>([]);

  readonly calendarTitle = computed(() => {
    const d = this.calendarMonth();
    return `${d.getFullYear()} 年 ${d.getMonth() + 1} 月`;
  });

  readonly calendarDays = computed(() => {
    const d = this.calendarMonth();
    const year = d.getFullYear();
    const month = d.getMonth();
    const firstDay = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const today = new Date();
    const cells: { day: number; isToday: boolean; isCurrentMonth: boolean }[] = [];
    const prevMonthDays = new Date(year, month, 0).getDate();
    for (let i = firstDay - 1; i >= 0; i--) {
      cells.push({ day: prevMonthDays - i, isToday: false, isCurrentMonth: false });
    }
    for (let i = 1; i <= daysInMonth; i++) {
      const isToday = year === today.getFullYear() && month === today.getMonth() && i === today.getDate();
      cells.push({ day: i, isToday, isCurrentMonth: true });
    }
    const remaining = 42 - cells.length;
    for (let i = 1; i <= remaining; i++) {
      cells.push({ day: i, isToday: false, isCurrentMonth: false });
    }
    return cells;
  });

  readonly visibleCalendarEvents = computed(() => {
    const d = this.calendarMonth();
    const year = d.getFullYear();
    const month = d.getMonth();
    const monthStart = new Date(year, month, 1);
    const monthEnd = new Date(year, month + 1, 0);
    return this.calendarEvents().filter(
      (ev) => ev.startDate <= monthEnd && ev.endDate >= monthStart
    );
  });

  getEventsForDay(day: number): CalendarEvent[] {
    const d = this.calendarMonth();
    const cellDate = new Date(d.getFullYear(), d.getMonth(), day);
    return this.visibleCalendarEvents().filter(
      (ev) => cellDate >= ev.startDate && cellDate <= ev.endDate
    );
  }

  prevMonth(): void {
    this.calendarMonth.update((d) => new Date(d.getFullYear(), d.getMonth() - 1, 1));
  }

  nextMonth(): void {
    this.calendarMonth.update((d) => new Date(d.getFullYear(), d.getMonth() + 1, 1));
  }

  goToday(): void {
    const now = new Date();
    this.calendarMonth.set(new Date(now.getFullYear(), now.getMonth(), 1));
  }

  // ─── Recent Quotations ──────────────────────────────────────
  readonly recentQuotations = signal<RecentQuotationDto[]>([]);

  // ─── ApexCharts: Bar Chart (月度報價趨勢) ───────────────────
  barChartSeries: ApexAxisChartSeries = [];
  barChartOptions = this.buildBarChartOptions([], []);

  // ─── ApexCharts: Donut Chart (發票狀態) ─────────────────────
  donutSeries: ApexNonAxisChartSeries = [];
  donutOptions = this.buildDonutChartOptions([], []);

  // ─── Stat Card Icons ────────────────────────────────────────
  private readonly statIcons: Record<string, string> = {
    quotation: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z"/></svg>`,
    invoice: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="M9 14.25l6-6m4.5-3.493V21.75l-3.75-1.5-3.75 1.5-3.75-1.5-3.75 1.5V4.757c0-1.108.806-2.057 1.907-2.185a48.507 48.507 0 0111.186 0c1.1.128 1.907 1.077 1.907 2.185zM9.75 9h.008v.008H9.75V9zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm4.125 4.5h.008v.008h-.008V13.5zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z"/></svg>`,
    customer: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z"/></svg>`,
    income: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="M2.25 18.75a60.07 60.07 0 0115.797 2.101c.727.198 1.453-.342 1.453-1.096V18.75M3.75 4.5v.75A.75.75 0 013 6h-.75m0 0v-.375c0-.621.504-1.125 1.125-1.125H20.25M2.25 6v9m18-10.5v.75c0 .414.336.75.75.75h.75m-1.5-1.5h.375c.621 0 1.125.504 1.125 1.125v9.75c0 .621-.504 1.125-1.125 1.125h-.375m1.5-1.5H21a.75.75 0 00-.75.75v.75m0 0H3.75m0 0h-.375a1.125 1.125 0 01-1.125-1.125V15m1.5 1.5v-.75A.75.75 0 003 15h-.75M15 10.5a3 3 0 11-6 0 3 3 0 016 0zm3 0h.008v.008H18V10.5zm-12 0h.008v.008H6V10.5z"/></svg>`,
  };

  // ─── Lifecycle ──────────────────────────────────────────────
  ngOnInit(): void {
    this.dashboardApi.getDashboard().subscribe({
      next: (data) => this.applyData(data),
      error: () => this.loading.set(false),
    });
  }

  private applyData(data: DashboardData): void {
    const s = data.stats;

    // Stat cards
    this.stats.set([
      {
        label: '進行中報價',
        value: String(s.activeQuotations),
        sub: `已報價 ${s.quotedCount} ｜ 已簽約 ${s.signedCount}`,
        icon: this.statIcons['quotation'],
        accent: '#00D4FF',
      },
      {
        label: '待處理發票',
        value: String(s.pendingInvoices),
        sub: `已開 ${s.issuedCount} ｜ 已寄出 ${s.sentCount}`,
        icon: this.statIcons['invoice'],
        accent: '#06FFF4',
      },
      {
        label: '客戶總數',
        value: String(s.totalCustomers),
        sub: `本月新增 ${s.newCustomersThisMonth}`,
        icon: this.statIcons['customer'],
        accent: '#10FFB0',
      },
      {
        label: '累計收款',
        value: this.formatAmount(s.totalIncome),
        sub: `${s.totalIncomeRecords} 筆收款紀錄`,
        icon: this.statIcons['income'],
        accent: '#FFB300',
      },
    ]);

    // Recent quotations
    this.recentQuotations.set(data.recentQuotations);

    // Bar chart — 月度報價趨勢
    const barLabels = data.monthlyTrend.map((m) => m.label);
    const barAmounts = data.monthlyTrend.map((m) => m.amount);
    const barCounts = data.monthlyTrend.map((m) => m.count);
    this.barChartSeries = [
      { name: '金額', type: 'bar', data: barAmounts },
      { name: '筆數', type: 'line', data: barCounts },
    ];
    this.barChartOptions = this.buildBarChartOptions(barLabels, barAmounts);

    // Donut chart — 發票狀態
    const statusColors: Record<number, string> = {
      0: '#00D4FF',
      1: '#FFB300',
      2: '#10FFB0',
      3: '#FF4466',
    };
    const donutLabels = data.invoiceStatusCounts.map((sc) => sc.label);
    const donutValues = data.invoiceStatusCounts.map((sc) => sc.count);
    const donutColors = data.invoiceStatusCounts.map((sc) => statusColors[sc.status] ?? '#969696');
    this.donutSeries = donutValues;
    this.donutOptions = this.buildDonutChartOptions(donutLabels, donutColors);

    // Calendar events
    this.calendarEvents.set(
      data.calendarEvents.map((ev, i) => ({
        customer: ev.customer,
        name: ev.name,
        startDate: new Date(ev.startDate),
        endDate: new Date(ev.endDate),
        color: EVENT_COLORS[i % EVENT_COLORS.length],
      }))
    );

    this.loading.set(false);
  }

  // ─── Chart Builders ─────────────────────────────────────────
  private buildBarChartOptions(categories: string[], amounts: number[]): any {
    return {
      chart: {
        type: 'bar',
        height: 260,
        background: 'transparent',
        toolbar: { show: false },
        fontFamily: 'inherit',
      },
      plotOptions: {
        bar: {
          borderRadius: 4,
          columnWidth: '50%',
        },
      },
      colors: ['#00D4FF', '#FFB300'],
      fill: {
        type: 'gradient',
        gradient: {
          shade: 'dark',
          type: 'vertical',
          shadeIntensity: 0.3,
          opacityFrom: 1,
          opacityTo: 0.6,
          stops: [0, 100],
        },
      },
      stroke: {
        width: [0, 3],
        curve: 'smooth',
      },
      grid: {
        borderColor: 'rgba(0, 212, 255, 0.08)',
        strokeDashArray: 4,
        xaxis: { lines: { show: false } },
        yaxis: { lines: { show: true } },
        padding: { top: 0, bottom: 0 },
      },
      xaxis: {
        categories,
        labels: { style: { colors: '#969696', fontSize: '12px' } },
        axisBorder: { show: false },
        axisTicks: { show: false },
      },
      yaxis: [
        {
          title: { text: '金額', style: { color: '#969696', fontSize: '12px' } },
          labels: {
            style: { colors: '#969696', fontSize: '11px' },
            formatter: (val: number) => this.formatAmount(val),
          },
        },
        {
          opposite: true,
          title: { text: '筆數', style: { color: '#969696', fontSize: '12px' } },
          labels: {
            style: { colors: '#969696', fontSize: '11px' },
            formatter: (val: number) => String(Math.round(val)),
          },
        },
      ],
      dataLabels: { enabled: false },
      tooltip: {
        theme: 'dark',
        style: { fontSize: '13px' },
        y: {
          formatter: (val: number, opts: any) => {
            if (opts.seriesIndex === 0) return this.formatAmount(val);
            return `${val} 筆`;
          },
        },
      },
      legend: {
        position: 'top',
        horizontalAlign: 'right',
        labels: { colors: '#CCCCCC' },
        markers: { size: 8, shape: 'circle' as const },
      },
    };
  }

  private buildDonutChartOptions(labels: string[], colors: string[]): any {
    return {
      chart: {
        type: 'donut',
        height: 300,
        background: 'transparent',
        fontFamily: 'inherit',
      },
      labels,
      colors,
      stroke: {
        width: 2,
        colors: ['#252526'],
      },
      plotOptions: {
        pie: {
          donut: {
            size: '68%',
            labels: {
              show: true,
              name: {
                show: true,
                fontSize: '14px',
                color: '#CCCCCC',
                offsetY: -8,
              },
              value: {
                show: true,
                fontSize: '24px',
                fontWeight: 700,
                color: '#FFFFFF',
                offsetY: 4,
                formatter: (val: string) => val,
              },
              total: {
                show: true,
                label: '總計',
                fontSize: '13px',
                color: '#969696',
                formatter: (w: any) => {
                  return String(w.globals.spikeHeight ?? w.globals.series.reduce((a: number, b: number) => a + b, 0));
                },
              },
            },
          },
        },
      },
      dataLabels: { enabled: false },
      legend: {
        position: 'bottom',
        labels: { colors: '#CCCCCC' },
        markers: {
          size: 10,
          shape: 'square' as const,
          offsetX: -4,
        },
        itemMargin: { horizontal: 12, vertical: 4 },
      },
      tooltip: {
        theme: 'dark',
        style: { fontSize: '13px' },
        y: {
          formatter: (val: number) => `${val} 張`,
        },
      },
    };
  }

  // ─── Helpers ────────────────────────────────────────────────
  formatAmount(n: number): string {
    if (n >= 1000000) return `$${(n / 1000000).toFixed(1)}M`;
    if (n >= 1000) return `$${(n / 1000).toFixed(0)}K`;
    return `$${n.toLocaleString()}`;
  }

  statusLabel(s: number): string {
    return ['已報價', '已簽約', '已結案', '已取消'][s] ?? '';
  }

  statusColor(s: number): string {
    return ['#38BDF8', '#00D4FF', '#10FFB0', '#FF4466'][s] ?? '#969696';
  }

  safeIcon(svg: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(svg);
  }
}

// Re-export type for template usage
type ApexAxisChartSeries = { name: string; type?: string; data: number[] }[];
