import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../../../core/auth/auth.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { getApiErrorMessage } from '../../../../shared/services/api-error-message';
import { DirectoryViewComponent } from '../../components/directory-view/directory-view.component';
import { EmployeeDetailsComponent } from '../../components/employee-details/employee-details.component';
import { EmployeeHierarchyComponent } from '../../components/employee-hierarchy/employee-hierarchy.component';
import { Department } from '../../models/employee-master.model';
import { Employee } from '../../models/employee.model';
import { EmployeeService } from '../../services/employee.service';

@Component({
  selector: 'app-employee-management',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    DirectoryViewComponent,
    EmployeeDetailsComponent,
    EmployeeHierarchyComponent
  ],
  templateUrl: './employee-management.component.html',
  styleUrl: './employee-management.component.css'
})
export class EmployeeManagementComponent implements OnInit, OnDestroy {
  employees: Employee[] = [];
  departments: Department[] = [];
  isLoading = true;
  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;
  searchTerm = '';
  selectedDepartmentId: number | null = null;
  selectedStatus: 'all' | 'active' | 'inactive' = 'all';
  appliedSearchTerm = '';
  appliedDepartmentName: string | null = null;
  appliedStatus: 'all' | 'active' | 'inactive' = 'all';
  readonly canAddEmployee: boolean;
  readonly canEditEmployee: boolean;
  readonly canDeleteEmployee: boolean;
  readonly canViewEmployeeCard: boolean;
  readonly canReleaseEmployee: boolean;
  readonly canTerminateEmployee: boolean;
  readonly canViewHierarchy: boolean;
  activeView: 'directory' | 'hierarchy' | 'details' = 'directory';
  hierarchyEmployeeId: number | null = null;
  hierarchyDirect = false;
  selectedEmployeeId: number | null = null;
  errorMessage = '';
  private loadRequestId = 0;
  private searchTimeout?: ReturnType<typeof setTimeout>;

  get resultsLabel(): string {
    if (this.appliedDepartmentName) {
      return `${this.appliedDepartmentName} Employees`;
    }
    if (this.appliedStatus === 'active') {
      return 'Active Employees';
    }
    if (this.appliedStatus === 'inactive') {
      return 'Inactive Employees';
    }
    return 'All Employees';
  }

  constructor(
    private readonly employeeService: EmployeeService,
    private readonly authService: AuthService,
    private readonly toastService: ToastService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly router: Router
  ) {
    this.canAddEmployee = authService.hasPermission('administration.create');
    this.canEditEmployee = authService.hasPermission('administration.update');
    this.canDeleteEmployee = authService.hasPermission('administration.delete');
    this.canViewEmployeeCard = authService.hasPermission('administration.cardview');
    this.canReleaseEmployee = authService.hasPermission('administration.release');
    this.canTerminateEmployee = authService.hasPermission('administration.terminate')
      || authService.hasPermission('terminations.add');
    this.canViewHierarchy = authService.hasPermission('administration.hierarchy.view');
  }

  ngOnInit(): void {
    this.loadDepartments();
    this.loadEmployees();
  }

  ngOnDestroy(): void {
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }
  }

  onSearchInput(term: string): void {
    this.searchTerm = term;
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }
    this.searchTimeout = setTimeout(() => this.applyFilters(), 350);
  }

  onDepartmentChange(departmentId: number | null): void {
    this.selectedDepartmentId = departmentId;
    this.applyFilters();
  }

  onStatusChange(status: 'all' | 'active' | 'inactive'): void {
    this.selectedStatus = status;
    this.applyFilters();
  }

  applyFilters(): void {
    this.appliedSearchTerm = this.searchTerm.trim();
    this.appliedDepartmentName = this.departments.find(
      (department) => department.departmentId === this.selectedDepartmentId
    )?.departmentName ?? null;
    this.appliedStatus = this.selectedStatus;
    this.loadEmployees(1);
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedDepartmentId = null;
    this.selectedStatus = 'all';
    this.appliedSearchTerm = '';
    this.appliedDepartmentName = null;
    this.appliedStatus = 'all';
    this.loadEmployees(1);
  }

  changePage(pageNumber: number): void {
    this.loadEmployees(pageNumber);
  }

  changePageSize(pageSize: number): void {
    this.pageSize = pageSize;
    this.loadEmployees(1);
  }

  editEmployee(employeeId: number): void {
    this.router.navigate(['/employees/edit', employeeId]);
  }

  releaseEmployee(employeeId: number): void {
    this.router.navigate(['/employees/release', employeeId]);
  }

  terminateEmployee(employeeId: number): void {
    this.router.navigate(['/employees/terminate', employeeId]);
  }

  deleteEmployee(employeeId: number): void {
    this.employeeService.deleteEmployee(employeeId).subscribe({
      next: () => {
        this.toastService.success('Employee deleted successfully.');
        const lastItemOnPage = this.totalCount - 1 <= (this.pageNumber - 1) * this.pageSize;
        this.loadEmployees(lastItemOnPage && this.pageNumber > 1 ? this.pageNumber - 1 : this.pageNumber);
      },
      error: (error) => {
        this.toastService.error(getApiErrorMessage(error, 'Could not delete employee.'));
      }
    });
  }

  showEmployeeHierarchy(employeeId: number, direct = false): void {
    this.hierarchyEmployeeId = employeeId;
    this.hierarchyDirect = direct;
    this.activeView = 'hierarchy';
  }

  showEmployeeDetails(employeeId: number): void {
    this.selectedEmployeeId = employeeId;
    this.activeView = 'details';
  }

  returnToDirectory(): void {
    this.activeView = 'directory';
  }

  private loadDepartments(): void {
    this.employeeService.getDepartments().subscribe({
      next: (departments) => {
        this.departments = departments;
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        this.toastService.error(getApiErrorMessage(error, 'Could not load departments.'));
      }
    });
  }

  private loadEmployees(pageNumber = this.pageNumber): void {
    const requestId = ++this.loadRequestId;
    this.isLoading = true;
    this.errorMessage = '';
    this.pageNumber = pageNumber;
    this.employeeService.getAllActiveInactiveEmployees(
      this.pageNumber,
      this.pageSize,
      this.appliedSearchTerm,
      'all',
      this.appliedDepartmentName,
      this.appliedStatus
    ).subscribe({
      next: (page) => {
        if (requestId !== this.loadRequestId) return;
        this.employees = page.items;
        this.totalCount = page.totalCount;
        this.isLoading = false;
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        if (requestId !== this.loadRequestId) return;
        this.isLoading = false;
        this.errorMessage = getApiErrorMessage(error, 'Could not load employees.');
        this.changeDetectorRef.markForCheck();
      }
    });
  }
}
