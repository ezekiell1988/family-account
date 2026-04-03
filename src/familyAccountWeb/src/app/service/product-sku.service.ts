import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { ProductSKUDto } from '../shared/models';
import { CreateProductSKURequest, UpdateProductSKURequest } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class ProductSKUService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('ProductSKUService');
  private readonly base   = `${environment.apiUrl}product-skus`;

  // ── Estado ───────────────────────────────────────────────────────
  items     = signal<ProductSKUDto[]>([]);
  isLoading = signal<boolean>(false);
  error     = signal<string | null>(null);

  clearError(): void { this.error.set(null); }

  // ── CARGAR TODOS ─────────────────────────────────────────────────
  loadList(): Observable<ProductSKUDto[]> {
    this.isLoading.set(true);
    this.error.set(null);
    return this.http.get<ProductSKUDto[]>(`${this.base}/data.json`).pipe(
      tap(res => {
        this.items.set(res ?? []);
        this.isLoading.set(false);
        this.logger.info(`✅ ProductSKUs cargados: ${res?.length ?? 0}`);
      }),
      catchError(err => {
        this.error.set(err?.error ?? 'Error al cargar SKUs');
        this.isLoading.set(false);
        this.logger.error('❌ Error al cargar ProductSKUs:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── CREAR ─────────────────────────────────────────────────────────
  create(req: CreateProductSKURequest): Observable<ProductSKUDto> {
    this.isLoading.set(true);
    this.error.set(null);
    return this.http.post<ProductSKUDto>(`${this.base}/`, req).pipe(
      tap(created => {
        this.items.update(list => [...list, created]);
        this.isLoading.set(false);
        this.logger.info(`✅ ProductSKU creado: ${created.codeProductSKU}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al crear SKU';
        this.error.set(typeof msg === 'string' ? msg : 'Error al crear SKU');
        this.isLoading.set(false);
        this.logger.error('❌ Error al crear ProductSKU:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── ACTUALIZAR ────────────────────────────────────────────────────
  update(id: number, req: UpdateProductSKURequest): Observable<ProductSKUDto> {
    this.isLoading.set(true);
    this.error.set(null);
    return this.http.put<ProductSKUDto>(`${this.base}/${id}`, req).pipe(
      tap(updated => {
        this.items.update(list => list.map(i => i.idProductSKU === id ? updated : i));
        this.isLoading.set(false);
        this.logger.info(`✅ ProductSKU actualizado: ${updated.codeProductSKU}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al actualizar SKU';
        this.error.set(typeof msg === 'string' ? msg : 'Error al actualizar SKU');
        this.isLoading.set(false);
        this.logger.error('❌ Error al actualizar ProductSKU:', err);
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
        this.items.update(list => list.filter(i => i.idProductSKU !== id));
        this.isLoading.set(false);
        this.logger.info(`✅ ProductSKU eliminado: ${id}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al eliminar SKU';
        this.error.set(typeof msg === 'string' ? msg : 'Error al eliminar SKU');
        this.isLoading.set(false);
        this.logger.error('❌ Error al eliminar ProductSKU:', err);
        return throwError(() => err);
      }),
    );
  }
}

