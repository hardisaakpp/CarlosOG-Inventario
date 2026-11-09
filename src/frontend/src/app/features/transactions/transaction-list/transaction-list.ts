import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, input, numberAttribute, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { debounceTime, finalize } from 'rxjs';

import { Product } from '../../../models/product';
import { Transaction, TransactionType } from '../../../models/transaction';
import { ProductService } from '../../../services/product.service';
import { TransactionService } from '../../../services/transaction.service';

@Component({
  selector: 'app-transaction-list',
  imports: [
    CurrencyPipe,
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTableModule,
  ],
  templateUrl: './transaction-list.html',
  styleUrl: './transaction-list.css',
})
export class TransactionList implements OnInit {
  private transactionService = inject(TransactionService);
  private productService = inject(ProductService);
  private fb = inject(FormBuilder);

  // Llega como query param (?productId=3) desde el botón de historial de productos.
  productId = input(undefined, { transform: numberAttribute });

  columns = ['date', 'product', 'type', 'quantity', 'unitPrice', 'totalPrice', 'actions'];

  transactions = signal<Transaction[]>([]);
  products = signal<Product[]>([]);
  total = signal(0);
  loading = signal(false);
  pageIndex = signal(0);
  pageSize = signal(10);

  filters = this.fb.group({
    productId: [null as number | null],
    type: ['' as TransactionType | ''],
    range: this.fb.group({
      start: [null as Date | null],
      end: [null as Date | null],
    }),
  });

  // Producto filtrado: se muestra su nombre y stock sobre la tabla (historial por producto).
  selectedProduct = computed(() => {
    const id = this.filters.controls.productId.value;
    return this.products().find((p) => p.id === id);
  });

  constructor() {
    this.filters.valueChanges
      .pipe(debounceTime(300), takeUntilDestroyed(inject(DestroyRef)))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
      });
  }

  ngOnInit() {
    const id = this.productId();
    if (!isNaN(id!)) {
      this.filters.patchValue({ productId: id }, { emitEvent: false });
    }

    this.productService.list({ page: 1, pageSize: 100 }).subscribe((res) => this.products.set(res.items));
    this.load();
  }

  load() {
    const { productId, type, range } = this.filters.getRawValue();

    // El filtro "hasta" incluye el día completo.
    let to = range.end;
    if (to) {
      to = new Date(to.getFullYear(), to.getMonth(), to.getDate(), 23, 59, 59, 999);
    }

    this.loading.set(true);
    this.transactionService
      .list({
        productId,
        type,
        from: range.start,
        to,
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((res) => {
        this.transactions.set(res.items);
        this.total.set(res.totalCount);
      });
  }

  onPage(event: PageEvent) {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  clearFilters() {
    this.filters.reset({ productId: null, type: '', range: { start: null, end: null } });
  }

  remove(transaction: Transaction) {
    if (!confirm('¿Eliminar esta transacción? Se revertirá su efecto en el stock.')) return;

    this.transactionService.remove(transaction.id).subscribe(() => {
      if (this.transactions().length === 1 && this.pageIndex() > 0) {
        this.pageIndex.update((i) => i - 1);
      }
      this.load();
      // El stock del producto cambió: se recarga para mostrarlo actualizado.
      this.productService.list({ page: 1, pageSize: 100 }).subscribe((res) => this.products.set(res.items));
    });
  }
}
