import { Routes } from '@angular/router';

export const SETTINGS_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'users',
    pathMatch: 'full',
  },
  {
    path: 'users',
    data: { breadcrumb: '使用者管理' },
    loadComponent: () =>
      import('./pages/users-page/users-page.component').then(
        (m) => m.UsersPageComponent
      ),
  },
  {
    path: 'groups',
    data: { breadcrumb: '群組管理' },
    loadComponent: () =>
      import('./pages/groups-page/groups-page.component').then(
        (m) => m.GroupsPageComponent
      ),
  },
  {
    path: 'countries',
    data: { breadcrumb: '國家管理' },
    loadComponent: () =>
      import('./pages/countries-page/countries-page.component').then(
        (m) => m.CountriesPageComponent
      ),
  },
  {
    path: 'payments',
    data: { breadcrumb: '付款條件' },
    loadComponent: () =>
      import('./pages/payments-page/payments-page.component').then(
        (m) => m.PaymentsPageComponent
      ),
  },
];
