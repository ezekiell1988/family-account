import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { CostCenterDto } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class CostCenterService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('CostCenterService');
  private readonly base   = `${environment.apiUrl}cost-centers`;

  // ── Estado ───────────────────────────────────────────────────────
  items = signal<CostCenterDto[]>([]);

  // ── CARGAR TODOS ─────────────────────────────────────────────────
  loadList(): Observable<CostCenterDto[]> {
    return this.http.get<CostCenterDto[]>(`${this.base}/data.json`).pipe(
      tap(res => {
        this.items.set(res ?? []);
        this.logger.info(`✅ CostCenters cargados: ${res?.length ?? 0}`);
      }),
      catchError(err => {
        this.logger.error('❌ Error al cargar CostCenters:', err);
        return throwError(() => err);
      }),
    );
  }
}
