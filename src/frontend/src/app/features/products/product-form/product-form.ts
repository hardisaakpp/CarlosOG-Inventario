import { Component, OnInit, computed, inject, input, numberAttribute, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';
import { finalize, startWith } from 'rxjs';

import { ProductRequest } from '../../../models/product';
import { ProductService } from '../../../services/product.service';

@Component({
  selector: 'app-product-form',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatAutocompleteModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  templateUrl: './product-form.html',
  styleUrl: './product-form.css',
})
export class ProductForm implements OnInit {
  private productService = inject(ProductService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  // Viene del parámetro :id de la ruta (withComponentInputBinding); solo existe al editar.
  id = input(undefined, { transform: numberAttribute });
  isEdit = computed(() => !isNaN(this.id()!));

  saving = signal(false);

  form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    category: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', Validators.maxLength(500)],
    imageUrl: ['', [Validators.maxLength(500), Validators.pattern(/^(https?:\/\/.+)?$/)]],
    price: [null as number | null, [Validators.required, Validators.min(0.01)]],
    stock: [0, [Validators.required, Validators.min(0), Validators.pattern(/^\d+$/)]],
  });

  categories = signal<string[]>([]);

  private categoryText = toSignal(this.form.controls.category.valueChanges.pipe(startWith('')), {
    initialValue: '',
  });

  // Sugerencias que coinciden con lo que se va escribiendo.
  filteredCategories = computed(() => {
    const text = (this.categoryText() ?? '').toLowerCase();
    return this.categories().filter((c) => c.toLowerCase().includes(text));
  });

  ngOnInit() {
    this.productService.categories().subscribe((c) => this.categories.set(c));

    if (!this.isEdit()) return;

    // Al editar, el stock es solo informativo: cambia únicamente con transacciones.
    this.form.controls.stock.disable();

    this.productService.get(this.id()!).subscribe((p) => this.form.patchValue(p));
  }

  save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const category = value.category!.trim();
    const product: ProductRequest = {
      name: value.name!.trim(),
      // Si ya existe una categoría igual (sin importar mayúsculas), se reutiliza su escritura
      // para no crear duplicados como "audio" y "Audio".
      category: this.categories().find((c) => c.toLowerCase() === category.toLowerCase()) ?? category,
      description: value.description?.trim() || null,
      imageUrl: value.imageUrl?.trim() || null,
      price: value.price!,
      stock: value.stock!,
    };

    const request = this.isEdit()
      ? this.productService.update(this.id()!, product)
      : this.productService.create(product);

    this.saving.set(true);
    request
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe(() => this.router.navigate(['/products']));
  }
}
