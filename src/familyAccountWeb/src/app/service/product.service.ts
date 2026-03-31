import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { ProductDto } from '../shared/models';
import { CreateProductRequest } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('ProductService');
  private readonly base   = `${environment.apiUrl}products`;

  // ── Estado ───────────────────────────────────────────────────────
  items = signal<ProductDto[]>([]);

  // ── CARGAR TODOS ─────────────────────────────────────────────────
  loadList(): Observable<ProductDto[]> {
    return this.http.get<ProductDto[]>(`${this.base}/data.json`).pipe(
      tap(res => {
        this.items.set(res ?? []);
        this.logger.info(`✅ Productos cargados: ${res?.length ?? 0}`);
      }),
      catchError(err => {
        this.logger.error('❌ Error al cargar Productos:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── CREAR ─────────────────────────────────────────────────────────
  create(req: CreateProductRequest): Observable<ProductDto> {
    return this.http.post<ProductDto>(this.base, req).pipe(
      tap(created => {
        this.items.update(list => [...list, created]);
        this.logger.info(`✅ Producto creado: ${created.codeProduct}`);
      }),
      catchError(err => {
        this.logger.error('❌ Error al crear Producto:', err);
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
}
