import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full',
  },
  {
    path: 'login',
    loadChildren: () =>
      import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        data: { breadcrumb: '首頁' },
        loadChildren: () =>
          import('./features/dashboard/dashboard.routes').then(
            (m) => m.DASHBOARD_ROUTES
          ),
      },
      {
        path: 'quotation',
        data: { breadcrumb: '報價管理' },
        loadChildren: () =>
          import('./features/quotation/quotation.routes').then(
            (m) => m.QUOTATION_ROUTES
          ),
      },
      {
        path: 'customer',
        data: { breadcrumb: '客戶管理' },
        loadChildren: () =>
          import('./features/customer/customer.routes').then(
            (m) => m.CUSTOMER_ROUTES
          ),
      },
      {
        path: 'invoice',
        data: { breadcrumb: '發票管理' },
        loadChildren: () =>
          import('./features/invoice/invoice.routes').then(
            (m) => m.INVOICE_ROUTES
          ),
      },
      {
        path: 'income',
        data: { breadcrumb: '收款管理' },
        loadChildren: () =>
          import('./features/income/income.routes').then(
            (m) => m.INCOME_ROUTES
          ),
      },
      {
        path: 'settings',
        data: { breadcrumb: '系統設定' },
        loadChildren: () =>
          import('./features/settings/settings.routes').then(
            (m) => m.SETTINGS_ROUTES
          ),
      },
    ],
  },
  {
    path: '**',
    redirectTo: 'dashboard',
  },
];
