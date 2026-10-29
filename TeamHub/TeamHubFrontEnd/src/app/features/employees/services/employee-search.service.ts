import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { EmployeeCard, EmployeeCardFilters } from '../models/employee-card.model';

interface ApiResponse<T> {
  data: T;
  success: boolean;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class EmployeeSearchService {
  private readonly apiUrl = `${environment.apiUrl}api/employee-search`;

  constructor(private readonly http: HttpClient) {}

  searchEmployees(
    filters: EmployeeCardFilters,
    pageNumber = 1,
    pageSize = 10
  ): Observable<PagedResult<EmployeeCard>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    const optional: Array<[string, string | number | null | undefined]> = [
      ['name', filters.name],
      ['designation', filters.designation],
      ['departmentId', filters.departmentId],
      ['contact', filters.contact]
    ];

    for (const [key, value] of optional) {
      if (value === null || value === undefined || value === '') continue;
      params = params.set(key, value);
    }

    return this.http
      .get<ApiResponse<PagedResult<EmployeeCard>>>(this.apiUrl, { params })
      .pipe(map((response) => response.data));
  }
}
