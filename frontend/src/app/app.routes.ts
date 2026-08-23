import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.Dashboard),
    title: 'Dashboard · AssetLite',
  },
  {
    path: 'assets',
    loadComponent: () => import('./pages/assets/assets-list').then((m) => m.AssetsList),
    title: 'Assets · AssetLite',
  },
  {
    // Must be declared before 'assets/:tag' so "new" is not matched as a tag.
    path: 'assets/new',
    loadComponent: () => import('./pages/assets/asset-form').then((m) => m.AssetForm),
    title: 'Register asset · AssetLite',
  },
  {
    path: 'assets/:tag',
    loadComponent: () => import('./pages/assets/asset-detail').then((m) => m.AssetDetail),
    title: 'Asset detail · AssetLite',
  },
  {
    path: 'offices',
    loadComponent: () => import('./pages/offices/offices').then((m) => m.Offices),
    title: 'Offices · AssetLite',
  },
  {
    path: 'categories',
    loadComponent: () => import('./pages/categories/categories').then((m) => m.Categories),
    title: 'Categories · AssetLite',
  },
  {
    path: 'reports',
    loadComponent: () => import('./pages/reports/reports').then((m) => m.Reports),
    title: 'Reports · AssetLite',
  },
  { path: '**', redirectTo: '' },
];
