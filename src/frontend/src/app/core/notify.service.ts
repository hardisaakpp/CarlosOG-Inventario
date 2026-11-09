import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({ providedIn: 'root' })
export class NotifyService {
  private snackBar = inject(MatSnackBar);

  success(message: string) {
    this.snackBar.open(message, 'Cerrar', { duration: 3500, panelClass: 'snack-success' });
  }

  error(message: string) {
    this.snackBar.open(message, 'Cerrar', { duration: 6000, panelClass: 'snack-error' });
  }
}
