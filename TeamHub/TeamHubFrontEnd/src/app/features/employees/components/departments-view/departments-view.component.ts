import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, EventEmitter, Input, OnDestroy, Output, OnInit } from '@angular/core';
import { map } from 'rxjs';

import { DepartmentHierarchyComponent } from '../department-hierarchy/department-hierarchy.component';
import { DirectoryControlsComponent, EmployeeSearchField } from '../directory-controls/directory-controls.component';
import { ResultsHeaderComponent } from '../results-header/results-header.component';
import { Department } from '../../models/employee-master.model';
import { Employee } from '../../models/employee.model';
import { EmployeeService, PublicEmployeeDetails } from '../../services/employee.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { getApiErrorMessage } from '../../../../shared/services/api-error-message';

@Component({
  selector: 'app-departments-view',
  standalone: true,
  imports: [
    CommonModule,
    DepartmentHierarchyComponent,
    DirectoryControlsComponent,
    ResultsHeaderComponent
  ],
  templateUrl: './departments-view.component.html',
  styleUrl: './departments-view.component.css'
})
export class DepartmentsViewComponent implements OnInit, OnDestroy {
  departments: Department[] = [];
  departmentLeaders: Record<number, Employee> = {};
  departmentEmployeeCounts: Record<number, number> = {};
  departmentHead: Employee | null = null;
  departmentHeadDetails: PublicEmployeeDetails | null = null;
  departmentMembers: Employee[] = [];
  isLoadingHierarchy = false;
  hierarchyLookupComplete = false;
  employees: Employee[] = [];
  selectedDepartment: Department | null = null;
  showEmployeeGrid = false;
  isLoadingDepartments = true;
  isLoadingEmployees = true;
  totalCount = 0;
  searchTerm = '';
  searchField: EmployeeSearchField = 'all';
  @Input() initialDepartmentName: string | null = null;
  @Input() readOnlyHome = false;
  @Input() canEditEmployee = false;
  @Input() canViewEmployeeDetails = true;
  @Input() canReleaseEmployee = false;
  @Input() canTerminateEmployee = false;
  @Output() editEmployee = new EventEmitter<number>();
  @Output() releaseEmployee = new EventEmitter<number>();
  @Output() terminateEmployee = new EventEmitter<number>();
  @Output() viewEmployeeHierarchy = new EventEmitter<number>();
  @Output() viewEmployeeDirectHierarchy = new EventEmitter<number>();
  @Output() viewEmployeeDetails = new EventEmitter<number>();
  private loadRequestId = 0;
  private hierarchyRequestId = 0;
  private searchTimeout: ReturnType<typeof setTimeout> | undefined;

