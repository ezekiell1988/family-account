import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import {
  PurchaseInvoiceDto,
  CreatePurchaseInvoiceRequest,
  UpdatePurchaseInvoiceRequest,
  FiscalPeriodLookup,
} from '../shared/models';

@Injectable({ providedIn: 'root' })
export class PurchaseInvoiceService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('PurchaseInvoiceService');
  private readonly base   = `${environment.apiUrl}purchase-invoices`;
  private readonly fpBase = `${environment.apiUrl}fiscal-periods`;

  // ── Estado ───────────────────────────────────────────────────────
  items         = signal<PurchaseInvoiceDto[]>([]);
  totalCount    = signal<number>(0);
  fiscalPeriods = signal<FiscalPeriodLookup[]>([]);
  isLoading     = signal<boolean>(false);
  error         = signal<string | null>(null);

  clearError(): void { this.error.set(null); }

  private start(): void { this.isLoading.set(true); this.error.set(null); }
  private stop():  void { this.isLoading.set(false); }
  private fail(msg: string): void { this.error.set(msg); }

  // ── PERÍODOS FISCALES ────────────────────────────────────────────
  loadFiscalPeriods(): Observable<FiscalPeriodLookup[]> {
    return this.http.get<FiscalPeriodLookup[]>(`${this.fpBase}/data.json`).pipe(
      tap(res => this.fiscalPeriods.set(res ?? [])),
      catchError(err => {
        this.logger.error('❌ Error al cargar períodos fiscales:', err);
        return throwError(() => err);
      }),
    );
  }

  // ── LISTAR ───────────────────────────────────────────────────────
  loadList(): Observable<PurchaseInvoiceDto[]> {
    this.start();
    return this.http.get<PurchaseInvoiceDto[]>(`${this.base}/data.json`).pipe(
      tap(res => {
        const items = res ?? [];
        this.items.set(items);
        this.totalCount.set(items.length);
        this.logger.info(`✅ Facturas de compra cargadas: ${items.length}`);
      }),
      catchError(err => {
        this.fail('Error al cargar las facturas de compra');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── POR PERÍODO ──────────────────────────────────────────────────
  loadByPeriod(idFiscalPeriod: number): Observable<PurchaseInvoiceDto[]> {
    this.start();
    return this.http.get<PurchaseInvoiceDto[]>(`${this.base}/by-period/${idFiscalPeriod}.json`).pipe(
      tap(res => {
        const items = res ?? [];
        this.items.set(items);
        this.totalCount.set(items.length);
        this.logger.info(`✅ Facturas del período ${idFiscalPeriod}: ${items.length}`);
      }),
      catchError(err => {
        this.fail('Error al cargar facturas del período');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── CREAR ────────────────────────────────────────────────────────
  create(request: CreatePurchaseInvoiceRequest): Observable<PurchaseInvoiceDto> {
    this.start();
    return this.http.post<PurchaseInvoiceDto>(`${this.base}/`, request).pipe(
      tap(res => {
        this.items.update(ls => [res, ...ls]);
        this.totalCount.update(n => n + 1);
        this.logger.info(`✅ Factura creada: ${res.numberInvoice}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al crear la factura de compra';
        this.fail(typeof msg === 'string' ? msg : 'Error al crear la factura de compra');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── ACTUALIZAR ───────────────────────────────────────────────────
  update(id: number, request: UpdatePurchaseInvoiceRequest): Observable<PurchaseInvoiceDto> {
    this.start();
    return this.http.put<PurchaseInvoiceDto>(`${this.base}/${id}`, request).pipe(
      tap(res => {
        this.items.update(ls => ls.map(i => (i.idPurchaseInvoice === id ? res : i)));
        this.logger.info(`✅ Factura actualizada: ${res.numberInvoice}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al actualizar la factura de compra';
        this.fail(typeof msg === 'string' ? msg : 'Error al actualizar la factura de compra');
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
        this.items.update(ls => ls.filter(i => i.idPurchaseInvoice !== id));
        this.totalCount.update(n => n - 1);
        this.logger.info(`✅ Factura eliminada: ${id}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al eliminar la factura de compra';
        this.fail(typeof msg === 'string' ? msg : 'Error al eliminar la factura de compra');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── CONFIRMAR ────────────────────────────────────────────────────
  confirm(id: number): Observable<PurchaseInvoiceDto> {
    this.start();
    return this.http.post<PurchaseInvoiceDto>(`${this.base}/${id}/confirm`, {}).pipe(
      tap(res => {
        this.items.update(ls => ls.map(i => (i.idPurchaseInvoice === id ? res : i)));
        this.logger.info(`✅ Factura confirmada: ${id}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al confirmar la factura';
        this.fail(typeof msg === 'string' ? msg : 'Error al confirmar la factura');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── ANULAR ───────────────────────────────────────────────────────
  cancel(id: number): Observable<PurchaseInvoiceDto> {
    this.start();
    return this.http.post<PurchaseInvoiceDto>(`${this.base}/${id}/cancel`, {}).pipe(
      tap(res => {
        this.items.update(ls => ls.map(i => (i.idPurchaseInvoice === id ? res : i)));
        this.logger.info(`✅ Factura anulada: ${id}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al anular la factura';
        this.fail(typeof msg === 'string' ? msg : 'Error al anular la factura');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }
}
