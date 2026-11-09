import { Injectable } from '@angular/core';
import { MatPaginatorIntl } from '@angular/material/paginator';

// Textos del paginador en español (por defecto Material los trae en inglés).
@Injectable()
export class PaginatorEs extends MatPaginatorIntl {
  override itemsPerPageLabel = 'Elementos por página:';
  override nextPageLabel = 'Página siguiente';
  override previousPageLabel = 'Página anterior';
  override firstPageLabel = 'Primera página';
  override lastPageLabel = 'Última página';

  override getRangeLabel = (page: number, pageSize: number, length: number) => {
    if (length === 0) return '0 de 0';

    const start = page * pageSize + 1;
    const end = Math.min((page + 1) * pageSize, length);
    return `${start} – ${end} de ${length}`;
  };
}
