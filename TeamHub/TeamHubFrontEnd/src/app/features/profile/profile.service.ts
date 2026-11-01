import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { catchError, map, Observable, of, shareReplay, tap, throwError } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { environment } from '../../../environments/environment';

export interface EmployeeProfileBloodGroup {
  bloodGroupId: number;
  bloodGroupName: string;
}

export interface EmployeeProfileAddress {
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
}

export interface EmployeeProfileSkill {
  employeeSkillId: number;
  skillId: number;
  skillName: string;
  skillLevel: number;
  isPrimary: boolean;
}

export interface EmployeeProfileDressSize {
  dressId: number;
  dressName: string;
  size: string;
  isActive: boolean;
}

export interface EmployeeProfileSkillOption {
  skillId: number;
  skillName: string;
  isActive: boolean;
}

export interface EmployeeProfileDressSizeOption {
  dressId: number;
  dressName: string;
  size: string;
  isActive: boolean;
}

export interface EmployeeProfileRole {
  roleId: number;
  roleName: string;
}

export interface EmployeeProfileEmployee {
  employeeId: number;
  eNumber: number;
  firstName: string;
  middleName: string | null;
  lastName: string;
  joinedDate: string;
  joinedDateBS: string;
  email: string;
  alternateEmail: string | null;
  designationId: number;
  designationName: string | null;
  departmentId: number;
  departmentName: string | null;
  jobTypeId: number;
  jobTypeName: string | null;
  bloodGroupId: number;
  bloodGroupName: string | null;
  genderId: number;
  genderName: string | null;
  reportsToEmployeeId: number | null;
  reportsToEmployeeName: string | null;
  leadEmployeeId: number | null;
  leadEmployeeName: string | null;
  stationNumber: string | null;
  recruitmentId: number | null;
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
  roleIds: number[];
  roleName: string | null;
  teamId: number;
  teamName: string | null;
  businessUnitId: number | null;
  businessUnitName: string | null;
  cotivityEmployeeId: number | null;
  cotivityEmployeeName: string | null;
  isActive: boolean;
  imageBase64: string | null;
  addresses: EmployeeProfileAddress[];
  skills: EmployeeProfileSkill[];
  dressSizes: EmployeeProfileDressSize[];
  educations: EmployeeProfileEducation[];
  trainings: EmployeeProfileTraining[];
}

export interface EmployeeProfileEducation {
  educationId: number;
  qualification: string;
  boardUniversity: string | null;
  institution: string | null;
  faculty: string | null;
  passedYear: number | null;
  joinedDate: string | null;
  passoutDate: string | null;
  joinedDateBS: string | null;
  passoutDateBS: string | null;
  divisionGrade: string | null;
  remarks: string | null;
}

export interface EmployeeProfileTraining {
  trainingId: number;
  trainingTitle: string;
  trainingProvider: string | null;
  trainingType: string | null;
  trainingLocation: string | null;
  joinedDate: string | null;
  passoutDate: string | null;
  joinedDateBS: string | null;
  passoutDateBS: string | null;
  duration: string | null;
  certificateNo: string | null;
  remarks: string | null;
}

export interface EmployeeProfile {
  username: string | null;
  employee: EmployeeProfileEmployee;
  bloodGroups: EmployeeProfileBloodGroup[];
  roles: EmployeeProfileRole[];
  skillOptions: EmployeeProfileSkillOption[];
  dressSizeOptions: EmployeeProfileDressSizeOption[];
}

export interface EmployeeProfileSummary {
  employeeName: string;
  imageBase64: string | null;
}

export interface EmployeeProfilePatch {
  joinedDate?: string;
  joinedDateBS?: string;
  dateOfBirth?: string | null;
  dateOfBirthBS?: string | null;
  alternateEmail?: string | null;
  bloodGroupId?: number;
  mobileNumber?: string | null;
  emergencyContactNumber?: string | null;
  motherName?: string | null;
  spouseName?: string | null;
  latitude?: number;
  longitude?: number;
  imageBase64?: string;
  skills?: Array<{ skillId: number; skillLevel: number }>;
  dressIds?: number[];
}

interface ApiResponse<T> {
  data: T;
  success: boolean;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly profileUrl = `${environment.apiUrl}api/profile/me`;
  private myProfile$?: Observable<EmployeeProfile>;
  private myProfileToken: string | null = null;
  private mySummary$?: Observable<EmployeeProfileSummary>;
  private mySummaryToken: string | null = null;

  getMySummary(): Observable<EmployeeProfileSummary> {
    const token = this.authService.accessToken;
    if (!this.mySummary$ || this.mySummaryToken !== token) {
      this.mySummaryToken = token;
      this.mySummary$ = this.http.get<ApiResponse<EmployeeProfileSummary>>(`${this.profileUrl}/summary`).pipe(
        map((response) => response.data),
        shareReplay({ bufferSize: 1, refCount: false }),
        catchError((error: unknown) => {
          this.mySummary$ = undefined;
          this.mySummaryToken = null;
          return throwError(() => error);
        })
      );
    }

    return this.mySummary$;
  }

  getMyProfile(): Observable<EmployeeProfile> {
    const token = this.authService.accessToken;
    if (!this.myProfile$ || this.myProfileToken !== token) {
      this.myProfileToken = token;
      this.myProfile$ = this.http.get<ApiResponse<EmployeeProfile>>(this.profileUrl).pipe(
        map((response) => response.data),
        shareReplay({ bufferSize: 1, refCount: false }),
        catchError((error: unknown) => {
          this.myProfile$ = undefined;
          this.myProfileToken = null;
          return throwError(() => error);
        })
      );
    }

    return this.myProfile$;
  }

  updateMyProfile(patch: EmployeeProfilePatch): Observable<EmployeeProfile> {
    return this.http.patch<ApiResponse<EmployeeProfile>>(this.profileUrl, patch).pipe(
      map((response) => response.data),
      tap((profile) => {
        this.myProfileToken = this.authService.accessToken;
        this.myProfile$ = of(profile);
        this.mySummaryToken = this.authService.accessToken;
        this.mySummary$ = of({
          employeeName: [profile.employee.firstName, profile.employee.middleName, profile.employee.lastName]
            .filter((name): name is string => !!name?.trim())
            .join(' '),
          imageBase64: profile.employee.imageBase64
        });
      })
    );
  }
}
