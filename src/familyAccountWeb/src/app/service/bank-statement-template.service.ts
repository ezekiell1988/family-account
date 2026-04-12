import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { BankStatementTemplateDto } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class BankStatementTemplateService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('BankStatementTemplateService');
  private readonly base   = `${environment.apiUrl}bank-statement-templates`;

  // ── Estado ───────────────────────────────────────────────────────
  items     = signal<BankStatementTemplateDto[]>([]);
  isLoading = signal<boolean>(false);
  error     = signal<string | null>(null);

  clearError(): void { this.error.set(null); }

  private start(): void { this.isLoading.set(true); this.error.set(null); }
  private stop():  void { this.isLoading.set(false); }
  private fail(msg: string): void { this.error.set(msg); }

  // ── LISTAR ───────────────────────────────────────────────────────
  loadList(): Observable<BankStatementTemplateDto[]> {
    this.start();
    return this.http.get<BankStatementTemplateDto[]>(`${this.base}/data.json`).pipe(
      tap(res => {
        this.items.set(res ?? []);
      }),
      catchError(err => {
        this.fail('Error al cargar plantillas bancarias');
        this.logger.error('Error cargando plantillas', err);
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }
}
