import { Component } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { PanelComponent } from '../../../../shared/components/panel/panel.component';

@Component({
  selector: 'app-income-list',
  standalone: true,
  imports: [PageHeaderComponent, PanelComponent],
  template: `
    <app-page-header title="收款清單">
      <button class="px-3 py-1.5 bg-primary-600 hover:bg-primary-700 text-white text-sm font-medium rounded-[var(--radius-btn)] transition-colors">
        新增收款
      </button>
    </app-page-header>
    <app-panel>
      <p class="text-slate-500 text-sm">收款清單開發中</p>
    </app-panel>
  `,
})
export class IncomeListComponent {}
