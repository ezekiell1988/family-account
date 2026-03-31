import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { ContactDto, GetOrCreateContactRequest } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class ContactService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('ContactService');
  private readonly base   = `${environment.apiUrl}contacts`;

  // ── Estado ───────────────────────────────────────────────────────
  providers = signal<ContactDto[]>([]);
  isLoading = signal<boolean>(false);

  private start(): void { this.isLoading.set(true); }
  private stop():  void { this.isLoading.set(false); }

  // ── Cargar proveedores ───────────────────────────────────────────
  loadProviders(): Observable<ContactDto[]> {
    if (this.providers().length > 0) {
      return new Observable(obs => { obs.next(this.providers()); obs.complete(); });
    }
    this.start();
    return this.http.get<ContactDto[]>(`${this.base}/data.json`, { params: { type: 'PRO' } }).pipe(
      tap(res => this.providers.set(res ?? [])),
      catchError(err => {
        this.logger.error('❌ Error al cargar proveedores:', err);
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── Obtener o crear proveedor ────────────────────────────────────
  getOrCreate(name: string): Observable<ContactDto> {
    const req: GetOrCreateContactRequest = { name, contactTypeCode: 'PRO' };
    return this.http.post<ContactDto>(`${this.base}/get-or-create`, req).pipe(
      tap(res => {
        // Agregar al cache local si no existe
        if (!this.providers().some(p => p.idContact === res.idContact)) {
          this.providers.update(list => [...list, res].sort((a, b) => a.name.localeCompare(b.name)));
        }
      }),
      catchError(err => {
        this.logger.error('❌ Error al crear proveedor:', err);
        return throwError(() => err);
      }),
    );
  }
}
