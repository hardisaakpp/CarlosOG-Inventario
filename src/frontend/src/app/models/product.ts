export interface Product {
  id: number;
  name: string;
  description: string | null;
  category: string;
  imageUrl: string | null;
  price: number;
  stock: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface ProductRequest {
  name: string;
  description: string | null;
  category: string;
  imageUrl: string | null;
  price: number;
  stock: number;
}

export interface ProductFilters {
  name?: string | null;
  category?: string | null;
  minPrice?: number | null;
  maxPrice?: number | null;
  minStock?: number | null;
  maxStock?: number | null;
  page: number;
  pageSize: number;
}
