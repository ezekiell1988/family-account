import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { CurrencyDto } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class CurrencyService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('CurrencyService');
  private readonly base   = `${environment.apiUrl}currencies`;

  // ── Estado ───────────────────────────────────────────────────────
  currencies = signal<CurrencyDto[]>([]);
  isLoading  = signal<boolean>(false);
  error      = signal<string | null>(null);

  private start(): void { this.isLoading.set(true); this.error.set(null); }
  private stop():  void { this.isLoading.set(false); }

  // ── LISTAR ───────────────────────────────────────────────────────
  loadList(): Observable<CurrencyDto[]> {
    if (this.currencies().length > 0) {
      return new Observable(obs => { obs.next(this.currencies()); obs.complete(); });
    }
    this.start();
    return this.http.get<CurrencyDto[]>(`${this.base}/data.json`).pipe(
      tap(res => this.currencies.set(res ?? [])),
      catchError(err => {
        this.logger.error('❌ Error al cargar monedas:', err);
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }
}
