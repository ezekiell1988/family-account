import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { ProductTypeDto } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class ProductTypeService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('ProductTypeService');
  private readonly base   = `${environment.apiUrl}product-types`;

  // ── Estado ───────────────────────────────────────────────────────
  items     = signal<ProductTypeDto[]>([]);
  isLoading = signal<boolean>(false);

  // ── CARGAR TODOS ─────────────────────────────────────────────────
  loadList(): Observable<ProductTypeDto[]> {
    this.isLoading.set(true);
    return this.http.get<ProductTypeDto[]>(`${this.base}/data.json`).pipe(
      tap(res => {
        this.items.set(res ?? []);
        this.isLoading.set(false);
        this.logger.info(`✅ Tipos de producto cargados: ${res?.length ?? 0}`);
      }),
      catchError(err => {
        this.isLoading.set(false);
        this.logger.error('❌ Error al cargar tipos de producto:', err);
        return throwError(() => err);
      }),
    );
  }
}
