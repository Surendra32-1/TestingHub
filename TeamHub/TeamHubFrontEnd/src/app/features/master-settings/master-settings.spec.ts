import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { ToastService } from '../../shared/services/toast.service';
import { MasterSettings } from './master-settings';

describe('MasterSettings', () => {
  let component: MasterSettings;
  let fixture: ComponentFixture<MasterSettings>;
  let routeParams: BehaviorSubject<ReturnType<typeof convertToParamMap>>;

  beforeEach(async () => {
    routeParams = new BehaviorSubject(convertToParamMap({ masterKey: 'cotivity-employee' }));
    await TestBed.configureTestingModule({
      imports: [MasterSettings],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: { paramMap: routeParams.asObservable() }
        },
        {
          provide: HttpClient,
          useValue: {
            get: () => of({ success: true, message: '', data: [] }),
            post: () => of({ success: true, message: '', data: {} }),
            put: () => of({}),
            delete: () => of({})
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MasterSettings);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should configure Cotivity employee request fields in Settings', () => {
    expect(component.fields.map((field) => field.key)).toEqual([
      'cotivityEmployeeName',
      'cotivityDepartmentId',
      'cotivityDepartmentDesignationId',
      'reportsToCotivityEmployeeId',
      'isActive'
    ]);
    expect(component.form.get('reportsToCotivityEmployeeId')?.value).toBeNull();
    expect(component.form.get('reportsToCotivityEmployeeId')?.valid).toBe(true);
    const reportsToField = component.fields.find((field) => field.key === 'reportsToCotivityEmployeeId');
    expect(reportsToField?.type).toBe('select');
    expect(reportsToField?.optionsSource).toBe('cotivityEmployees');
  });

  it('leaves required rank blank while allowing an explicit zero', () => {
    component.fields = [{ key: 'rank', label: 'Rank', type: 'number', min: 0, integer: true }];
    component.resetForm();

    const rank = component.form.get('rank');
    expect(rank?.value).toBeNull();
    expect(rank?.hasError('required')).toBe(true);

    rank?.setValue(0);
    expect(rank?.valid).toBe(true);
  });

  it('should show required-field feedback when department and designation are missing', () => {
    const toastService = TestBed.inject(ToastService);
    let toastMessage = '';
    const subscription = toastService.messages$.subscribe((message) => {
      toastMessage = message.message;
    });

    component.save();
    fixture.detectChanges();

    expect(toastMessage).toContain('Cotivity department is required.');
    expect(toastMessage).toContain('Cotivity designation is required.');
    expect(fixture.nativeElement.querySelector('.field-error')).toBeNull();
    subscription.unsubscribe();
  });

  it('should display the reporting employee name from its ID when the API omits the name', () => {
    component.cotivityEmployees = [{ id: 7, name: 'Ravi Macherla' }];
    const field = component.fields.find((item) => item.key === 'reportsToCotivityEmployeeId')!;

    expect(component.displayValue({ reportsToCotivityEmployeeId: 7 }, field)).toBe('Ravi Macherla');
    expect(component.displayValue({ reportsToCotivityEmployeeId: null }, field)).toBe('-');
  });

  it('should treat the string "null" as empty when editing and displaying master values', () => {
    component.fields = [
      { key: 'cotivityEmployeeName', label: 'Cotivity employee name', type: 'text' },
      { key: 'rank', label: 'Rank', type: 'number' },
      { key: 'isActive', label: 'Active', type: 'checkbox' }
    ];
    component.resetForm();
    component.edit({
      cotivityEmployeeId: 12,
      cotivityEmployeeName: 'null',
      rank: null,
      isActive: true
    });

    expect(component.form.get('cotivityEmployeeName')?.value).toBe('');
    expect(component.form.get('rank')?.value).toBeNull();
    expect(component.displayValue(
      { cotivityEmployeeName: 'null' },
      { key: 'cotivityEmployeeName', label: 'Cotivity employee name', type: 'text' }
    )).toBe('-');
  });

  it('clears literal null from inputs and shows touched-field validation in a toast', () => {
    const control = component.form.get('cotivityEmployeeName')!;
    const input = document.createElement('input');
    input.setAttribute('formControlName', 'cotivityEmployeeName');
    input.value = 'null';

    component.normalizeNullInput({ target: input } as unknown as Event, 'cotivityEmployeeName');

    expect(input.value).toBe('');
    expect(control.value).toBe('');

    const toastService = TestBed.inject(ToastService);
    let toastMessage = '';
    const subscription = toastService.messages$.subscribe((message) => {
      toastMessage = message.message;
    });
    component.showFieldError('cotivityEmployeeName');

    expect(toastMessage).toContain('Cotivity employee name is required.');
    expect(control.touched).toBe(true);
    expect(control.dirty).toBe(false);
    subscription.unsubscribe();
  });

  it('normalizes literal null values already present in all master form control types', () => {
    component.fields = [
      { key: 'name', label: 'Name', type: 'text' },
      { key: 'rank', label: 'Rank', type: 'number' },
      { key: 'departmentId', label: 'Department', type: 'select', required: false },
      { key: 'isActive', label: 'Active', type: 'checkbox' }
    ];
    component.resetForm();
    component.form.patchValue({
      name: 'null',
      rank: 'null',
      departmentId: 'null'
    });

    component.normalizeNullFormValues();

    expect(component.form.get('name')?.value).toBe('');
    expect(component.form.get('rank')?.value).toBeNull();
    expect(component.form.get('departmentId')?.value).toBeNull();
    expect(component.form.get('isActive')?.value).toBe(true);
  });

  it('does not render a null placeholder on non-Cotivity text and number inputs', () => {
    routeParams.next(convertToParamMap({ masterKey: 'designation' }));
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const inputs = Array.from(host.querySelectorAll<HTMLInputElement>('form input'))
      .filter((input) => input.type === 'text' || input.type === 'number');
    expect(inputs.map((input) => input.getAttribute('placeholder'))).toEqual([null, null]);
    expect(inputs.map((input) => input.value)).toEqual(['', '']);
  });
});
