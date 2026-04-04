import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import {
  ProductOptionGroupDto,
  CreateProductOptionGroupRequest,
  UpdateProductOptionGroupRequest,
} from '../shared/models';

@Injectable({ providedIn: 'root' })
export class ProductOptionGroupService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('ProductOptionGroupService');
  private readonly base   = `${environment.apiUrl}product-option-groups`;

  getByProduct(idProduct: number): Observable<ProductOptionGroupDto[]> {
    return this.http.get<ProductOptionGroupDto[]>(`${this.base}/by-product/${idProduct}.json`).pipe(
      catchError(err => {
        this.logger.error('❌ Error al cargar grupos de opciones:', err);
        return throwError(() => err);
      }),
    );
  }

  create(req: CreateProductOptionGroupRequest): Observable<ProductOptionGroupDto> {
    return this.http.post<ProductOptionGroupDto>(`${this.base}/`, req).pipe(
      catchError(err => {
        this.logger.error('❌ Error al crear grupo de opciones:', err);
        return throwError(() => err);
      }),
    );
  }

  update(id: number, req: UpdateProductOptionGroupRequest): Observable<ProductOptionGroupDto> {
    return this.http.put<ProductOptionGroupDto>(`${this.base}/${id}`, req).pipe(
      catchError(err => {
        this.logger.error('❌ Error al actualizar grupo de opciones:', err);
        return throwError(() => err);
      }),
    );
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`).pipe(
      catchError(err => {
        this.logger.error('❌ Error al eliminar grupo de opciones:', err);
        return throwError(() => err);
      }),
    );
  }
}
