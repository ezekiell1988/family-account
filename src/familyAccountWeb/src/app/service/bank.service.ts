import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { BankDto, CreateBankRequest, UpdateBankRequest } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class BankService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('BankService');
  private readonly base   = `${environment.apiUrl}banks`;

  banks      = signal<BankDto[]>([]);
  totalCount = signal<number>(0);
  isLoading  = signal<boolean>(false);
  error      = signal<string | null>(null);

  clearError(): void { this.error.set(null); }
  private start(): void { this.isLoading.set(true); this.error.set(null); }
  private stop():  void { this.isLoading.set(false); }
  private fail(msg: string): void { this.error.set(msg); }

  loadList(): Observable<BankDto[]> {
    this.start();
    return this.http.get<BankDto[]>(`${this.base}/data.json`).pipe(
      tap(res => {
        const items = res ?? [];
        this.banks.set(items);
        this.totalCount.set(items.length);
      }),
      catchError(err => {
        this.fail('Error al cargar los bancos');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  create(request: CreateBankRequest): Observable<BankDto> {
    this.start();
    return this.http.post<BankDto>(`${this.base}/`, request).pipe(
      tap(res => {
        this.banks.update(ls => [...ls, res].sort((a, b) => a.codeBank.localeCompare(b.codeBank)));
        this.totalCount.update(n => n + 1);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al crear el banco';
        this.fail(typeof msg === 'string' ? msg : 'Error al crear el banco');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  update(id: number, request: UpdateBankRequest): Observable<BankDto> {
    this.start();
    return this.http.put<BankDto>(`${this.base}/${id}`, request).pipe(
      tap(res => {
        this.banks.update(ls =>
          ls.map(b => (b.idBank === id ? res : b)).sort((a, b) => a.codeBank.localeCompare(b.codeBank)),
        );
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al actualizar el banco';
        this.fail(typeof msg === 'string' ? msg : 'Error al actualizar el banco');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  delete(id: number): Observable<void> {
    this.start();
    return this.http.delete<void>(`${this.base}/${id}`).pipe(
      tap(() => {
        this.banks.update(ls => ls.filter(b => b.idBank !== id));
        this.totalCount.update(n => n - 1);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al eliminar el banco';
        this.fail(typeof msg === 'string' ? msg : 'Error al eliminar el banco');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }
}
