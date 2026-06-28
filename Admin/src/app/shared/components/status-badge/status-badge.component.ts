import { Component, computed, input } from '@angular/core';
import { QuotationStatus, InvoiceStatus } from '../../../core/models/enums';

interface BadgeConfig {
  label: string;
  classes: string;
}

const QUOTATION_BADGE: Record<number, BadgeConfig> = {
  [QuotationStatus.Quoted]:     { label: '已報價', classes: 'bg-status-quoted-bg text-status-quoted' },
  [QuotationStatus.Contracted]: { label: '已成交', classes: 'bg-status-contracted-bg text-status-contracted' },
  [QuotationStatus.Closed]:     { label: '已結案', classes: 'bg-status-closed-bg text-status-closed' },
  [QuotationStatus.Cancelled]:  { label: '已取消', classes: 'bg-status-cancelled-bg text-status-cancelled' },
};

const INVOICE_BADGE: Record<number, BadgeConfig> = {
  [InvoiceStatus.Opened]:   { label: '未開立', classes: 'bg-slate-100 text-slate-600' },
  [InvoiceStatus.Sent]:     { label: '已開立', classes: 'bg-primary-100 text-primary-700' },
  [InvoiceStatus.Received]: { label: '已收款', classes: 'bg-status-closed-bg text-status-closed' },
  [InvoiceStatus.Voided]:   { label: '已作廢', classes: 'bg-status-cancelled-bg text-status-cancelled' },
};

@Component({
  selector: 'app-status-badge',
  standalone: true,
  template: `
    <span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium {{ badge().classes }}">
      {{ badge().label }}
    </span>
  `,
})
export class StatusBadgeComponent {
  readonly status = input.required<number>();
  readonly type = input<'quotation' | 'invoice'>('quotation');

  readonly badge = computed<BadgeConfig>(() => {
    const map = this.type() === 'invoice' ? INVOICE_BADGE : QUOTATION_BADGE;
    return map[this.status()] ?? { label: '未知', classes: 'bg-slate-100 text-slate-500' };
  });
}
