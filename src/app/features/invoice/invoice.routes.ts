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
];
