import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  EmployeeRoleMatrix,
  RolePermissionMatrix,
  SaveRolePermissionsRequest,
  SaveUserRolesRequest
} from './rbac.models';

@Injectable({ providedIn: 'root' })
export class RbacService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = `${environment.apiUrl}api`;

  getEmployeeRoleMatrix(query: { pageNumber: number; pageSize: number; search?: string }): Observable<EmployeeRoleMatrix> {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber)
      .set('pageSize', query.pageSize);
    if (query.search) params = params.set('search', query.search);
    return this.http
      .get<ApiResponse<EmployeeRoleMatrix>>(`${this.apiBaseUrl}/user-roles/matrix`, { params })
      .pipe(map((response) => response.data));
  }

  saveEmployeeRoleMatrix(request: SaveUserRolesRequest): Observable<ApiResponse<unknown>> {
    return this.http.put<ApiResponse<unknown>>(`${this.apiBaseUrl}/user-roles/matrix`, request);
  }

  getRolePermissionMatrix(query: { pageNumber: number; pageSize: number; search?: string }): Observable<RolePermissionMatrix> {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber)
      .set('pageSize', query.pageSize);
    if (query.search) params = params.set('search', query.search);
    return this.http
      .get<ApiResponse<RolePermissionMatrix>>(`${this.apiBaseUrl}/role-permissions/matrix`, { params })
      .pipe(map((response) => response.data));
  }

  saveRolePermissionMatrix(request: SaveRolePermissionsRequest): Observable<ApiResponse<unknown>> {
    return this.http.put<ApiResponse<unknown>>(`${this.apiBaseUrl}/role-permissions/matrix`, request);
  }
}
