import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'products', pathMatch: 'full' },
  {
    path: 'products',
    loadComponent: () => import('./features/products/product-list/product-list').then((m) => m.ProductList),
  },
  {
    path: 'products/new',
    loadComponent: () => import('./features/products/product-form/product-form').then((m) => m.ProductForm),
  },
  {
    path: 'products/:id/edit',
    loadComponent: () => import('./features/products/product-form/product-form').then((m) => m.ProductForm),
  },
  {
    path: 'transactions',
    loadComponent: () =>
      import('./features/transactions/transaction-list/transaction-list').then((m) => m.TransactionList),
  },
  {
    path: 'transactions/new',
    loadComponent: () =>
      import('./features/transactions/transaction-form/transaction-form').then((m) => m.TransactionForm),
  },
  {
    path: 'transactions/:id/edit',
    loadComponent: () =>
      import('./features/transactions/transaction-form/transaction-form').then((m) => m.TransactionForm),
  },
  { path: '**', redirectTo: 'products' },
];
