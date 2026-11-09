import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, computed, inject, input, numberAttribute, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { Product } from '../../../models/product';
import { Transaction, TransactionRequest, TransactionType } from '../../../models/transaction';
import { ProductService } from '../../../services/product.service';
import { TransactionService } from '../../../services/transaction.service';

@Component({
  selector: 'app-transaction-form',
  imports: [
    CurrencyPipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './transaction-form.html',
  styleUrl: './transaction-form.css',
})
export class TransactionForm implements OnInit {
  private transactionService = inject(TransactionService);
  private productService = inject(ProductService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  // Viene del parámetro :id de la ruta; solo existe al editar.
  id = input(undefined, { transform: numberAttribute });
  isEdit = computed(() => !isNaN(this.id()!));

  products = signal<Product[]>([]);
  saving = signal(false);

  // Transacción original (solo al editar): se usa para conservar la fecha
  // y para calcular cuánto stock hay realmente disponible.
  private original = signal<Transaction | null>(null);

  form = this.fb.group({
    type: ['Purchase' as TransactionType, Validators.required],
    productId: [null as number | null, Validators.required],
    quantity: [1, [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/), this.stockValidator()]],
    unitPrice: [null as number | null, Validators.min(0.01)],
    detail: ['', Validators.maxLength(500)],
  });

  selectedProduct = signal<Product | undefined>(undefined);

  // Stock con el que se puede contar para una venta. Al editar la misma transacción
  // sobre el mismo producto, primero se devuelve lo que esa transacción ya movió.
  availableStock = computed(() => {
    const product = this.selectedProduct();
    if (!product) return 0;

    const original = this.original();
    if (!original || original.productId !== product.id) return product.stock;

    return product.stock + (original.type === 'Sale' ? original.quantity : -original.quantity);
  });

  constructor() {
    const { type, productId, quantity } = this.form.controls;

    productId.valueChanges.subscribe((id) => {
      const product = this.products().find((p) => p.id === id);
      this.selectedProduct.set(product);

      // Al elegir un producto se sugiere su precio actual (se puede cambiar).
      if (product && !this.isEdit()) {
        this.form.controls.unitPrice.setValue(product.price);
      }
      quantity.updateValueAndValidity();
    });

    type.valueChanges.subscribe(() => quantity.updateValueAndValidity());
  }

  ngOnInit() {
    this.productService.list({ page: 1, pageSize: 100 }).subscribe((res) => {
      this.products.set(res.items);

      if (this.isEdit()) {
        this.loadTransaction();
      }
    });
  }

  private loadTransaction() {
    this.transactionService.get(this.id()!).subscribe((t) => {
      this.original.set(t);
      this.form.patchValue({
        type: t.type,
        productId: t.productId,
        quantity: t.quantity,
        unitPrice: t.unitPrice,
        detail: t.detail ?? '',
      });
    });
  }

  // Una venta no puede superar el stock disponible del producto.
  private stockValidator() {
    return (control: AbstractControl): ValidationErrors | null => {
      const type = this.form?.controls.type.value;
      if (type !== 'Sale' || !this.selectedProduct()) return null;

      return control.value > this.availableStock() ? { insufficientStock: true } : null;
    };
  }

  save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const transaction: TransactionRequest = {
      type: value.type!,
      productId: value.productId!,
      quantity: value.quantity!,
      unitPrice: value.unitPrice,
      detail: value.detail?.trim() || null,
      // Si no se envía la fecha, el backend usa la actual; al editar se conserva la original.
      date: this.original()?.date,
    };

    const request = this.isEdit()
      ? this.transactionService.update(this.id()!, transaction)
      : this.transactionService.create(transaction);

    this.saving.set(true);
    request
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe(() => this.router.navigate(['/transactions']));
  }
}
