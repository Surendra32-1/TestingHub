import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { EMPTY, expand, map, Observable, reduce, shareReplay } from 'rxjs';

import { Department } from '../models/employee-master.model';
import { Employee } from '../models/employee.model';
import { DepartmentTree } from '../models/organization-tree.model';
import { HttpParams } from '@angular/common/http';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { environment } from '../../../../environments/environment';

interface ApiResponse<T> {
  data: T;
  success: boolean;
  message: string;
}

export interface CreateEmployeeRequest {
  eNumber: number;
  firstName: string;
  middleName: string | null;
  lastName: string;
  joinedDate: string;
  joinedDateBS: string;
  email: string;
  alternateEmail: string | null;
  designationId: number;
  departmentId: number;
  jobTypeId: number;
  bloodGroupId: number;
  genderId: number;
  reportsToEmployeeId: number | null;
  leadEmployeeId: number | null;
  stationNumber: string | null;
  recruitmentId: number | null;
  extensionNumber: number;
  citizenshipNumber: string;
  dateOfBirth: string | null;
  dateOfBirthBS: string | null;
  phoneNumber: string | null;
  mobileNumber: string | null;
  emergencyContactNumber: string | null;
  emergencyContactPerson: string;
  fatherName: string;
  motherName: string | null;
  spouseName: string | null;
  panNumber: string;
  lockerNumber: string | null;
  roleIds: number[];
  teamId: number;
  cotivityEmployeeId: number | null;
  imageBase64: string | null;
  isActive: boolean;
  addresses: Array<{
    addressTypeId: number;
    addressLine1: string;
    addressLine2: string | null;
    city: string;
    state: string;
    latitude: number;
    longitude: number;
    isPrimary: boolean;
  }>;
  skills: Array<{ skillId: number; skillLevel: number; isPrimary: boolean }>;
  dressSizes: Array<{ dressId: number; dressName: string; size: string; isActive: boolean }>;
}

export interface EmployeeEditResponse extends Omit<CreateEmployeeRequest, 'addresses' | 'skills' | 'dressSizes'> {
  employeeId: number;
  designationName: string | null;
  departmentName: string | null;
  jobTypeName: string | null;
  bloodGroupName: string | null;
  genderName: string | null;
  reportsToEmployeeName: string | null;
  leadEmployeeName: string | null;
  recruitmentName: string | null;
  roleName: string | null;
  teamName: string | null;
  cotivityEmployeeName: string | null;
  addresses: Array<{
    addressId: number;
    addressTypeId: number;
    addressTypeName: string;
    addressLine1: string;
    addressLine2: string | null;
    city: string;
    state: string;
    latitude: number;
    longitude: number;
    isPrimary: boolean;
  }>;
  skills: Array<{
    employeeSkillId: number;
    skillId: number;
    skillName: string;
    skillLevel: number;
    isPrimary: boolean;
  }>;
  dressSizes: Array<{
    dressId: number;
    dressName: string;
    size: string;
    isActive: boolean;
  }>;
}

export interface EmployeeReleaseRequest {
  releaseType: 'Resignation' | 'Retirement' | 'Contract End' | 'Internal Transfer' | 'Mutual Separation';
  resignationDate: string | null;
  noticeDate: string | null;
  lastWorkingDate: string | null;
  noticePeriodDays: number | null;
  servedNoticeDays: number | null;
  reason: string | null;
  earlyReleaseReason?: string | null;
  remarks: string | null;
  knowledgeTransferCompleted: boolean;
  assetsReturned: boolean;
  exitInterviewCompleted: boolean;
  settlementAmount: number | null;
  relievingLetterIssued: boolean;
  experienceLetterIssued: boolean;
  status: 'Draft' | 'Submitted' | 'PendingApproval' | 'Approved' | 'Released' | 'Rejected' | 'Cancelled';
}

export type ReleaseStatus = EmployeeReleaseRequest['status'];

export interface ReleaseClearanceUpdate {
  knowledgeTransferCompleted?: boolean;
  assetsReturned?: boolean;
  exitInterviewCompleted?: boolean;
  relievingLetterIssued?: boolean;
  experienceLetterIssued?: boolean;
  settlementAmount?: number | null;
}

