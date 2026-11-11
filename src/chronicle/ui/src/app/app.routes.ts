import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/events/events-list.component').then(m => m.EventsListComponent)
  },
  {
    path: 'events/:id',
    loadComponent: () => import('./features/event-detail/event-detail.component').then(m => m.EventDetailComponent)
  },
  { path: '**', redirectTo: '' }
];
