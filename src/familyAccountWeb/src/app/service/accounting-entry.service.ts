import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import {
  AccountingEntryDto,
  CreateAccountingEntryRequest,
  UpdateAccountingEntryRequest,
  FiscalPeriodLookup,
  CurrencyLookup,
} from '../shared/models';

@Injectable({ providedIn: 'root' })
export class AccountingEntryService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('AccountingEntryService');
  private readonly base   = `${environment.apiUrl}accounting-entries`;
  private readonly fpBase = `${environment.apiUrl}fiscal-periods`;
  private readonly cuBase = `${environment.apiUrl}currencies`;

  // ── Estado ───────────────────────────────────────────────────────
  entries        = signal<AccountingEntryDto[]>([]);
  totalCount     = signal<number>(0);
  fiscalPeriods  = signal<FiscalPeriodLookup[]>([]);
  currencies     = signal<CurrencyLookup[]>([]);
  isLoading      = signal<boolean>(false);
  error          = signal<string | null>(null);

  clearError(): void { this.error.set(null); }

  private start(): void { this.isLoading.set(true); this.error.set(null); }
  private stop():  void { this.isLoading.set(false); }
  private fail(msg: string): void { this.error.set(msg); }

  // ── LISTAR asientos ──────────────────────────────────────────────
  loadList(): Observable<AccountingEntryDto[]> {
    this.start();
    return this.http.get<AccountingEntryDto[]>(`${this.base}/data.json`).pipe(
      tap(res => {
        const items = res ?? [];
        this.entries.set(items);
        this.totalCount.set(items.length);
        this.logger.info(`✅ Asientos cargados: ${items.length}`);
      }),
      catchError(err => {
        this.fail('Error al cargar los asientos contables');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── CARGAR períodos fiscales (lookup) ────────────────────────────
  loadFiscalPeriods(): Observable<FiscalPeriodLookup[]> {
    return this.http.get<FiscalPeriodLookup[]>(`${this.fpBase}/data.json`).pipe(
      tap(res => this.fiscalPeriods.set(res ?? [])),
      catchError(err => {
        this.logger.error('❌ Error al cargar períodos fiscales:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── CARGAR monedas (lookup) ───────────────────────────────────────
  loadCurrencies(): Observable<CurrencyLookup[]> {
    return this.http.get<CurrencyLookup[]>(`${this.cuBase}/data.json`).pipe(
      tap(res => this.currencies.set(res ?? [])),
      catchError(err => {
        this.logger.error('❌ Error al cargar monedas:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── CREAR ────────────────────────────────────────────────────────
  create(request: CreateAccountingEntryRequest): Observable<AccountingEntryDto> {
    this.start();
    return this.http.post<AccountingEntryDto>(`${this.base}/`, request).pipe(
      tap(res => {
        this.entries.update(ls => [res, ...ls]);
        this.totalCount.update(n => n + 1);
        this.logger.info(`✅ Asiento creado: ${res.numberEntry}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al crear el asiento';
        this.fail(typeof msg === 'string' ? msg : 'Error al crear el asiento');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── ACTUALIZAR ───────────────────────────────────────────────────
  update(id: number, request: UpdateAccountingEntryRequest): Observable<AccountingEntryDto> {
    this.start();
    return this.http.put<AccountingEntryDto>(`${this.base}/${id}`, request).pipe(
      tap(res => {
        this.entries.update(ls => ls.map(i => (i.idAccountingEntry === id ? res : i)));
        this.logger.info(`✅ Asiento actualizado: ${res.numberEntry}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al actualizar el asiento';
        this.fail(typeof msg === 'string' ? msg : 'Error al actualizar el asiento');
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
        this.entries.update(ls => ls.filter(i => i.idAccountingEntry !== id));
        this.totalCount.update(n => n - 1);
        this.logger.info(`✅ Asiento eliminado: ${id}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al eliminar el asiento';
        this.fail(typeof msg === 'string' ? msg : 'Error al eliminar el asiento');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }
}
