import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';

import { EmployeeHierarchyResponse } from './models';
import { environment } from '../../../../../environments/environment';

interface ApiResponse<T> {
  data: T;
  success: boolean;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class EmployeeHierarchyService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}api/employees`;

  getHierarchy(employeeId: number): Observable<EmployeeHierarchyResponse> {
    return this.http
      .get<ApiResponse<EmployeeHierarchyResponse>>(`${this.apiUrl}/${employeeId}/hierarchy`)
      .pipe(map((response) => response.data));
  }
}
