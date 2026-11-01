import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { DepartmentsViewComponent } from '../../components/departments-view/departments-view.component';
import { DirectoryViewComponent } from '../../components/directory-view/directory-view.component';
import { EmployeeTabComponent } from '../../components/employee-tab/employee-tab.component';
import { EmployeeHierarchyComponent } from '../../components/employee-hierarchy/employee-hierarchy.component';
import { EmployeeDetailsComponent } from '../../components/employee-details/employee-details.component';
import { Employee } from '../../models/employee.model';
import { EmployeeService } from '../../services/employee.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { getApiErrorMessage } from '../../../../shared/services/api-error-message';
import { AuthService } from '../../../../core/auth/auth.service';

type EmployeeView = 'directory' | 'departments' | 'hierarchy' | 'details';

@Component({
  selector: 'app-employee-directory',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    EmployeeTabComponent,
    DirectoryViewComponent,
    DepartmentsViewComponent,
    EmployeeHierarchyComponent,
    EmployeeDetailsComponent
  ],
  templateUrl: './employee-directory.component.html',
  styleUrl: './employee-directory.component.css'
})
export class EmployeeDirectoryComponent implements OnInit {
  employees: Employee[] = [];
  isLoading = true;
  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;
  searchTerm = '';
  searchField: 'all' | 'name' | 'designation' | 'department' = 'all';
  selectedDepartmentName: string | null = null;
  hierarchyEmployeeId: number | null = null;
  hierarchyDirect = false;
  selectedEmployeeId: number | null = null;
  readonly tableLayout: boolean;
  readonly readOnlyHome: boolean;
  readonly canViewAdministrationHierarchy: boolean;
  readonly canViewEmployeeCard: boolean;
  readonly canAddEmployee: boolean;
  readonly canEditEmployee: boolean;
  readonly canReleaseEmployee: boolean;
  readonly canTerminateEmployee: boolean;
  private searchTimeout: ReturnType<typeof setTimeout> | undefined;
  private loadRequestId = 0;
  activeView: EmployeeView = 'directory';
  readonly tabs: { key: EmployeeView; label: string }[] = [
    { key: 'directory', label: 'Directory' },
    { key: 'departments', label: 'Departments' }
  ];

  constructor(
    private readonly employeeService: EmployeeService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly toastService: ToastService,
    private readonly router: Router,
    authService: AuthService,
    route: ActivatedRoute
  ) {
    this.tableLayout = route.snapshot.data['layout'] === 'table';
    this.readOnlyHome = route.snapshot.data['readOnlyHome'] === true;
    this.canViewAdministrationHierarchy = authService.hasPermission('administration.hierarchy.view');
    this.canViewEmployeeCard = authService.hasPermission('administration.cardview');
    this.canAddEmployee = authService.hasPermission('administration.create');
    this.canEditEmployee = authService.hasPermission('administration.update');
    this.canReleaseEmployee = authService.hasPermission('administration.release');
    this.canTerminateEmployee = authService.hasPermission('administration.terminate')
      || authService.hasPermission('terminations.add');
  }

  ngOnInit(): void {
    this.loadEmployees();
  }

  loadEmployees(pageNumber = this.pageNumber, pageSize = this.pageSize): void {
    const requestId = ++this.loadRequestId;
    this.isLoading = true;
    this.pageNumber = pageNumber;
    this.pageSize = pageSize;
    const employeePage = this.readOnlyHome
      ? this.employeeService.getPublicEmployees(
        pageNumber,
        pageSize,
        this.searchTerm,
        this.searchField,
        this.selectedDepartmentName
      )
      : this.employeeService.getEmployees(
        pageNumber,
        pageSize,
        this.searchTerm,
        this.searchField,
        this.selectedDepartmentName
      );
    employeePage.subscribe({
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
        this.toastService.error(getApiErrorMessage(error, 'Could not load employees.'));
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  updateSearchTerm(searchTerm: string): void {
    this.searchTerm = searchTerm;
    this.scheduleSearch();
  }

  updateSearchField(searchField: 'all' | 'name' | 'designation' | 'department'): void {
    this.searchField = searchField;
    this.scheduleSearch();
  }

  filterByDepartment(departmentName: string): void {
    if (!departmentName) return;
    this.selectedDepartmentName = departmentName;
    this.activeView = 'departments';
  }

  clearDepartmentFilter(): void {
    if (!this.selectedDepartmentName) return;
    this.selectedDepartmentName = null;
    this.loadEmployees(1, this.pageSize);
  }

  private scheduleSearch(): void {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => this.loadEmployees(1, this.pageSize), 300);
  }

  selectView(view: EmployeeView): void {
    if (view === 'directory' && this.activeView !== 'directory') {
      this.clearDepartmentFilter();
    }
    this.activeView = view;
  }

  showEmployeeHierarchy(employeeId: number): void {
    this.hierarchyEmployeeId = employeeId;
    this.hierarchyDirect = false;
    this.activeView = 'hierarchy';
  }

  showDirectHierarchy(employeeId: number): void {
    this.hierarchyEmployeeId = employeeId;
    this.hierarchyDirect = true;
    this.activeView = 'hierarchy';
  }

  showEmployeeDetails(employeeId: number): void {
    this.selectedEmployeeId = employeeId;
    this.activeView = 'details';
  }

  returnToDirectory(): void {
    this.clearDepartmentFilter();
    this.activeView = 'directory';
  }

  changePage(pageNumber: number): void {
    this.loadEmployees(pageNumber, this.pageSize);
  }

  changePageSize(pageSize: number): void {
    this.loadEmployees(1, pageSize);
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
}
