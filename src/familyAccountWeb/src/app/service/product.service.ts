import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { ProductDto } from '../shared/models';
import { CreateProductRequest, UpdateProductRequest } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('ProductService');
  private readonly base   = `${environment.apiUrl}products`;

  // ── Estado ───────────────────────────────────────────────────────
  items      = signal<ProductDto[]>([]);
  isLoading  = signal<boolean>(false);
  error      = signal<string | null>(null);

  clearError(): void { this.error.set(null); }

  // ── CARGAR TODOS ─────────────────────────────────────────────────
  loadList(): Observable<ProductDto[]> {
    this.isLoading.set(true);
    this.error.set(null);
    return this.http.get<ProductDto[]>(`${this.base}/data.json`).pipe(
      tap(res => {
        this.items.set(res ?? []);
        this.logger.info(`✅ Productos cargados: ${res?.length ?? 0}`);
      }),
      catchError(err => {
        this.error.set(err?.error ?? 'Error al cargar productos');
        this.logger.error('❌ Error al cargar Productos:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── CREAR ─────────────────────────────────────────────────────────
  create(req: CreateProductRequest): Observable<ProductDto> {
    this.isLoading.set(true);
    this.error.set(null);
    return this.http.post<ProductDto>(`${this.base}/`, req).pipe(
      tap(created => {
        this.items.update(list => [...list, created]);
        this.isLoading.set(false);
        this.logger.info(`✅ Producto creado: ${created.codeProduct}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al crear producto';
        this.error.set(typeof msg === 'string' ? msg : 'Error al crear producto');
        this.isLoading.set(false);
        this.logger.error('❌ Error al crear Producto:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── ACTUALIZAR ────────────────────────────────────────────────────
  update(id: number, req: UpdateProductRequest): Observable<ProductDto> {
    this.isLoading.set(true);
    this.error.set(null);
    return this.http.put<ProductDto>(`${this.base}/${id}`, req).pipe(
      tap(updated => {
        this.items.update(list => list.map(i => i.idProduct === id ? updated : i));
        this.isLoading.set(false);
        this.logger.info(`✅ Producto actualizado: ${updated.codeProduct}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al actualizar producto';
        this.error.set(typeof msg === 'string' ? msg : 'Error al actualizar producto');
        this.isLoading.set(false);
        this.logger.error('❌ Error al actualizar Producto:', err);
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
        this.items.update(list => list.filter(i => i.idProduct !== id));
        this.isLoading.set(false);
        this.logger.info(`✅ Producto eliminado: ${id}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al eliminar producto';
        this.error.set(typeof msg === 'string' ? msg : 'Error al eliminar producto');
        this.isLoading.set(false);
        this.logger.error('❌ Error al eliminar Producto:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── ASOCIAR SKU ───────────────────────────────────────────────────
  addSKU(idProduct: number, idProductSKU: number): Observable<void> {
    return this.http.post<void>(`${this.base}/${idProduct}/skus/${idProductSKU}`, {}).pipe(
      tap(() => this.logger.info(`✅ SKU ${idProductSKU} asociado al producto ${idProduct}`)),
      catchError(err => {
        this.logger.error('❌ Error al asociar SKU al producto:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── DESASOCIAR SKU ────────────────────────────────────────────────
  removeSKU(idProduct: number, idProductSKU: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${idProduct}/skus/${idProductSKU}`).pipe(
      tap(() => this.logger.info(`✅ SKU ${idProductSKU} desasociado del producto ${idProduct}`)),
      catchError(err => {
        this.logger.error('❌ Error al desasociar SKU del producto:', err);
        return throwError(() => err);
      }),
    );
  }
}