export interface EmployeeTerminationRequest {
  terminationType: 'Performance' | 'Policy Violation' | 'Misconduct' | 'Layoff' | 'Contract End' | 'Attendance' | 'Other'
    | 'PolicyViolation' | 'Fraud' | 'Absenteeism' | 'Restructuring';
  effectiveDate: string | null;
  investigationRequired: boolean;
  investigationSummary: string | null;
  warningCount: number | null;
  lastWarningDate: string | null;
  reason: string | null;
  remarks: string | null;
  assetRecovered: boolean;
  systemAccessRevoked: boolean;
  exitInterviewCompleted: boolean;
  settlementAmount: number | null;
  eligibleForRehire: boolean;
  terminationLetterIssued: boolean;
  terminationLetterDate: string | null;
  status: 'Draft' | 'Submitted' | 'PendingApproval' | 'Approved' | 'Terminated' | 'Rejected' | 'Cancelled'
    | 'Pending Approval' | 'Completed' | 'UnderInvestigation';
}

export type TerminationStatus = EmployeeTerminationRequest['status'];

export interface TerminationClearanceUpdate {
  assetRecovered?: boolean;
  systemAccessRevoked?: boolean;
  exitInterviewCompleted?: boolean;
  terminationLetterIssued?: boolean;
  settlementAmount?: number | null;
}

export interface EmployeeReleaseRecord extends EmployeeReleaseRequest {
  releasedId: number;
  employeeId: number;
  approvedBy: string | null;
  approvedDate: string | null;
  createdBy: string | null;
  createdDate: string | null;
  lastModifiedBy: string | null;
  lastModifiedDate: string | null;
  employeeName: string;
  employeeCode: number;
  department: string | null;
  designation: string | null;
  managerName: string | null;
}

export interface EmployeeTerminationRecord extends EmployeeTerminationRequest {
  terminationId: number;
  employeeId: number;
  approvedBy: string | null;
  approvedDate: string | null;
  createdBy: string | null;
  createdDate: string | null;
  lastModifiedBy: string | null;
  lastModifiedDate: string | null;
  employeeName: string;
  employeeCode: number;
  department: string | null;
  designation: string | null;
  managerName: string | null;
  joinedDate: string | null;
}

export interface ReleasePage<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export type EmployeeReleasePage = ReleasePage<EmployeeReleaseRecord>;
export type EmployeeTerminationPage = ReleasePage<EmployeeTerminationRecord>;

export interface EmployeeDetails {
  employeeId: number;
  eNumber: number;
  firstName: string;
  middleName: string | null;
  lastName: string;
  joinedDate: string;
  joinedDateBS: string | null;
  email: string;
  alternateEmail: string | null;
  designationName: string | null;
  departmentName: string | null;
  jobTypeName: string | null;
  bloodGroupName: string | null;
  genderName: string | null;
  reportsToEmployeeName: string | null;
  leadEmployeeName: string | null;
  stationNumber: string | null;
  recruitmentName: string | null;
  extensionNumber: number;
  citizenshipNumber: string;
  dateOfBirth: string | null;
  dateOfBirthBS: string | null;
  phoneNumber: string | null;
  mobileNumber: string | null;
  emergencyContactNumber: string | null;
  emergencyContactPerson: string;
  fatherName: string;
  motherName: string | null;
  spouseName: string | null;
  panNumber: string;
  lockerNumber: string | null;
  roleName: string | null;
  teamName: string | null;
  cotivityEmployeeName: string | null;
  isActive: boolean;
  imageBase64: string | null;
  addresses: Array<{
    addressId: number;
    addressTypeName: string;
    addressLine1: string;
    addressLine2: string | null;
    city: string;
    state: string;
    isPrimary: boolean;
  }>;
  skills: Array<{
    employeeSkillId: number;
    skillName: string;
    skillLevel: number;
    isPrimary: boolean;
  }>;
  dressSizes: Array<{
    dressId: number;
    dressName: string;
    size: string;
    isActive: boolean;
  }>;
}

interface EmployeeApiRecord {
  id: number;
  name: string;
  designation: string | null;
  designationRank: number;
  department: string | null;
  teamName: string | null;
  imageBase64: string | null;
}


