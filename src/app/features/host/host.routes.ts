import { Routes } from '@angular/router';

export const HOST_ROUTES: Routes = [
  {
    path: '',
    data: { breadcrumb: '網站清單' },
    loadComponent: () =>
      import('./pages/host-list/host-list.component').then(
        (m) => m.HostListComponent
      ),
  },
];
