import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import {
  ProductAccountDto,
  CreateProductAccountRequest,
  UpdateProductAccountRequest,
} from '../shared/models';

@Injectable({ providedIn: 'root' })
export class ProductAccountService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('ProductAccountService');
  private readonly base   = `${environment.apiUrl}product-accounts`;

  // ── Estado ───────────────────────────────────────────────────────
  items = signal<ProductAccountDto[]>([]);

  // ── CARGAR TODOS ─────────────────────────────────────────────────
  loadList(): Observable<ProductAccountDto[]> {
    return this.http.get<ProductAccountDto[]>(`${this.base}/data.json`).pipe(
      tap(res => {
        this.items.set(res ?? []);
        this.logger.info(`✅ ProductAccounts cargados: ${res?.length ?? 0}`);
      }),
      catchError(err => {
        this.logger.error('❌ Error al cargar ProductAccounts:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── CREAR ──────────────────────────────────────────────────────────
  create(req: CreateProductAccountRequest): Observable<ProductAccountDto> {
    return this.http.post<ProductAccountDto>(`${this.base}/`, req).pipe(
      tap(created => {
        this.items.update(list => [...list, created]);
        this.logger.success(`✅ ProductAccount creado: ${created.idProductAccount}`);
      }),
      catchError(err => {
        this.logger.error('❌ Error al crear ProductAccount:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── ACTUALIZAR ────────────────────────────────────────────────────
  update(id: number, req: UpdateProductAccountRequest): Observable<ProductAccountDto> {
    return this.http.put<ProductAccountDto>(`${this.base}/${id}`, req).pipe(
      tap(updated => {
        this.items.update(list => list.map(i => i.idProductAccount === id ? updated : i));
        this.logger.success(`✅ ProductAccount actualizado: ${id}`);
      }),
      catchError(err => {
        this.logger.error('❌ Error al actualizar ProductAccount:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── ELIMINAR ──────────────────────────────────────────────────────
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`).pipe(
      tap(() => {
        this.items.update(list => list.filter(i => i.idProductAccount !== id));
        this.logger.success(`✅ ProductAccount eliminado: ${id}`);
      }),
      catchError(err => {
        this.logger.error('❌ Error al eliminar ProductAccount:', err);
        return throwError(() => err);
      }),
    );
  }
}
