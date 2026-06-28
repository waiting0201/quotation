import { Routes } from '@angular/router';

export const INCOME_ROUTES: Routes = [
  {
    path: '',
    data: { breadcrumb: '入帳清單' },
    loadComponent: () =>
      import('./pages/income-list/income-list.component').then(
        (m) => m.IncomeListComponent
      ),
  },
  {
    path: 'create',
    data: { breadcrumb: '新增入帳' },
    loadComponent: () =>
      import('./pages/income-create/income-create.component').then(
        (m) => m.IncomeCreateComponent
      ),
  },
];
