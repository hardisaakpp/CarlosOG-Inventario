import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, tap } from 'rxjs';
import { TRANSACTIONS_API } from '../core/api-urls';
import { NotifyService } from '../core/notify.service';
import { ApiResponse, PagedResult } from '../models/api';
import { Transaction, TransactionFilters, TransactionRequest } from '../models/transaction';
import { toParams } from './params';

@Injectable({ providedIn: 'root' })
export class TransactionService {
  private http = inject(HttpClient);
  private notify = inject(NotifyService);

  list(filters: TransactionFilters) {
    return this.http.get<PagedResult<Transaction>>(TRANSACTIONS_API, {
      params: toParams(filters),
    });
  }

  get(id: number) {
    return this.http.get<Transaction>(`${TRANSACTIONS_API}/${id}`);
  }

  create(transaction: TransactionRequest) {
    return this.http.post<ApiResponse<Transaction>>(TRANSACTIONS_API, transaction).pipe(
      tap((res) => this.notify.success(res.message)),
      map((res) => res.data),
    );
  }

  update(id: number, transaction: TransactionRequest) {
    return this.http.put<ApiResponse<Transaction>>(`${TRANSACTIONS_API}/${id}`, transaction).pipe(
      tap((res) => this.notify.success(res.message)),
      map((res) => res.data),
    );
  }

  remove(id: number) {
    return this.http
      .delete<ApiResponse<boolean>>(`${TRANSACTIONS_API}/${id}`)
      .pipe(tap((res) => this.notify.success(res.message)));
  }
}
