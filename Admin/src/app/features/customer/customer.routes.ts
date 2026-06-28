import { Routes } from '@angular/router';

export const CUSTOMER_ROUTES: Routes = [
  {
    path: '',
    data: { breadcrumb: '客戶清單' },
    loadComponent: () =>
      import('./pages/customer-list/customer-list.component').then(
        (m) => m.CustomerListComponent
      ),
  },
  {
    path: 'create',
    data: { breadcrumb: '新增客戶' },
    loadComponent: () =>
      import('./pages/customer-form/customer-form.component').then(
        (m) => m.CustomerFormComponent
      ),
  },
  {
    path: 'category',
    data: { breadcrumb: '客戶分類' },
    loadComponent: () =>
      import('./pages/customer-type-list/customer-type-list.component').then(
        (m) => m.CustomerTypeListComponent
      ),
  },
  {
    path: ':id',
    data: { breadcrumb: '編輯客戶' },
    loadComponent: () =>
      import('./pages/customer-form/customer-form.component').then(
        (m) => m.CustomerFormComponent
      ),
  },
];
