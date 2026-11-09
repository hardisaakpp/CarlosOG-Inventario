import { HttpParams } from '@angular/common/http';

// Arma los query params ignorando los filtros vacíos.
export function toParams(filters: object): HttpParams {
  let params = new HttpParams();

  for (const [key, value] of Object.entries(filters)) {
    if (value === null || value === undefined || value === '') continue;
    params = params.set(key, value instanceof Date ? value.toISOString() : String(value));
  }

  return params;
}
