import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { LoginDto, CreateLoginRequest, UpdateLoginRequest } from '../shared/models';
import { PagedResult } from '../shared/models';

@Injectable({
  providedIn: 'root',
})
export class LoginService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('LoginService');
  private readonly base   = `${environment.apiUrl}Login`;

  // ── Estado ───────────────────────────────────────────────────────
  logins     = signal<LoginDto[]>([]);
  totalCount = signal<number>(0);
  isLoading  = signal<boolean>(false);
  error      = signal<string | null>(null);

  hasLogins = computed(() => this.logins().length > 0);

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
  loadList(page = 0, pageSize = 100): Observable<PagedResult<LoginDto>> {
    this.start();
    return this.http.get<PagedResult<LoginDto>>(`${this.base}?page=${page}&pageSize=${pageSize}`).pipe(
      tap(res => {
        this.logins.set(res.data ?? []);
        this.totalCount.set(res.totalCount ?? 0);
        this.logger.info(`✅ Usuarios cargados: ${res.totalCount}`);
      }),
      catchError(err => {
        this.fail('Error al cargar los usuarios');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── CREAR ────────────────────────────────────────────────────────
  create(request: CreateLoginRequest): Observable<LoginDto> {
    this.start();
    return this.http.post<LoginDto>(this.base, request).pipe(
      tap(res => {
        this.logins.update(ls => [...ls, res]);
        this.totalCount.update(n => n + 1);
        this.logger.info(`✅ Usuario creado: ${res.codeLogin}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al crear el usuario';
        this.fail(typeof msg === 'string' ? msg : 'Error al crear el usuario');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── ACTUALIZAR ───────────────────────────────────────────────────
  update(id: number, request: UpdateLoginRequest): Observable<LoginDto> {
    this.start();
    return this.http.put<LoginDto>(`${this.base}/${id}`, request).pipe(
      tap(res => {
        this.logins.update(ls => ls.map(l => l.idLogin === id ? res : l));
        this.logger.info(`✅ Usuario actualizado: ${res.codeLogin}`);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al actualizar el usuario';
        this.fail(typeof msg === 'string' ? msg : 'Error al actualizar el usuario');
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
        this.logins.update(ls => ls.filter(l => l.idLogin !== id));
        this.totalCount.update(n => Math.max(0, n - 1));
        this.logger.info(`✅ Usuario eliminado ID ${id}`);
      }),
      catchError(err => {
        this.fail('Error al eliminar el usuario');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }
}
