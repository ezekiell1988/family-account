import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { UnitOfMeasureDto } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class UnitOfMeasureService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('UnitOfMeasureService');
  private readonly base   = `${environment.apiUrl}units-of-measure`;

  // ── Estado ───────────────────────────────────────────────────────
  items     = signal<UnitOfMeasureDto[]>([]);
  isLoading = signal<boolean>(false);

  // ── CARGAR TODOS ─────────────────────────────────────────────────
  loadList(): Observable<UnitOfMeasureDto[]> {
    this.isLoading.set(true);
    return this.http.get<UnitOfMeasureDto[]>(`${this.base}/data.json`).pipe(
      tap(res => {
        this.items.set(res ?? []);
        this.isLoading.set(false);
        this.logger.info(`✅ Unidades de medida cargadas: ${res?.length ?? 0}`);
      }),
      catchError(err => {
        this.isLoading.set(false);
        this.logger.error('❌ Error al cargar unidades de medida:', err);
        return throwError(() => err);
      }),
    );
  }
}
