export type TransactionType = 'Purchase' | 'Sale';

export interface Transaction {
  id: number;
  date: string;
  type: TransactionType;
  productId: number;
  productName: string;
  productStock: number;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  detail: string | null;
}

export interface TransactionRequest {
  date?: string;
  type: TransactionType;
  productId: number;
  quantity: number;
  unitPrice?: number | null;
  detail: string | null;
}

export interface TransactionFilters {
  productId?: number | null;
  type?: TransactionType | '' | null;
  from?: Date | null;
  to?: Date | null;
  page: number;
  pageSize: number;
}
