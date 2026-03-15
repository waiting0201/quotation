import { Routes } from '@angular/router';

export const QUOTATION_ROUTES: Routes = [
  {
    path: '',
    data: { breadcrumb: '報價清單' },
    loadComponent: () =>
      import('./pages/quotation-list/quotation-list.component').then(
        (m) => m.QuotationListComponent
      ),
  },
];
