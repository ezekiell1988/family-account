import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import {
  BankMovementDto,
  CreateBankMovementRequest,
  UpdateBankMovementRequest,
} from '../shared/models';
import { BankMovementTypeDto } from '../shared/models';
import { FiscalPeriodLookup } from '../shared/models';
import { BankAccountDto } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class BankMovementService {
  private readonly http    = inject(HttpClient);
  private readonly logger  = inject(LoggerService).getLogger('BankMovementService');
  private readonly base    = `${environment.apiUrl}bank-movements`;
  private readonly baBase  = `${environment.apiUrl}bank-accounts`;
  private readonly bmtBase = `${environment.apiUrl}bank-movement-types`;
  private readonly fpBase  = `${environment.apiUrl}fiscal-periods`;

  // ── Estado ───────────────────────────────────────────────────────
  items         = signal<BankMovementDto[]>([]);
  totalCount    = signal<number>(0);
  bankAccounts  = signal<BankAccountDto[]>([]);
  movementTypes = signal<BankMovementTypeDto[]>([]);
  fiscalPeriods = signal<FiscalPeriodLookup[]>([]);
  isLoading     = signal<boolean>(false);
  error         = signal<string | null>(null);

  clearError(): void { this.error.set(null); }

  private start(): void { this.isLoading.set(true); this.error.set(null); }
  private stop():  void { this.isLoading.set(false); }
  private fail(msg: string): void { this.error.set(msg); }

  // ── LISTAR ───────────────────────────────────────────────────────
  loadList(): Observable<BankMovementDto[]> {
    this.start();
    return this.http.get<BankMovementDto[]>(`${this.base}/data.json`).pipe(
      tap(res => {
        const items = res ?? [];
        this.items.set(items);
        this.totalCount.set(items.length);
        this.logger.info(`✅ Movimientos bancarios cargados: ${items.length}`);
      }),
      catchError(err => {
        this.fail('Error al cargar los movimientos bancarios');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── CATÁLOGOS ────────────────────────────────────────────────────
  loadBankAccounts(): Observable<BankAccountDto[]> {
    return this.http.get<BankAccountDto[]>(`${this.baBase}/data.json`).pipe(
      tap(res => this.bankAccounts.set(res ?? [])),
      catchError(err => {
        this.logger.error('❌ Error al cargar cuentas bancarias:', err);
        return throwError(() => err);
      }),
    );
  }

  loadMovementTypes(): Observable<BankMovementTypeDto[]> {
    return this.http.get<BankMovementTypeDto[]>(`${this.bmtBase}/data.json`).pipe(
      tap(res => this.movementTypes.set(res ?? [])),
      catchError(err => {
        this.logger.error('❌ Error al cargar tipos de movimiento:', err);
        return throwError(() => err);
      }),
    );
  }

  loadFiscalPeriods(): Observable<FiscalPeriodLookup[]> {
    return this.http.get<FiscalPeriodLookup[]>(`${this.fpBase}/data.json`).pipe(
      tap(res => this.fiscalPeriods.set(res ?? [])),
      catchError(err => {
        this.logger.error('❌ Error al cargar períodos fiscales:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── CREAR ────────────────────────────────────────────────────────
  create(request: CreateBankMovementRequest): Observable<BankMovementDto> {
    this.start();
    return this.http.post<BankMovementDto>(`${this.base}/`, request).pipe(
      tap(res => {
        this.items.update(ls => [res, ...ls]);
        this.totalCount.update(n => n + 1);
        this.logger.info(`✅ Movimiento creado: ${res.numberMovement}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al crear el movimiento bancario';
        this.fail(typeof msg === 'string' ? msg : 'Error al crear el movimiento bancario');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── ACTUALIZAR ───────────────────────────────────────────────────
  update(id: number, request: UpdateBankMovementRequest): Observable<BankMovementDto> {
    this.start();
    return this.http.put<BankMovementDto>(`${this.base}/${id}`, request).pipe(
      tap(res => {
        this.items.update(ls => ls.map(i => (i.idBankMovement === id ? res : i)));
        this.logger.info(`✅ Movimiento actualizado: ${res.numberMovement}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al actualizar el movimiento bancario';
        this.fail(typeof msg === 'string' ? msg : 'Error al actualizar el movimiento bancario');
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
        this.items.update(ls => ls.filter(i => i.idBankMovement !== id));
        this.totalCount.update(n => n - 1);
        this.logger.info(`✅ Movimiento eliminado: ${id}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al eliminar el movimiento bancario';
        this.fail(typeof msg === 'string' ? msg : 'Error al eliminar el movimiento bancario');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── CONFIRMAR ────────────────────────────────────────────────────
  confirm(id: number): Observable<BankMovementDto> {
    this.start();
    return this.http.post<BankMovementDto>(`${this.base}/${id}/confirm`, {}).pipe(
      tap(res => {
        this.items.update(ls => ls.map(i => (i.idBankMovement === id ? res : i)));
        this.logger.info(`✅ Movimiento confirmado: ${id}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al confirmar el movimiento';
        this.fail(typeof msg === 'string' ? msg : 'Error al confirmar el movimiento');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── ANULAR ───────────────────────────────────────────────────────
  cancel(id: number): Observable<BankMovementDto> {
    this.start();
    return this.http.post<BankMovementDto>(`${this.base}/${id}/cancel`, {}).pipe(
      tap(res => {
        this.items.update(ls => ls.map(i => (i.idBankMovement === id ? res : i)));
        this.logger.info(`✅ Movimiento anulado: ${id}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al anular el movimiento';
        this.fail(typeof msg === 'string' ? msg : 'Error al anular el movimiento');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }
}
