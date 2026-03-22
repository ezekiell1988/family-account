import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { AccountDto, CreateAccountRequest, UpdateAccountRequest } from '../shared/models';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('AccountService');
  private readonly base   = `${environment.apiUrl}accounts`;

  // ── Estado ───────────────────────────────────────────────────────
  accounts   = signal<AccountDto[]>([]);
  totalCount = signal<number>(0);
  isLoading  = signal<boolean>(false);
  error      = signal<string | null>(null);

  hasAccounts = computed(() => this.accounts().length > 0);

  clearError(): void {
    this.error.set(null);
  }

  private start(): void {
    this.isLoading.set(true);
    this.error.set(null);
  }

  private stop(): void {
    this.isLoading.set(false);
  }

  private fail(msg: string): void {
    this.error.set(msg);
  }

  // ── LISTAR ───────────────────────────────────────────────────────
  loadList(): Observable<AccountDto[]> {
    this.start();
    return this.http.get<AccountDto[]>(`${this.base}.json`).pipe(
      tap(res => {
        const items = res ?? [];
        this.accounts.set(items);
        this.totalCount.set(items.length);
        this.logger.info(`✅ Cuentas cargadas: ${items.length}`);
      }),
      catchError(err => {
        this.fail('Error al cargar las cuentas contables');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── CREAR ────────────────────────────────────────────────────────
  create(request: CreateAccountRequest): Observable<AccountDto> {
    this.start();
    return this.http.post<AccountDto>(`${this.base}/`, request).pipe(
      tap(res => {
        this.accounts.update(ls => [...ls, res].sort((a, b) => a.codeAccount.localeCompare(b.codeAccount)));
        this.totalCount.update(n => n + 1);
        this.logger.info(`✅ Cuenta creada: ${res.codeAccount} - ${res.nameAccount}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al crear la cuenta';
        this.fail(typeof msg === 'string' ? msg : 'Error al crear la cuenta');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── ACTUALIZAR ───────────────────────────────────────────────────
  update(id: number, request: UpdateAccountRequest): Observable<AccountDto> {
    this.start();
    return this.http.put<AccountDto>(`${this.base}/${id}`, request).pipe(
      tap(res => {
        this.accounts.update(ls =>
          ls.map(a => (a.idAccount === id ? res : a))
            .sort((a, b) => a.codeAccount.localeCompare(b.codeAccount)),
        );
        this.logger.info(`✅ Cuenta actualizada: ${res.codeAccount} - ${res.nameAccount}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al actualizar la cuenta';
        this.fail(typeof msg === 'string' ? msg : 'Error al actualizar la cuenta');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── ELIMINAR ─────────────────────────────────────────────────────
  delete(id: number): Observable<void> {
    this.start();
    return this.http.delete<void>(`${this.base}/${id}`).pipe(
      tap(() => {
        this.accounts.update(ls => ls.filter(a => a.idAccount !== id));
        this.totalCount.update(n => n - 1);
        this.logger.info(`✅ Cuenta eliminada: id=${id}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al eliminar la cuenta';
        this.fail(typeof msg === 'string' ? msg : 'Error al eliminar la cuenta');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }
}
