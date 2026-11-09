import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, tap } from 'rxjs';
import { PRODUCTS_API } from '../core/api-urls';
import { NotifyService } from '../core/notify.service';
import { ApiResponse, PagedResult } from '../models/api';
import { Product, ProductFilters, ProductRequest } from '../models/product';
import { toParams } from './params';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private http = inject(HttpClient);
  private notify = inject(NotifyService);

  list(filters: ProductFilters) {
    return this.http.get<PagedResult<Product>>(PRODUCTS_API, { params: toParams(filters) });
  }

  get(id: number) {
    return this.http.get<Product>(`${PRODUCTS_API}/${id}`);
  }

  categories() {
    return this.http.get<string[]>(`${PRODUCTS_API}/categories`);
  }

  create(product: ProductRequest) {
    return this.http.post<ApiResponse<Product>>(PRODUCTS_API, product).pipe(
      tap((res) => this.notify.success(res.message)),
      map((res) => res.data),
    );
  }

  update(id: number, product: ProductRequest) {
    return this.http.put<ApiResponse<Product>>(`${PRODUCTS_API}/${id}`, product).pipe(
      tap((res) => this.notify.success(res.message)),
      map((res) => res.data),
    );
  }

  remove(id: number) {
    return this.http
      .delete<ApiResponse<boolean>>(`${PRODUCTS_API}/${id}`)
      .pipe(tap((res) => this.notify.success(res.message)));
  }
}
