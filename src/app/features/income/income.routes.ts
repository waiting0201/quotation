import { Routes } from '@angular/router';

export const INCOME_ROUTES: Routes = [
  {
    path: '',
    data: { breadcrumb: '收款清單' },
    loadComponent: () =>
      import('./pages/income-list/income-list.component').then(
        (m) => m.IncomeListComponent
      ),
  },
];
