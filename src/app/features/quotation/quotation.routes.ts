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
  {
    path: 'create',
    data: { breadcrumb: '新增報價' },
    loadComponent: () =>
      import('./pages/quotation-form/quotation-form.component').then(
        (m) => m.QuotationFormComponent
      ),
  },
  {
    path: ':id/detail',
    data: { breadcrumb: '報價單詳情' },
    loadComponent: () =>
      import('./pages/quotation-detail/quotation-detail.component').then(
        (m) => m.QuotationDetailComponent
      ),
  },
  {
    path: ':id',
    data: { breadcrumb: '編輯報價' },
    loadComponent: () =>
      import('./pages/quotation-form/quotation-form.component').then(
        (m) => m.QuotationFormComponent
      ),
  },
];
