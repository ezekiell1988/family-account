import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { ProductUnitDto, CreateProductUnitRequest, UpdateProductUnitRequest } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class ProductUnitService {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('ProductUnitService');
  private readonly base   = `${environment.apiUrl}product-units`;

  getByProduct(idProduct: number): Observable<ProductUnitDto[]> {
    return this.http.get<ProductUnitDto[]>(`${this.base}/by-product/${idProduct}.json`).pipe(
      catchError(err => {
        this.logger.error('❌ Error al cargar presentaciones:', err);
        return throwError(() => err);
      }),
    );
  }

  create(req: CreateProductUnitRequest): Observable<ProductUnitDto> {
    return this.http.post<ProductUnitDto>(`${this.base}/`, req).pipe(
      catchError(err => {
        this.logger.error('❌ Error al crear presentación:', err);
        return throwError(() => err);
      }),
    );
  }

  update(id: number, req: UpdateProductUnitRequest): Observable<ProductUnitDto> {
    return this.http.put<ProductUnitDto>(`${this.base}/${id}`, req).pipe(
      catchError(err => {
        this.logger.error('❌ Error al actualizar presentación:', err);
        return throwError(() => err);
      }),
    );
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`).pipe(
      catchError(err => {
        this.logger.error('❌ Error al eliminar presentación:', err);
        return throwError(() => err);
      }),
    );
  }
}
