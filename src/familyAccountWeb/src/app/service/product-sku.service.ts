import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { ProductSKUDto } from '../shared/models';
import { CreateProductSKURequest } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class ProductSKUService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('ProductSKUService');
  private readonly base   = `${environment.apiUrl}product-skus`;

  // ── Estado ───────────────────────────────────────────────────────
  items = signal<ProductSKUDto[]>([]);

  // ── CARGAR TODOS ─────────────────────────────────────────────────
  loadList(): Observable<ProductSKUDto[]> {
    return this.http.get<ProductSKUDto[]>(`${this.base}/data.json`).pipe(
      tap(res => {
        this.items.set(res ?? []);
        this.logger.info(`✅ ProductSKUs cargados: ${res?.length ?? 0}`);
      }),
      catchError(err => {
        this.logger.error('❌ Error al cargar ProductSKUs:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── CREAR ─────────────────────────────────────────────────────────
  create(req: CreateProductSKURequest): Observable<ProductSKUDto> {
    return this.http.post<ProductSKUDto>(this.base, req).pipe(
      tap(created => {
        this.items.update(list => [...list, created]);
        this.logger.info(`✅ ProductSKU creado: ${created.codeProductSKU}`);
      }),
      catchError(err => {
        this.logger.error('❌ Error al crear ProductSKU:', err);
        return throwError(() => err);
      }),
    );
  }
}
