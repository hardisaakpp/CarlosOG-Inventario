import { CurrencyPipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { debounceTime, finalize } from 'rxjs';

import { Product } from '../../../models/product';
import { ProductService } from '../../../services/product.service';

@Component({
  selector: 'app-product-list',
  imports: [
    CurrencyPipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTableModule,
  ],
  templateUrl: './product-list.html',
  styleUrl: './product-list.css',
})
export class ProductList {
  private productService = inject(ProductService);
  private fb = inject(FormBuilder);

  columns = ['image', 'name', 'category', 'price', 'stock', 'actions'];

  products = signal<Product[]>([]);
  categories = signal<string[]>([]);
  total = signal(0);
  loading = signal(false);
  pageIndex = signal(0);
  pageSize = signal(10);

  filters = this.fb.group({
    name: [''],
    category: [''],
    minPrice: [null as number | null],
    maxPrice: [null as number | null],
    minStock: [null as number | null],
    maxStock: [null as number | null],
  });

  constructor() {
    this.filters.valueChanges
      .pipe(debounceTime(300), takeUntilDestroyed(inject(DestroyRef)))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
      });

    this.productService.categories().subscribe((c) => this.categories.set(c));
    this.load();
  }

  load() {
    this.loading.set(true);

    this.productService
      .list({
        ...this.filters.getRawValue(),
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((res) => {
        this.products.set(res.items);
        this.total.set(res.totalCount);
      });
  }

  onPage(event: PageEvent) {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  clearFilters() {
    this.filters.reset({ name: '', category: '' });
  }

  remove(product: Product) {
    if (!confirm(`¿Eliminar el producto "${product.name}"?`)) return;

    this.productService.remove(product.id).subscribe(() => {
      // Si era el único de la última página, retrocede una.
      if (this.products().length === 1 && this.pageIndex() > 0) {
        this.pageIndex.update((i) => i - 1);
      }
      this.load();
    });
  }
}
