import {
  Component,
  inject,
  signal,
  computed,
  OnInit,
  DestroyRef,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { QuotationApiService } from '../../services/quotation-api.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { QuotationDetailResponse } from '../../models/quotation.model';

const STATUS_CONFIG: Record<number, { label: string; cssClass: string }> = {
  0: { label: '已報價', cssClass: 'quoted' },
  1: { label: '已簽約', cssClass: 'signed' },
  2: { label: '已結案', cssClass: 'closed' },
  3: { label: '已取消', cssClass: 'cancelled' },
};

const TAX_CONFIG: Record<number, { label: string; cssClass: string }> = {
  0: { label: '稅外加', cssClass: 'extra' },
  1: { label: '稅內含', cssClass: 'included' },
  2: { label: '免稅', cssClass: 'exempt' },
};

@Component({
  selector: 'app-quotation-detail',
  standalone: true,
  imports: [],
  templateUrl: './quotation-detail.component.html',
  styleUrl: './quotation-detail.component.scss',
})
export class QuotationDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(QuotationApiService);
  private readonly notification = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(false);
  readonly quotation = signal<QuotationDetailResponse | null>(null);

  readonly detailSubtotal = computed(() =>
    (this.quotation()?.details ?? []).reduce((sum, d) => sum + d.total, 0)
  );

  /** 折前未稅小計 = 明細 + 內容加總 */
  readonly rawSubtotal = computed(() => {
    const q = this.quotation();
    if (!q) return 0;
    return (
      (q.details ?? []).reduce((sum, d) => sum + d.total, 0) +
      (q.contents ?? []).reduce((sum, c) => sum + c.price, 0)
    );
  });

  /** 折扣金額（後端計算） */
  readonly discountAmount = computed(() => this.quotation()?.discountAmount ?? 0);

  /** 折後未稅合計（total/tax 已是折後值，反推即為折後未稅） */
  readonly pretaxTotal = computed(() => {
    const q = this.quotation();
    if (!q) return 0;
    return q.total - q.tax;
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/quotation']);
      return;
    }
    this.loadDetail(id);
  }

  private loadDetail(id: string): void {
    this.loading.set(true);
    this.api
      .getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.quotation.set(res.data);
          this.loading.set(false);
        },
        error: () => {
          this.notification.error('載入報價單失敗');
          this.loading.set(false);
          this.router.navigate(['/quotation']);
        },
      });
  }

  goBack(): void {
    this.router.navigate(['/quotation']);
  }

  goToEdit(): void {
    const q = this.quotation();
    if (q) this.router.navigate(['/quotation', q.itemId]);
  }

  downloadPdf(): void {
    const q = this.quotation();
    if (!q) return;
    this.api
      .downloadPdf(q.itemId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `${q.itemCode}.pdf`;
          a.click();
          URL.revokeObjectURL(url);
        },
        error: () => this.notification.error('PDF 下載失敗'),
      });
  }

  formatDate(dateStr: string | null | undefined): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return '—';
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}/${m}/${day}`;
  }

  formatAmount(amount: number): string {
    return `NT$${amount.toLocaleString()}`;
  }

  getStatusLabel(status: number): string {
    return STATUS_CONFIG[status]?.label ?? '未知';
  }

  getStatusClass(status: number): string {
    return STATUS_CONFIG[status]?.cssClass ?? '';
  }

  getTaxLabel(taxType: number): string {
    return TAX_CONFIG[taxType]?.label ?? '—';
  }

  getTaxClass(taxType: number): string {
    return TAX_CONFIG[taxType]?.cssClass ?? '';
  }
}
