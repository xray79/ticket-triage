import { Routes } from '@angular/router';
import { authGuard, permissionGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'tickets' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'tickets',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/tickets/ticket-queue.component').then((m) => m.TicketQueueComponent)
  },
  {
    path: 'tickets/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/tickets/ticket-detail.component').then((m) => m.TicketDetailComponent)
  },
  {
    path: 'admin/users',
    canActivate: [authGuard, permissionGuard('users:manage')],
    loadComponent: () =>
      import('./features/admin-users/user-management.component').then((m) => m.UserManagementComponent)
  },
  { path: '**', redirectTo: 'tickets' }
];
