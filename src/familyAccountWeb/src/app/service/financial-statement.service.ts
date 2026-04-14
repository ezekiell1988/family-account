import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import {
  FinancialStatementFilter,
  IncomeStatementDto,
  BalanceSheetDto,
  CashFlowStatementDto,
  EquityStatementDto,
} from '../shared/models';

@Injectable({ providedIn: 'root' })
export class FinancialStatementService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('FinancialStatementService');
  private readonly base   = `${environment.apiUrl}financial-statements`;

  // ── Estado ───────────────────────────────────────────────────────
  incomeStatement  = signal<IncomeStatementDto | null>(null);
  balanceSheet     = signal<BalanceSheetDto | null>(null);
  cashFlow         = signal<CashFlowStatementDto | null>(null);
  equityStatement  = signal<EquityStatementDto | null>(null);
  isLoading        = signal<boolean>(false);
  error            = signal<string | null>(null);

  clearError(): void { this.error.set(null); }

  private start(): void { this.isLoading.set(true); this.error.set(null); }
  private stop():  void { this.isLoading.set(false); }
  private fail(msg: string): void { this.error.set(msg); }

  private buildParams(filter: FinancialStatementFilter): HttpParams {
    let params = new HttpParams();
    if (filter.idFiscalPeriod != null) params = params.set('idFiscalPeriod', filter.idFiscalPeriod);
    if (filter.dateFrom)               params = params.set('dateFrom', filter.dateFrom);
    if (filter.dateTo)                 params = params.set('dateTo', filter.dateTo);
    if (filter.year != null)           params = params.set('year', filter.year);
    if (filter.month != null)          params = params.set('month', filter.month);
    return params;
  }

  // ── Estado de Resultado ──────────────────────────────────────────
  loadIncomeStatement(filter: FinancialStatementFilter): Observable<IncomeStatementDto> {
    this.start();
    return this.http.get<IncomeStatementDto>(`${this.base}/income-statement.json`, { params: this.buildParams(filter) }).pipe(
      tap(res => {
        this.incomeStatement.set(res);
        this.logger.info('✅ Estado de Resultado cargado');
      }),
      catchError(err => {
        const msg = err?.error?.detail ?? 'Error al cargar el Estado de Resultado';
        this.fail(typeof msg === 'string' ? msg : 'Error al cargar el Estado de Resultado');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── Estado de Situación Financiera ───────────────────────────────
  loadBalanceSheet(filter: FinancialStatementFilter): Observable<BalanceSheetDto> {
    this.start();
    return this.http.get<BalanceSheetDto>(`${this.base}/balance-sheet.json`, { params: this.buildParams(filter) }).pipe(
      tap(res => {
        this.balanceSheet.set(res);
        this.logger.info('✅ Balance General cargado');
      }),
      catchError(err => {
        const msg = err?.error?.detail ?? 'Error al cargar el Estado de Situación Financiera';
        this.fail(typeof msg === 'string' ? msg : 'Error al cargar el Estado de Situación Financiera');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── Estado de Flujo de Efectivo ──────────────────────────────────
  loadCashFlow(filter: FinancialStatementFilter): Observable<CashFlowStatementDto> {
    this.start();
    return this.http.get<CashFlowStatementDto>(`${this.base}/cash-flow.json`, { params: this.buildParams(filter) }).pipe(
      tap(res => {
        this.cashFlow.set(res);
        this.logger.info('✅ Flujo de Efectivo cargado');
      }),
      catchError(err => {
        const msg = err?.error?.detail ?? 'Error al cargar el Estado de Flujo de Efectivo';
        this.fail(typeof msg === 'string' ? msg : 'Error al cargar el Estado de Flujo de Efectivo');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── Estado de Cambios en el Patrimonio ───────────────────────────
  loadEquityChanges(filter: FinancialStatementFilter): Observable<EquityStatementDto> {
    this.start();
    return this.http.get<EquityStatementDto>(`${this.base}/equity-changes.json`, { params: this.buildParams(filter) }).pipe(
      tap(res => {
        this.equityStatement.set(res);
        this.logger.info('✅ Cambios en el Patrimonio cargado');
      }),
      catchError(err => {
        const msg = err?.error?.detail ?? 'Error al cargar el Estado de Cambios en el Patrimonio';
        this.fail(typeof msg === 'string' ? msg : 'Error al cargar el Estado de Cambios en el Patrimonio');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }
}
