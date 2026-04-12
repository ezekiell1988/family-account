import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import {
  BankStatementImportDto,
  BankStatementTransactionDto,
  BulkClassifyRequest,
  BulkClassifyResult,
  ClassifyTransactionRequest,
} from '../shared/models';

@Injectable({ providedIn: 'root' })
export class BankStatementImportService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('BankStatementImportService');
  private readonly base   = `${environment.apiUrl}bank-statement-imports`;
  private readonly txBase = `${environment.apiUrl}bank-statement-transactions`;

  // ── Estado ───────────────────────────────────────────────────────
  items        = signal<BankStatementImportDto[]>([]);
  transactions = signal<BankStatementTransactionDto[]>([]);
  isLoading    = signal<boolean>(false);
  isUploading  = signal<boolean>(false);
  isPolling    = signal<boolean>(false);
  isBulkClassifying = signal<boolean>(false);
  error        = signal<string | null>(null);

  clearError(): void { this.error.set(null); }

  private start(): void { this.isLoading.set(true); this.error.set(null); }
  private stop():  void { this.isLoading.set(false); }
  private fail(msg: string): void { this.error.set(msg); }

  // ── LISTAR imports ────────────────────────────────────────────────
  loadList(): Observable<BankStatementImportDto[]> {
    this.start();
    return this.http.get<BankStatementImportDto[]>(`${this.base}/data.json`).pipe(
      tap(res => this.items.set(res ?? [])),
      catchError(err => {
        this.fail('Error al cargar importaciones bancarias');
        this.logger.error('Error cargando importaciones', err);
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── UPLOAD ────────────────────────────────────────────────────────
  upload(
    idBankAccount: number,
    idTemplate: number,
    file: File,
  ): Observable<BankStatementImportDto> {
    this.isUploading.set(true);
    this.error.set(null);
    const formData = new FormData();
    formData.append('file', file);
    return this.http
      .post<BankStatementImportDto>(
        `${this.base}/upload/${idBankAccount}/${idTemplate}`,
        formData,
      )
      .pipe(
        tap(res => this.items.update(ls => [res, ...ls])),
        catchError(err => {
          const msg = err?.error ?? 'Error al cargar el archivo';
          this.fail(typeof msg === 'string' ? msg : 'Error al cargar el archivo');
          this.logger.error('Error en upload', err);
          return throwError(() => err);
        }),
        finalize(() => this.isUploading.set(false)),
      );
  }

  // ── POLLING por ID ────────────────────────────────────────────────
  pollById(id: number): Observable<BankStatementImportDto> {
    this.isPolling.set(true);
    return this.http.get<BankStatementImportDto>(`${this.base}/${id}.json`).pipe(
      tap(res =>
        this.items.update(ls =>
          ls.map(i => (i.idBankStatementImport === id ? res : i)),
        ),
      ),
      catchError(err => {
        this.logger.error('Error en polling', err);
        return throwError(() => err);
      }),
      finalize(() => this.isPolling.set(false)),
    );
  }

  // ── TRANSACCIONES por import ──────────────────────────────────────
  loadTransactions(importId: number): Observable<BankStatementTransactionDto[]> {
    this.start();
    this.transactions.set([]);
    return this.http
      .get<BankStatementTransactionDto[]>(`${this.txBase}/import/${importId}.json`)
      .pipe(
        tap(res => this.transactions.set(res ?? [])),
        catchError(err => {
          this.fail('Error al cargar transacciones');
          this.logger.error('Error cargando transacciones', err);
          return throwError(() => err);
        }),
        finalize(() => this.stop()),
      );
  }

  // ── CLASIFICAR transacción manual ─────────────────────────────────
  classifyTransaction(
    id: number,
    req: ClassifyTransactionRequest,
  ): Observable<BankStatementTransactionDto> {
    this.isBulkClassifying.set(true);
    return this.http
      .patch<BankStatementTransactionDto>(`${this.txBase}/${id}/classify`, req)
      .pipe(
        tap(res =>
          this.transactions.update(ls =>
            ls.map(t => (t.idBankStatementTransaction === id ? res : t)),
          ),
        ),
        catchError(err => {
          const msg = err?.error ?? 'Error al clasificar la transacción';
          this.fail(typeof msg === 'string' ? msg : 'Error al clasificar la transacción');
          this.logger.error('Error clasificando transacción', err);
          return throwError(() => err);
        }),
        finalize(() => this.isBulkClassifying.set(false)),
      );
  }

  // ── CLASIFICAR masivamente (bulk) ─────────────────────────────────
  classifyBatch(
    importId: number,
    req: BulkClassifyRequest,
  ): Observable<BulkClassifyResult> {
    this.isBulkClassifying.set(true);
    return this.http
      .post<BulkClassifyResult>(`${this.base}/${importId}/classify-batch`, req)
      .pipe(
        catchError(err => {
          const msg = err?.error ?? 'Error al clasificar transacciones';
          this.fail(typeof msg === 'string' ? msg : 'Error al clasificar transacciones');
          this.logger.error('Error en bulk classify', err);
          return throwError(() => err);
        }),
        finalize(() => this.isBulkClassifying.set(false)),
      );
  }
}
