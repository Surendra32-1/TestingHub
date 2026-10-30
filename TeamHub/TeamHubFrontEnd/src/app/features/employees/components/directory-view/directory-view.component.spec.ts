import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { Employee } from '../../models/employee.model';
import { EmployeeService } from '../../services/employee.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { DirectoryViewComponent } from './directory-view.component';

const EMPLOYEE: Employee = {
  id: 1,
  name: 'Avery Employee',
  designation: 'Engineer',
  designationRank: 1,
  department: 'Engineering',
  teamName: 'Core Platform',
  imageBase64: '',
  accent: '#ffffff'
};

describe('DirectoryViewComponent management table', () => {
  let fixture: ComponentFixture<DirectoryViewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DirectoryViewComponent],
      providers: [
        provideRouter([]),
        { provide: EmployeeService, useValue: {} },
        { provide: ToastService, useValue: {} }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DirectoryViewComponent);
    fixture.componentRef.setInput('tableLayout', true);
    fixture.componentRef.setInput('managementLayout', true);
  });

  it('omits action headings and cells when their permissions are not granted', () => {
    fixture.componentRef.setInput('employees', [EMPLOYEE]);
    fixture.componentRef.setInput('canViewCard', false);
    fixture.componentRef.setInput('canEditEmployee', false);
    fixture.componentRef.setInput('canDeleteEmployee', false);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const headings = Array.from(host.querySelectorAll('th')).map((heading) => heading.textContent?.trim());
    const employeeRow = host.querySelector('tbody tr');

    expect(headings).not.toContain('Card View');
    expect(headings).not.toContain('Edit');
    expect(headings).not.toContain('Delete');
    expect(employeeRow?.querySelectorAll('td')).toHaveLength(8);
    expect(fixture.componentInstance.managementColumnCount).toBe(8);
  });

  it('renders action headings and cells when their permissions are granted', () => {
    fixture.componentRef.setInput('employees', [EMPLOYEE]);
    fixture.componentRef.setInput('canViewCard', true);
    fixture.componentRef.setInput('canEditEmployee', true);
    fixture.componentRef.setInput('canDeleteEmployee', true);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const headings = Array.from(host.querySelectorAll('th')).map((heading) => heading.textContent?.trim());
    const employeeRow = host.querySelector('tbody tr');

    expect(headings).toContain('Card View');
    expect(headings).toContain('Edit');
    expect(headings).toContain('Delete');
    expect(employeeRow?.querySelectorAll('td')).toHaveLength(11);
    expect(fixture.componentInstance.managementColumnCount).toBe(11);
  });

  it('shows only one loading message in the table layout', () => {
    fixture.componentRef.setInput('isLoading', true);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const loadingMessages = host.textContent?.match(/Loading employees\.\.\./g) ?? [];

    expect(loadingMessages).toHaveLength(1);
  });
});
