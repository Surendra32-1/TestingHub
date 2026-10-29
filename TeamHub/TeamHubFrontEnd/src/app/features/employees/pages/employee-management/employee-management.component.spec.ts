import { ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { AuthService } from '../../../../core/auth/auth.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { Department } from '../../models/employee-master.model';
import { EmployeeService } from '../../services/employee.service';
import { EmployeeManagementComponent } from './employee-management.component';

const EMPTY_PAGE = {
  items: [],
  pageNumber: 1,
  pageSize: 10,
  totalCount: 0,
  totalPages: 0,
  hasNextPage: false,
  hasPreviousPage: false
};

const DEPARTMENTS: Department[] = [{ departmentId: 2, departmentName: 'Engineering' } as Department];

describe('EmployeeManagementComponent filters', () => {
  let component: EmployeeManagementComponent;
  let getAllActiveInactiveEmployees: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    getAllActiveInactiveEmployees = vi.fn(() => of(EMPTY_PAGE));
    const employeeService = {
      getAllActiveInactiveEmployees,
      getDepartments: () => of(DEPARTMENTS)
    };
    const authService = { hasPermission: () => true };
    const toastService = { success: vi.fn(), error: vi.fn(), confirm: vi.fn() };
    const changeDetectorRef = { markForCheck: vi.fn(), detectChanges: vi.fn() };
    const router = { navigate: vi.fn() };

    component = new EmployeeManagementComponent(
      employeeService as unknown as EmployeeService,
      authService as unknown as AuthService,
      toastService as unknown as ToastService,
      changeDetectorRef as unknown as ChangeDetectorRef,
      router as unknown as Router
    );
    component.departments = DEPARTMENTS;
  });

  it('sends the selected status to the API instead of always asking for active employees', () => {
    component.onStatusChange('inactive');

    expect(getAllActiveInactiveEmployees).toHaveBeenLastCalledWith(1, 10, '', 'all', null, 'inactive');
    expect(component.resultsLabel).toBe('Inactive Employees');
  });

  it('sends the selected department and resets to the first page', () => {
    component.pageNumber = 3;

    component.onDepartmentChange(2);

    expect(getAllActiveInactiveEmployees).toHaveBeenLastCalledWith(1, 10, '', 'all', 'Engineering', 'all');
  });

  it('combines search, department and status into one request', () => {
    component.searchTerm = 'linda';
    component.onDepartmentChange(2);
    component.onStatusChange('inactive');

    expect(getAllActiveInactiveEmployees).toHaveBeenLastCalledWith(1, 10, 'linda', 'all', 'Engineering', 'inactive');
  });

  it('debounces typing into a single request carrying the search term', () => {
    vi.useFakeTimers();
    try {
      component.onSearchInput('li');
      component.onSearchInput('linda');

      expect(getAllActiveInactiveEmployees).not.toHaveBeenCalled();

      vi.advanceTimersByTime(350);

      expect(getAllActiveInactiveEmployees).toHaveBeenCalledTimes(1);
      expect(getAllActiveInactiveEmployees).toHaveBeenLastCalledWith(1, 10, 'linda', 'all', null, 'all');
    } finally {
      vi.useRealTimers();
    }
  });
});
