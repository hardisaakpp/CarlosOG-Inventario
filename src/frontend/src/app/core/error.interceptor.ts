import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { EMPTY, catchError } from 'rxjs';
import { NotifyService } from './notify.service';

// Cualquier error HTTP se muestra aquí en un snackbar y la petición termina sin emitir,
// por lo que los componentes solo manejan el caso exitoso (y finalize para los spinners).
// El backend responde con ProblemDetails: "detail" trae el motivo y,
// en errores de validación (400), "errors" trae un mensaje por campo.
function getMessage(err: HttpErrorResponse): string {
  if (err.status === 0) {
    return 'No se pudo conectar con el servidor.';
  }

  const body = err.error;
  if (body?.errors) {
    return Object.values<string[]>(body.errors).flat().join(' ');
  }

  return body?.detail ?? body?.title ?? 'Ocurrió un error inesperado.';
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notify = inject(NotifyService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      notify.error(getMessage(err));
      return EMPTY;
    }),
  );
};
