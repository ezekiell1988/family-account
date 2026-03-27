import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { BankMovementTypeDto, CreateBankMovementTypeRequest, UpdateBankMovementTypeRequest } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class BankMovementTypeService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('BankMovementTypeService');
  private readonly base   = `${environment.apiUrl}bank-movement-types`;

  items      = signal<BankMovementTypeDto[]>([]);
  totalCount = signal<number>(0);
  isLoading  = signal<boolean>(false);
  error      = signal<string | null>(null);

  clearError(): void { this.error.set(null); }
  private start(): void { this.isLoading.set(true); this.error.set(null); }
  private stop():  void { this.isLoading.set(false); }
  private fail(msg: string): void { this.error.set(msg); }

  loadList(): Observable<BankMovementTypeDto[]> {
    this.start();
    return this.http.get<BankMovementTypeDto[]>(`${this.base}/data.json`).pipe(
      tap(res => {
        const items = res ?? [];
        this.items.set(items);
        this.totalCount.set(items.length);
      }),
      catchError(err => {
        this.fail('Error al cargar los tipos de movimiento bancario');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  create(request: CreateBankMovementTypeRequest): Observable<BankMovementTypeDto> {
    this.start();
    return this.http.post<BankMovementTypeDto>(`${this.base}/`, request).pipe(
      tap(res => {
        this.items.update(ls =>
          [...ls, res].sort((a, b) => a.codeBankMovementType.localeCompare(b.codeBankMovementType)),
        );
        this.totalCount.update(n => n + 1);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al crear el tipo de movimiento bancario';
        this.fail(typeof msg === 'string' ? msg : 'Error al crear el tipo de movimiento bancario');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  update(id: number, request: UpdateBankMovementTypeRequest): Observable<BankMovementTypeDto> {
    this.start();
    return this.http.put<BankMovementTypeDto>(`${this.base}/${id}`, request).pipe(
      tap(res => {
        this.items.update(ls =>
          ls.map(i => (i.idBankMovementType === id ? res : i))
            .sort((a, b) => a.codeBankMovementType.localeCompare(b.codeBankMovementType)),
        );
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al actualizar el tipo de movimiento bancario';
        this.fail(typeof msg === 'string' ? msg : 'Error al actualizar el tipo de movimiento bancario');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  delete(id: number): Observable<void> {
    this.start();
    return this.http.delete<void>(`${this.base}/${id}`).pipe(
      tap(() => {
        this.items.update(ls => ls.filter(i => i.idBankMovementType !== id));
        this.totalCount.update(n => n - 1);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al eliminar el tipo de movimiento bancario';
        this.fail(typeof msg === 'string' ? msg : 'Error al eliminar el tipo de movimiento bancario');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }
}
