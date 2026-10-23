import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, EventEmitter, Input, Output, OnInit } from '@angular/core';

import { Department } from '../../models/employee-master.model';
import { Employee } from '../../models/employee.model';
import { EmployeeService } from '../../services/employee.service';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { ToastService } from '../../../../shared/services/toast.service';
import { getApiErrorMessage } from '../../../../shared/services/api-error-message';

@Component({
  selector: 'app-departments-view',
  standalone: true,
  imports: [CommonModule, PaginationComponent],
  templateUrl: './departments-view.component.html',
  styleUrl: './departments-view.component.css'
})
export class DepartmentsViewComponent implements OnInit {
  departments: Department[] = [];
  departmentLeaders: Record<number, Employee> = {};
  employees: Employee[] = [];
  selectedDepartment: Department | null = null;
  isLoadingDepartments = true;
  isLoadingEmployees = false;
  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;
  @Output() departmentSelectedChange = new EventEmitter<boolean>();
  private loadRequestId = 0;

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
        for (const department of departments) {
          this.employeeService.getActiveEmployeesByDepartmentId(department.departmentId, 1, 1).subscribe({
            next: (page) => {
              const leader = page.items[0];
              if (leader) {
                this.departmentLeaders = { ...this.departmentLeaders, [department.departmentId]: leader };
                this.changeDetectorRef.markForCheck();
              }
            },
            error: (error) => {
              this.toastService.error(getApiErrorMessage(error, `Could not load the highest-ranked employee for ${department.departmentName}.`));
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

  selectDepartment(department: Department): void {
    this.selectedDepartment = department;
    this.departmentSelectedChange.emit(true);
    this.loadEmployees(1, this.pageSize);
  }

  showDepartments(): void {
    this.selectedDepartment = null;
    this.employees = [];
    this.totalCount = 0;
    this.loadRequestId++;
    this.departmentSelectedChange.emit(false);
  }

  changePage(pageNumber: number): void {
    this.loadEmployees(pageNumber, this.pageSize);
  }

  changePageSize(pageSize: number): void {
    this.loadEmployees(1, pageSize);
  }

  private loadEmployees(pageNumber: number, pageSize: number): void {
    const departmentId = this.selectedDepartment?.departmentId;
    if (departmentId === undefined) return;

    const requestId = ++this.loadRequestId;
    this.isLoadingEmployees = true;
    this.pageNumber = pageNumber;
    this.pageSize = pageSize;
    this.employeeService.getActiveEmployeesByDepartmentId(departmentId, pageNumber, pageSize).subscribe({
      next: (page) => {
        if (requestId !== this.loadRequestId) return;
        this.employees = page.items;
        this.totalCount = page.totalCount;
        this.isLoadingEmployees = false;
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
}
