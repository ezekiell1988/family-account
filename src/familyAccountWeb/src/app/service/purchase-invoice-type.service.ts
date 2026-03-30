import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { PurchaseInvoiceTypeDto } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class PurchaseInvoiceTypeService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('PurchaseInvoiceTypeService');
  private readonly base   = `${environment.apiUrl}purchase-invoice-types`;

  // ── Estado ───────────────────────────────────────────────────────
  items = signal<PurchaseInvoiceTypeDto[]>([]);

  // ── CARGAR ACTIVOS ───────────────────────────────────────────────
  loadActive(): Observable<PurchaseInvoiceTypeDto[]> {
    return this.http.get<PurchaseInvoiceTypeDto[]>(`${this.base}/active.json`).pipe(
      tap(res => {
        this.items.set(res ?? []);
        this.logger.info(`✅ Tipos de factura cargados: ${res?.length ?? 0}`);
      }),
      catchError(err => {
        this.logger.error('❌ Error al cargar tipos de factura:', err);
        return throwError(() => err);
      }),
    );
  }
}
