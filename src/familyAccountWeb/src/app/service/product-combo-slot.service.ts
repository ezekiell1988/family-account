import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import {
  ProductComboSlotDto,
  CreateProductComboSlotRequest,
  UpdateProductComboSlotRequest,
} from '../shared/models';

@Injectable({ providedIn: 'root' })
export class ProductComboSlotService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('ProductComboSlotService');
  private readonly base   = `${environment.apiUrl}product-combo-slots`;

  getByCombo(idProductCombo: number): Observable<ProductComboSlotDto[]> {
    return this.http.get<ProductComboSlotDto[]>(`${this.base}/by-combo/${idProductCombo}.json`).pipe(
      catchError(err => {
        this.logger.error('❌ Error al cargar slots de combo:', err);
        return throwError(() => err);
      }),
    );
  }

  create(req: CreateProductComboSlotRequest): Observable<ProductComboSlotDto> {
    return this.http.post<ProductComboSlotDto>(`${this.base}/`, req).pipe(
      catchError(err => {
        this.logger.error('❌ Error al crear slot de combo:', err);
        return throwError(() => err);
      }),
    );
  }

  update(id: number, req: UpdateProductComboSlotRequest): Observable<ProductComboSlotDto> {
    return this.http.put<ProductComboSlotDto>(`${this.base}/${id}`, req).pipe(
      catchError(err => {
        this.logger.error('❌ Error al actualizar slot de combo:', err);
        return throwError(() => err);
      }),
    );
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`).pipe(
      catchError(err => {
        this.logger.error('❌ Error al eliminar slot de combo:', err);
        return throwError(() => err);
      }),
    );
  }
}
