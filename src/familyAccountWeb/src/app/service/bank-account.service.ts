import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import {
  BankAccountDto,
  CreateBankAccountRequest,
  UpdateBankAccountRequest,
} from '../shared/models';

@Injectable({ providedIn: 'root' })
export class BankAccountService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('BankAccountService');
  private readonly base   = `${environment.apiUrl}bank-accounts`;

  // ── Estado ───────────────────────────────────────────────────────
  items      = signal<BankAccountDto[]>([]);
  totalCount = signal<number>(0);
  isLoading  = signal<boolean>(false);
  error      = signal<string | null>(null);

  clearError(): void { this.error.set(null); }

  private start(): void { this.isLoading.set(true); this.error.set(null); }
  private stop():  void { this.isLoading.set(false); }
  private fail(msg: string): void { this.error.set(msg); }

  // ── LISTAR ───────────────────────────────────────────────────────
  loadList(): Observable<BankAccountDto[]> {
    this.start();
    return this.http.get<BankAccountDto[]>(`${this.base}/data.json`).pipe(
      tap(res => {
        this.items.set(res ?? []);
        this.totalCount.set(res?.length ?? 0);
      }),
      catchError(err => {
        this.fail('Error al cargar cuentas bancarias');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── CREAR ────────────────────────────────────────────────────────
  create(req: CreateBankAccountRequest): Observable<BankAccountDto> {
    this.start();
    return this.http.post<BankAccountDto>(`${this.base}/`, req).pipe(
      tap(res => {
        this.items.update(ls => [...ls, res]);
        this.totalCount.update(n => n + 1);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al crear cuenta bancaria';
        this.fail(typeof msg === 'string' ? msg : 'Error al crear cuenta bancaria');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── ACTUALIZAR ───────────────────────────────────────────────────
  update(id: number, req: UpdateBankAccountRequest): Observable<BankAccountDto> {
    this.start();
    return this.http.put<BankAccountDto>(`${this.base}/${id}`, req).pipe(
      tap(res =>
        this.items.update(ls =>
          ls.map(i => (i.idBankAccount === id ? res : i)),
        ),
      ),
      catchError(err => {
        const msg = err?.error ?? 'Error al actualizar cuenta bancaria';
        this.fail(typeof msg === 'string' ? msg : 'Error al actualizar cuenta bancaria');
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
        this.items.update(ls => ls.filter(i => i.idBankAccount !== id));
        this.totalCount.update(n => n - 1);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al eliminar cuenta bancaria';
        this.fail(typeof msg === 'string' ? msg : 'Error al eliminar cuenta bancaria');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }
}
