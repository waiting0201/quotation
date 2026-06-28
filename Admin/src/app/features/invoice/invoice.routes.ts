import { Routes } from '@angular/router';

export const INVOICE_ROUTES: Routes = [
  {
    path: '',
    data: { breadcrumb: '請款清單' },
    loadComponent: () =>
      import('./pages/invoice-list/invoice-list.component').then(
        (m) => m.InvoiceListComponent
      ),
  },
  {
    path: 'create',
    data: { breadcrumb: '新增請款' },
    loadComponent: () =>
      import('./pages/invoice-form/invoice-form.component').then(
        (m) => m.InvoiceFormComponent
      ),
  },
  {
    path: ':id',
    data: { breadcrumb: '編輯請款' },
    loadComponent: () =>
      import('./pages/invoice-form/invoice-form.component').then(
        (m) => m.InvoiceFormComponent
      ),
  },
];
