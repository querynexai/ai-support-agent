import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./chat/chat').then(m => m.ChatComponent) },
  { path: 'admin', loadComponent: () => import('./admin/admin').then(m => m.AdminComponent) }
];