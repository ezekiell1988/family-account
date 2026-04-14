import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import {
  BankStatementTemplateDto,
  CreateBankStatementTemplateRequest,
  UpdateBankStatementTemplateRequest,
} from '../shared/models';

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

  // ── CREAR ────────────────────────────────────────────────────────
  create(payload: CreateBankStatementTemplateRequest): Observable<BankStatementTemplateDto> {
    this.start();
    return this.http.post<BankStatementTemplateDto>(`${this.base}`, payload).pipe(
      tap(res => {
        this.items.update(list => [res, ...list]);
        this.logger.info(`✅ Plantilla creada: ${res.codeTemplate}`);
      }),
      catchError(err => {
        this.fail('Error al crear plantilla bancaria');
        this.logger.error('Error creando plantilla', err);
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── ACTUALIZAR ───────────────────────────────────────────────────
  update(id: number, payload: UpdateBankStatementTemplateRequest): Observable<BankStatementTemplateDto> {
    this.start();
    return this.http.put<BankStatementTemplateDto>(`${this.base}/${id}`, payload).pipe(
      tap(res => {
        this.items.update(list =>
          list.map(i => i.idBankStatementTemplate === id ? res : i),
        );
        this.logger.info(`✅ Plantilla actualizada: ${res.codeTemplate}`);
      }),
      catchError(err => {
        this.fail('Error al actualizar plantilla bancaria');
        this.logger.error('Error actualizando plantilla', err);
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
        this.items.update(list =>
          list.filter(i => i.idBankStatementTemplate !== id),
        );
        this.logger.info(`✅ Plantilla eliminada id=${id}`);
      }),
      catchError(err => {
        this.fail('Error al eliminar plantilla bancaria');
        this.logger.error('Error eliminando plantilla', err);
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }
}
