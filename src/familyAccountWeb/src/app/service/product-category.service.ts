import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import {
  ProductCategoryDto,
  CreateProductCategoryRequest,
  UpdateProductCategoryRequest,
} from '../shared/models';

@Injectable({ providedIn: 'root' })
export class ProductCategoryService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('ProductCategoryService');
  private readonly base   = `${environment.apiUrl}product-categories`;

  // ── Estado ───────────────────────────────────────────────────────
  items     = signal<ProductCategoryDto[]>([]);
  isLoading = signal<boolean>(false);
  error     = signal<string | null>(null);

  clearError(): void { this.error.set(null); }

  // ── CARGAR TODOS ─────────────────────────────────────────────────
  loadList(): Observable<ProductCategoryDto[]> {
    this.isLoading.set(true);
    this.error.set(null);
    return this.http.get<ProductCategoryDto[]>(`${this.base}/data.json`).pipe(
      tap(res => {
        this.items.set(res ?? []);
        this.isLoading.set(false);
        this.logger.info(`✅ Categorías cargadas: ${res?.length ?? 0}`);
      }),
      catchError(err => {
        this.error.set(err?.error ?? 'Error al cargar categorías');
        this.isLoading.set(false);
        this.logger.error('❌ Error al cargar Categorías:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── CREAR ─────────────────────────────────────────────────────────
  create(req: CreateProductCategoryRequest): Observable<ProductCategoryDto> {
    this.isLoading.set(true);
    this.error.set(null);
    return this.http.post<ProductCategoryDto>(`${this.base}/`, req).pipe(
      tap(created => {
        this.items.update(list => [...list, created]);
        this.isLoading.set(false);
        this.logger.info(`✅ Categoría creada: ${created.nameProductCategory}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al crear categoría';
        this.error.set(typeof msg === 'string' ? msg : 'Error al crear categoría');
        this.isLoading.set(false);
        this.logger.error('❌ Error al crear Categoría:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── ACTUALIZAR ────────────────────────────────────────────────────
  update(id: number, req: UpdateProductCategoryRequest): Observable<ProductCategoryDto> {
    this.isLoading.set(true);
    this.error.set(null);
    return this.http.put<ProductCategoryDto>(`${this.base}/${id}`, req).pipe(
      tap(updated => {
        this.items.update(list => list.map(i => i.idProductCategory === id ? updated : i));
        this.isLoading.set(false);
        this.logger.info(`✅ Categoría actualizada: ${updated.nameProductCategory}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al actualizar categoría';
        this.error.set(typeof msg === 'string' ? msg : 'Error al actualizar categoría');
        this.isLoading.set(false);
        this.logger.error('❌ Error al actualizar Categoría:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── ELIMINAR ──────────────────────────────────────────────────────
  delete(id: number): Observable<void> {
    this.isLoading.set(true);
    this.error.set(null);
    return this.http.delete<void>(`${this.base}/${id}`).pipe(
      tap(() => {
        this.items.update(list => list.filter(i => i.idProductCategory !== id));
        this.isLoading.set(false);
        this.logger.info(`✅ Categoría eliminada: ${id}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al eliminar categoría';
        this.error.set(typeof msg === 'string' ? msg : 'Error al eliminar categoría');
        this.isLoading.set(false);
        this.logger.error('❌ Error al eliminar Categoría:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── ASOCIAR A PRODUCTO ───────────────────────────────────────────
  addToProduct(idCategory: number, idProduct: number): Observable<void> {
    return this.http.post<void>(`${this.base}/${idCategory}/products/${idProduct}`, {}).pipe(
      tap(() => this.logger.info(`✅ Categoría ${idCategory} asociada al producto ${idProduct}`)),
      catchError(err => {
        this.logger.error('❌ Error al asociar categoría al producto:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── DESASOCIAR DE PRODUCTO ───────────────────────────────────────
  removeFromProduct(idCategory: number, idProduct: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${idCategory}/products/${idProduct}`).pipe(
      tap(() => this.logger.info(`✅ Categoría ${idCategory} desasociada del producto ${idProduct}`)),
      catchError(err => {
        this.logger.error('❌ Error al desasociar categoría del producto:', err);
        return throwError(() => err);
      }),
    );
  }
}
