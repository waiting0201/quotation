import { Routes } from '@angular/router';

export const INVOICE_ROUTES: Routes = [
  {
    path: '',
    data: { breadcrumb: '發票清單' },
    loadComponent: () =>
      import('./pages/invoice-list/invoice-list.component').then(
        (m) => m.InvoiceListComponent
      ),
  },
  {
    path: 'create',
    data: { breadcrumb: '新增發票' },
    loadComponent: () =>
      import('./pages/invoice-form/invoice-form.component').then(
        (m) => m.InvoiceFormComponent
      ),
  },
  {
    path: ':id',
    data: { breadcrumb: '編輯發票' },
    loadComponent: () =>
      import('./pages/invoice-form/invoice-form.component').then(
        (m) => m.InvoiceFormComponent
      ),
  },
];
