import { ChangeDetectorRef } from '@angular/core';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { ToastService } from '../../../../shared/services/toast.service';
import { Department } from '../../models/employee-master.model';
import { EmployeeSearchService } from '../../services/employee-search.service';
import { EmployeeService } from '../../services/employee.service';
import { EmployeeSearchComponent } from './employee-search.component';

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

describe('EmployeeSearchComponent', () => {
  let component: EmployeeSearchComponent;
  let searchEmployees: ReturnType<typeof vi.fn>;
  let toastError: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    searchEmployees = vi.fn(() => of(EMPTY_PAGE));
    toastError = vi.fn();
    const employeeSearchService = { searchEmployees };
    const employeeService = { getDepartments: () => of(DEPARTMENTS) };
    const toastService = { success: vi.fn(), error: toastError, confirm: vi.fn() };
    const changeDetectorRef = { markForCheck: vi.fn(), detectChanges: vi.fn() };

    component = new EmployeeSearchComponent(
      employeeSearchService as unknown as EmployeeSearchService,
      employeeService as unknown as EmployeeService,
      changeDetectorRef as unknown as ChangeDetectorRef,
      toastService as unknown as ToastService
    );
  });

  it('asks the API for the first page of active employees with no filters', () => {
    component.searchEmployees();

    expect(searchEmployees).toHaveBeenLastCalledWith(
      { name: '', designation: '', departmentId: null, contact: '' },
      1,
      10
    );
  });

  it('sends every filled search field in the same request', () => {
    component.nameFilter = '  sabina ';
    component.designationFilter = 'engineer';
    component.departmentId = 2;
    component.contactFilter = '9868';

    component.searchEmployees();

    expect(searchEmployees).toHaveBeenLastCalledWith(
      { name: 'sabina', designation: 'engineer', departmentId: 2, contact: '9868' },
      1,
      10
    );
  });

  it('debounces typing into a single request', () => {
    vi.useFakeTimers();
    try {
      component.nameFilter = 'sa';
      component.onSearchInput();
      component.nameFilter = 'sabina';
      component.onSearchInput();

      expect(searchEmployees).not.toHaveBeenCalled();

      vi.advanceTimersByTime(300);

      expect(searchEmployees).toHaveBeenCalledTimes(1);
      expect(searchEmployees).toHaveBeenLastCalledWith(
        { name: 'sabina', designation: '', departmentId: null, contact: '' },
        1,
        10
      );
    } finally {
      vi.useRealTimers();
    }
  });

  it('goes back to the first page when the department changes', () => {
    component.pageNumber = 3;

    component.onDepartmentChange(2);

    expect(searchEmployees).toHaveBeenLastCalledWith(
      { name: '', designation: '', departmentId: 2, contact: '' },
      1,
      10
    );
  });

  it('clears every field and reloads', () => {
    component.nameFilter = 'sabina';
    component.departmentId = 2;
    component.searchEmployees();

    component.clearFilters();

    expect(component.nameFilter).toBe('');
    expect(component.departmentId).toBeNull();
    expect(searchEmployees).toHaveBeenLastCalledWith(
      { name: '', designation: '', departmentId: null, contact: '' },
      1,
      10
    );
  });

  it('reports a failed load through the toast service', () => {
    const failing = new EmployeeSearchComponent(
      { searchEmployees: () => throwError(() => new Error('boom')) } as unknown as EmployeeSearchService,
      { getDepartments: () => of(DEPARTMENTS) } as unknown as EmployeeService,
      { markForCheck: vi.fn(), detectChanges: vi.fn() } as unknown as ChangeDetectorRef,
      { success: vi.fn(), error: toastError, confirm: vi.fn() } as unknown as ToastService
    );

    failing.searchEmployees();

    expect(failing.employees).toEqual([]);
    expect(failing.errorMessage).not.toBe('');
    expect(toastError).toHaveBeenCalledWith(failing.errorMessage);
  });
});
