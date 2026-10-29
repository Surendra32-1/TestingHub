import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { TopbarComponent } from '../../../../shared/components/topbar/topbar.component';
import { getApiErrorMessage } from '../../../../shared/services/api-error-message';
import { ToastService } from '../../../../shared/services/toast.service';
import { Department } from '../../models/employee-master.model';
import { EmployeeCard } from '../../models/employee-card.model';
import { EmployeeSearchService } from '../../services/employee-search.service';
import { EmployeeService } from '../../services/employee.service';

@Component({
  selector: 'app-employee-search',
  standalone: true,
  imports: [CommonModule, FormsModule, TopbarComponent, PaginationComponent],
  templateUrl: './employee-search.component.html',
  styleUrl: './employee-search.component.css'
})
export class EmployeeSearchComponent implements OnInit {
  employees: EmployeeCard[] = [];
  departments: Department[] = [];
  isLoading = true;
  errorMessage = '';
  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;

  nameFilter = '';
  designationFilter = '';
  departmentId: number | null = null;
  contactFilter = '';

  private searchTimeout: ReturnType<typeof setTimeout> | undefined;
  private loadRequestId = 0;

  constructor(
    private readonly employeeSearchService: EmployeeSearchService,
    private readonly employeeService: EmployeeService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.loadDepartments();
    this.searchEmployees();
  }

  get hasFilters(): boolean {
    return Boolean(this.nameFilter.trim() || this.designationFilter.trim() || this.departmentId || this.contactFilter.trim());
  }

  get resultsLabel(): string {
    return this.totalCount === 1 ? '1 Active Employee' : `${this.totalCount} Active Employees`;
  }

  searchEmployees(pageNumber = this.pageNumber, pageSize = this.pageSize): void {
    const requestId = ++this.loadRequestId;
    this.isLoading = true;
    this.errorMessage = '';
    this.pageNumber = pageNumber;
    this.pageSize = pageSize;

    this.employeeSearchService.searchEmployees(
      {
        name: this.nameFilter.trim(),
        designation: this.designationFilter.trim(),
        departmentId: this.departmentId,
        contact: this.contactFilter.trim()
      },
      pageNumber,
      pageSize
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
        this.employees = [];
        this.totalCount = 0;
        this.isLoading = false;
        this.errorMessage = getApiErrorMessage(error, 'Could not load employees.');
        this.toastService.error(this.errorMessage);
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  onSearchInput(): void {
    this.scheduleSearch();
  }

  onDepartmentChange(departmentId: number | null): void {
    this.departmentId = departmentId;
    this.cancelPendingSearch();
    this.searchEmployees(1, this.pageSize);
  }

  clearFilters(): void {
    if (!this.hasFilters) return;
    this.nameFilter = '';
    this.designationFilter = '';
    this.departmentId = null;
    this.contactFilter = '';
    this.cancelPendingSearch();
    this.searchEmployees(1, this.pageSize);
  }

  changePage(pageNumber: number): void {
    this.searchEmployees(pageNumber, this.pageSize);
  }

  changePageSize(pageSize: number): void {
    this.searchEmployees(1, pageSize);
  }

  trackByEmployeeId(_: number, employee: EmployeeCard): number {
    return employee.id;
  }

  initials(name: string): string {
    const parts = name.trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) return '?';
    return (parts[0][0] + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase();
  }

  private scheduleSearch(): void {
    this.cancelPendingSearch();
    this.searchTimeout = setTimeout(() => this.searchEmployees(1, this.pageSize), 300);
  }

  private cancelPendingSearch(): void {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = undefined;
  }

  private loadDepartments(): void {
    this.employeeService.getDepartments().subscribe({
      next: (departments) => {
        this.departments = departments;
        this.changeDetectorRef.markForCheck();
      },
      error: () => undefined
    });
  }
}