  constructor(
    private readonly employeeService: EmployeeService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.employeeService.getDepartments().subscribe({
      next: (departments) => {
        this.departments = departments;
        this.isLoadingDepartments = false;
        const initialDepartment = departments.find(
          (department) => department.departmentName === this.initialDepartmentName
        );
        if (initialDepartment) {
          this.selectDepartment(initialDepartment);
        }
        for (const department of departments) {
          const employeePage = this.readOnlyHome
            ? this.employeeService.getPublicActiveEmployeesByDepartmentId(department.departmentId, 1, 1)
            : this.employeeService.getActiveEmployeesByDepartmentId(department.departmentId, 1, 1);
          employeePage.subscribe({
            next: (page) => {
              this.departmentEmployeeCounts = {
                ...this.departmentEmployeeCounts,
                [department.departmentId]: page.totalCount
              };
              const leader = page.items[0];
              if (leader) {
                this.departmentLeaders = { ...this.departmentLeaders, [department.departmentId]: leader };
              }
              this.changeDetectorRef.markForCheck();
            },
            error: (error) => {
              this.toastService.error(getApiErrorMessage(error, `Could not load active employees for ${department.departmentName}.`));
            }
          });
        }
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        this.isLoadingDepartments = false;
        this.toastService.error(getApiErrorMessage(error, 'Could not load departments.'));
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  ngOnDestroy(): void {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.loadRequestId++;
    this.hierarchyRequestId++;
  }

  selectDepartment(department: Department): void {
    this.selectedDepartment = department;
    this.showEmployeeGrid = true;
    this.loadDepartmentHierarchy(department);
    this.loadEmployees();
  }

  selectDepartmentFilter(departmentId: number | null): void {
    this.selectedDepartment = this.departments.find(
      (department) => department.departmentId === departmentId
    ) ?? null;
    this.showEmployeeGrid = true;
    if (this.selectedDepartment) {
      this.loadDepartmentHierarchy(this.selectedDepartment);
    } else {
      this.clearDepartmentHierarchy();
    }
    this.loadEmployees();
  }

  updateSearchTerm(searchTerm: string): void {
    this.searchTerm = searchTerm;
    this.scheduleSearch();
  }

  updateSearchField(searchField: EmployeeSearchField): void {
    this.searchField = searchField;
    this.scheduleSearch();
  }

  private scheduleSearch(): void {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => this.loadEmployees(), 300);
  }

  private loadEmployees(): void {
    const departmentId = this.selectedDepartment?.departmentId;

    const requestId = ++this.loadRequestId;
    this.isLoadingEmployees = true;
    this.departmentMembers = [];
    const employeeList = departmentId === undefined
      ? this.readOnlyHome
        ? this.employeeService.getAllPublicEmployees(this.searchTerm, this.searchField)
        : this.employeeService.getAllEmployees(this.searchTerm, this.searchField)
      : this.readOnlyHome
        ? this.employeeService.getAllPublicActiveEmployeesByDepartmentId(
          departmentId,
          this.searchTerm,
          this.searchField
        )
        : this.employeeService.getAllActiveEmployeesByDepartmentId(
          departmentId,
          this.searchTerm,
          this.searchField
        );
    employeeList.subscribe({
      next: (employees) => {
        if (requestId !== this.loadRequestId) return;
        this.employees = employees;
        this.totalCount = employees.length;
        this.isLoadingEmployees = false;
        this.rebuildMembers();
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        if (requestId !== this.loadRequestId) return;
        this.isLoadingEmployees = false;
        this.toastService.error(getApiErrorMessage(error, 'Could not load department employees.'));
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  release(employeeId: number): void {
    const employee = this.employees.find((item) => item.id === employeeId) ??
      (this.departmentHead?.id === employeeId ? this.departmentHead : undefined);
    this.toastService.confirm(
      `Create an offboarding release record for ${employee?.name ?? 'this employee'}? Choose a release type on the form that follows.`,
      () => this.releaseEmployee.emit(employeeId),
      'Release'
    );
  }

  terminate(employeeId: number): void {
    const employee = this.employees.find((item) => item.id === employeeId) ??
      (this.departmentHead?.id === employeeId ? this.departmentHead : undefined);
    this.toastService.confirm(
      `Create a termination record for ${employee?.name ?? 'this employee'}?`,
      () => this.terminateEmployee.emit(employeeId),
      'Continue'
    );
  }

  private loadDepartmentHierarchy(department: Department): void {
    const requestId = ++this.hierarchyRequestId;
    this.departmentHead = null;
    this.departmentMembers = [];
    this.isLoadingHierarchy = true;
    this.hierarchyLookupComplete = false;

    const employees = this.readOnlyHome
      ? this.employeeService.getAllPublicActiveEmployeesByDepartmentId(department.departmentId)
      : this.employeeService.getAllActiveEmployeesByDepartmentId(department.departmentId);
    employees.subscribe({
      next: (employees) => {
        if (requestId !== this.hierarchyRequestId) return;
        this.departmentHead = this.findDepartmentHead(employees);
        this.loadDepartmentHeadDetails(this.departmentHead, requestId);
        this.isLoadingHierarchy = false;
        this.hierarchyLookupComplete = true;
        this.rebuildMembers();
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        if (requestId !== this.hierarchyRequestId) return;
        this.isLoadingHierarchy = false;
        this.hierarchyLookupComplete = false;
        this.toastService.error(getApiErrorMessage(error, 'Could not load the department reporting hierarchy.'));
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  private findDepartmentHead(employees: Employee[]): Employee | null {
    const rankedEmployees = employees.filter((employee) => Number.isFinite(employee.designationRank));
    if (rankedEmployees.length === 0) return null;

    const highestRank = Math.min(...rankedEmployees.map((employee) => employee.designationRank));
    return rankedEmployees
      .filter((employee) => employee.designationRank === highestRank)
      .sort((left, right) => left.name.localeCompare(right.name, undefined, { sensitivity: 'base' }))[0];
  }

  private loadDepartmentHeadDetails(head: Employee | null, requestId: number): void {
    this.departmentHeadDetails = null;
    if (!head) return;

    this.employeeService.getPublicEmployeeDetails(head.id).pipe(map((response) => response.data)).subscribe({
      next: (details) => {
        if (requestId !== this.hierarchyRequestId) return;
        this.departmentHeadDetails = details;
        this.changeDetectorRef.markForCheck();
      },
      error: () => undefined
    });
  }

  private clearDepartmentHierarchy(): void {
    this.hierarchyRequestId++;
    this.departmentHead = null;
    this.departmentHeadDetails = null;
    this.departmentMembers = [];
    this.isLoadingHierarchy = false;
    this.hierarchyLookupComplete = false;
  }

  private rebuildMembers(): void {
    const rankOf = (employee: Employee) =>
      Number.isFinite(employee.designationRank) ? employee.designationRank : Number.MAX_SAFE_INTEGER;

    this.departmentMembers = this.employees
      .filter((employee) => employee.id !== this.departmentHead?.id)
      .sort((left, right) => {
        const byRank = rankOf(left) - rankOf(right);
        return byRank !== 0
          ? byRank
          : left.name.localeCompare(right.name, undefined, { sensitivity: 'base' });
      });
  }
}
