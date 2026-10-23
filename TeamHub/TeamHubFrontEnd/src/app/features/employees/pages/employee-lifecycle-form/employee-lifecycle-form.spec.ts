import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { EmployeeService } from '../../services/employee.service';
import { EmployeeLifecycleFormComponent } from './employee-lifecycle-form';

describe('EmployeeLifecycleFormComponent', () => {
  let component: EmployeeLifecycleFormComponent;
  let fixture: ComponentFixture<EmployeeLifecycleFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmployeeLifecycleFormComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              data: { mode: 'release' },
              paramMap: { get: () => null }
            }
          }
        },
        {
          provide: EmployeeService,
          useValue: {
            getEmployees: () => of({ hasNextPage: false, pageNumber: 1, items: [] }),
            createEmployeeRelease: () => of({ success: true }),
            createEmployeeTermination: () => of({ success: true }),
            getEmployeeDetails: () => of({ data: null })
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(EmployeeLifecycleFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('calculates served notice, shortfall, and expected release date', () => {
    component.form.patchValue({
      resignationDate: '2026-10-16',
      noticePeriodDays: 67,
      lastWorkingDate: '2026-10-23'
    });
    fixture.detectChanges();

    expect(component.servedNoticeDays).toBe(7);
    expect(component.noticePeriodShortfallDays).toBe(60);
    expect(component.expectedReleaseDate).toEqual(new Date(2026, 11, 22));
    expect(component.isEarlyRelease).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Notice Period Required');
    expect(fixture.nativeElement.textContent).toContain('Reason for Not Serving Full Notice Period');
  });

  it('rejects a last working day before the resignation date', () => {
    component.form.patchValue({
      resignationDate: '2026-10-16',
      lastWorkingDate: '2026-10-15',
      reason: 'Employee requested release'
    });

    expect(component.form.hasError('lastWorkingBeforeResignation')).toBe(true);
    expect(component.form.invalid).toBe(true);
  });
});