@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  private readonly palette = ['#dbeafe', '#e0e7ff', '#dcfce7', '#fef3c7', '#fce7f3', '#fee2e2', '#ede9fe'];
  private organizationTree$?: Observable<DepartmentTree[]>;
  private readonly apiUrl = `${environment.apiUrl}api/employees`;

  constructor(private readonly http: HttpClient) {}

  getEmployees(
    pageNumber = 1,
    pageSize = 10,
    searchTerm = '',
    searchField: 'all' | 'name' | 'designation' | 'department' = 'all'
  ): Observable<PagedResult<Employee>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize)
      .set('searchTerm', searchTerm)
      .set('searchField', searchField);

    return this.http.get<ApiResponse<PagedResult<EmployeeApiRecord>>>(this.apiUrl, { params }).pipe(
      map((response) => this.mapEmployeePage(response.data))
    );
  }

  getActiveEmployeesByDepartmentId(
    departmentId: number,
    pageNumber = 1,
    pageSize = 10
  ): Observable<PagedResult<Employee>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    return this.http
      .get<ApiResponse<PagedResult<EmployeeApiRecord>>>(`${this.apiUrl}/department/${departmentId}`, { params })
      .pipe(map((response) => this.mapEmployeePage(response.data)));
  }

  getDepartments(pageSize = 100): Observable<Department[]> {
    const url = `${environment.apiUrl}api/departments`;
    const getPage = (pageNumber: number) =>
      this.http.get<ApiResponse<PagedResult<Department>>>(url, {
        params: new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize)
      });

    return getPage(1).pipe(
      expand((response) =>
        response.data.hasNextPage ? getPage(response.data.pageNumber + 1) : EMPTY
      ),
      reduce((departments, response) => departments.concat(response.data.items), [] as Department[])
    );
  }

  createEmployee(request: CreateEmployeeRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.apiUrl, request);
  }

  getEmployeeById(employeeId: number): Observable<ApiResponse<EmployeeEditResponse>> {
    return this.http.get<ApiResponse<EmployeeEditResponse>>(`${this.apiUrl}/${employeeId}`);
  }

  getEmployeeDetails(employeeId: number): Observable<ApiResponse<EmployeeDetails>> {
    return this.http.get<ApiResponse<EmployeeDetails>>(`${this.apiUrl}/${employeeId}/details`);
  }

  updateEmployee(employeeId: number, request: CreateEmployeeRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${employeeId}`, request);
  }

  getEmployeeReleases(employeeId: number): Observable<ApiResponse<EmployeeReleaseRecord[]>> {
    return this.http.get<ApiResponse<EmployeeReleaseRecord[]>>(
      `${environment.apiUrl}api/employee-releases/employee/${employeeId}`
    );
  }

  getAllEmployeeReleases(): Observable<ApiResponse<EmployeeReleaseRecord[]>> {
    return this.http.get<ApiResponse<EmployeeReleaseRecord[]>>(`${environment.apiUrl}api/employee-releases`);
  }

  getEmployeeReleasesPaged(query: {
    pageNumber: number;
    pageSize: number;
    search?: string;
    department?: string;
    status?: string;
    releaseType?: string;
  }): Observable<ApiResponse<EmployeeReleasePage>> {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber)
      .set('pageSize', query.pageSize);
    if (query.search) params = params.set('search', query.search);
    if (query.department) params = params.set('department', query.department);
    if (query.status) params = params.set('status', query.status);
    if (query.releaseType) params = params.set('releaseType', query.releaseType);
    return this.http.get<ApiResponse<EmployeeReleasePage>>(`${environment.apiUrl}api/employee-releases/paged`, { params });
  }

  getEmployeeTerminationsPaged(query: {
    pageNumber: number;
    pageSize: number;
    search?: string;
    department?: string;
    status?: string;
  }): Observable<ApiResponse<EmployeeTerminationPage>> {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber)
      .set('pageSize', query.pageSize);
    if (query.search) params = params.set('search', query.search);
    if (query.department) params = params.set('department', query.department);
    if (query.status) params = params.set('status', query.status);
    return this.http.get<ApiResponse<EmployeeTerminationPage>>(`${environment.apiUrl}api/employee-terminations/paged`, { params });
  }

  createEmployeeRelease(employeeId: number, request: EmployeeReleaseRequest): Observable<ApiResponse<EmployeeReleaseRecord>> {
    return this.http.post<ApiResponse<EmployeeReleaseRecord>>(
      `${environment.apiUrl}api/employee-releases/employee/${employeeId}`, request
    );
  }

  updateEmployeeRelease(releasedId: number, request: EmployeeReleaseRequest): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}api/employee-releases/${releasedId}`, request);
  }

  updateEmployeeReleaseStatus(releasedId: number, status: ReleaseStatus): Observable<ApiResponse<EmployeeReleaseRecord>> {
    return this.http.post<ApiResponse<EmployeeReleaseRecord>>(
      `${environment.apiUrl}api/employee-releases/${releasedId}/status`, { status }
    );
  }

  updateEmployeeReleaseClearance(releasedId: number, clearance: ReleaseClearanceUpdate): Observable<ApiResponse<EmployeeReleaseRecord>> {
    return this.http.put<ApiResponse<EmployeeReleaseRecord>>(
      `${environment.apiUrl}api/employee-releases/${releasedId}/clearance`, clearance
    );
  }

  getEmployeeTerminations(employeeId: number): Observable<ApiResponse<EmployeeTerminationRecord[]>> {
    return this.http.get<ApiResponse<EmployeeTerminationRecord[]>>(
      `${environment.apiUrl}api/employee-terminations/employee/${employeeId}`
    );
  }

  getAllEmployeeTerminations(): Observable<ApiResponse<EmployeeTerminationRecord[]>> {
    return this.http.get<ApiResponse<EmployeeTerminationRecord[]>>(`${environment.apiUrl}api/employee-terminations`);
  }

  createEmployeeTermination(employeeId: number, request: EmployeeTerminationRequest): Observable<ApiResponse<EmployeeTerminationRecord>> {
    return this.http.post<ApiResponse<EmployeeTerminationRecord>>(
      `${environment.apiUrl}api/employee-terminations/employee/${employeeId}`, request
    );
  }

  updateEmployeeTermination(terminationId: number, request: EmployeeTerminationRequest): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}api/employee-terminations/${terminationId}`, request);
  }

  updateEmployeeTerminationStatus(terminationId: number, status: TerminationStatus): Observable<ApiResponse<EmployeeTerminationRecord>> {
    return this.http.post<ApiResponse<EmployeeTerminationRecord>>(
      `${environment.apiUrl}api/employee-terminations/${terminationId}/status`, { status }
    );
  }

  updateEmployeeTerminationClearance(terminationId: number, clearance: TerminationClearanceUpdate): Observable<ApiResponse<EmployeeTerminationRecord>> {
    return this.http.put<ApiResponse<EmployeeTerminationRecord>>(
      `${environment.apiUrl}api/employee-terminations/${terminationId}/clearance`, clearance
    );
  }

  getOrganizationTree(): Observable<DepartmentTree[]> {
    if (!this.organizationTree$) {
      this.organizationTree$ = this.http
        .get<ApiResponse<DepartmentTree[]>>(`${this.apiUrl}/tree`)
        .pipe(
          map((response) => response.data),
          shareReplay(1)
        );
    }

    return this.organizationTree$;
  }

  private createAvatar(name: string, background: string): string {
    const initials = name
      .split(' ')
      .slice(0, 2)
      .map((part) => part.charAt(0).toUpperCase())
      .join('');

    const svg = `
      <svg xmlns="http://www.w3.org/2000/svg" width="320" height="250" viewBox="0 0 320 250">
        <rect width="320" height="250" fill="${background}"/>
        <circle cx="160" cy="95" r="48" fill="#ffffff" opacity="0.85"/>
        <circle cx="160" cy="93" r="36" fill="#d1d5db"/>
        <path d="M113 196c18-28 32-42 47-42s29 14 47 42" fill="#ffffff" opacity="0.85"/>
        <text x="160" y="160" text-anchor="middle" font-size="18" font-family="Arial, sans-serif" font-weight="700" fill="#111827">${initials}</text>
      </svg>
    `;

    return `data:image/svg+xml;charset=UTF-8,${encodeURIComponent(svg)}`;
  }

  private mapEmployeePage(page: PagedResult<EmployeeApiRecord>): PagedResult<Employee> {
    return {
      ...page,
      items: page.items.map((employee, index) => {
        const accent = this.palette[index % this.palette.length];
        return {
          id: employee.id,
          name: employee.name,
          designation: employee.designation ?? '',
          designationRank: employee.designationRank,
          department: employee.department ?? '',
          teamName: employee.teamName ?? '',
          imageBase64: employee.imageBase64 ?? this.createAvatar(employee.name, accent),
          accent
        };
      })
    };
  }

getEmployeeMasters(): Observable<any> {
 
return this.http.get<any>(
`${this.apiUrl}/employee-master`
);
}
}
