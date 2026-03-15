import { Routes } from '@angular/router';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    data: { breadcrumb: '儀表板' },
    loadComponent: () =>
      import('./pages/dashboard-list/dashboard-list.component').then(
        (m) => m.DashboardListComponent
      ),
  },
];
