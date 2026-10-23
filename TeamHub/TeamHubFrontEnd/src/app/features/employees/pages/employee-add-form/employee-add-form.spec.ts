import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNativeDateAdapter } from '@angular/material/core';
import { of } from 'rxjs';
import { EmployeeService } from '../../services/employee.service';
import { EmployeeAddFormComponent } from './employee-add-form';
import { ToastService } from '../../../../shared/services/toast.service';

describe('EmployeeAddFormComponent', () => {
  let component: EmployeeAddFormComponent;
  let fixture: ComponentFixture<EmployeeAddFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmployeeAddFormComponent],
      providers: [
        provideRouter([]),
        provideNativeDateAdapter(),
        {
          provide: EmployeeService,
          useValue: {
            getEmployeeMasters: () => of({ data: {} }),
            createEmployee: () => of({ success: true, data: 1, message: 'created' })
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(EmployeeAddFormComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('leaves extension number blank on a new employee form', () => {
    expect(component.employeeForm.get('extensionNumber')?.value).toBeNull();
    expect(component.employeeForm.get('extensionNumber')?.hasError('required')).toBe(true);
  });

  it('shows form validation feedback through toast without inline messages', () => {
    const toastService = TestBed.inject(ToastService);
    let toastMessage = '';
    const subscription = toastService.messages$.subscribe((message) => {
      toastMessage = message.message;
    });

    component.onSubmit();
    fixture.detectChanges();

    expect(toastMessage).toContain('Employee number is required.');
    expect(fixture.nativeElement.querySelector('.validation-message')).toBeNull();
    expect(fixture.nativeElement.querySelector('.form-error')).toBeNull();
    subscription.unsubscribe();
  });

  it('clears literal null from inputs and shows untouched-field validation on blur in a toast', () => {
    const control = component.employeeForm.get('firstName')!;
    const input = document.createElement('input');
    input.setAttribute('formControlName', 'firstName');
    input.value = 'null';

    component.normalizeNullInput({ target: input } as unknown as Event, 'firstName');

    expect(input.value).toBe('');
    expect(control.value).toBe('');

    const toastService = TestBed.inject(ToastService);
    let toastMessage = '';
    const subscription = toastService.messages$.subscribe((message) => {
      toastMessage = message.message;
    });
    component.showFieldError('firstName');

    expect(toastMessage).toContain('First name is required.');
    expect(control.touched).toBe(true);
    expect(control.dirty).toBe(false);
    subscription.unsubscribe();
  });

  it('preserves selected skills and dresses and updates skill levels', () => {
    component.employeeForm.patchValue({ skillIds: ['1', '2'], dressIds: ['3'] });
    component.skillLevels = { 1: 4, 2: 2 };

    expect(component.isSkillSelected(1)).toBe(true);
    expect(component.isSkillSelected(3)).toBe(false);
    expect(component.skillLevelFor(1)).toBe(4);
    expect(component.isDressSelected(3)).toBe(true);
    expect(component.isDressSelected(1)).toBe(false);

    component.updateSkillLevel(1, { target: { value: '5' } } as unknown as Event);
    expect(component.skillLevelFor(1)).toBe(5);
  });

  it('synchronizes the joined date between A.D. and B.S. fields', async () => {
    await import('nepali-date-converter');
    await new Promise((resolve) => setTimeout(resolve, 0));

    const joinedDate = new Date(2025, 0, 1);
    component.employeeForm.get('joinedDate')?.setValue(joinedDate);
    const bsDate = component.employeeForm.get('joinedDateBS')?.value;

    expect(bsDate).toBe('2081-09-17');

    component.employeeForm.get('joinedDateBS')?.setValue('2081-09-18');
    const reflectedDate = component.employeeForm.get('joinedDate')?.value as Date;
    expect(reflectedDate).toBeInstanceOf(Date);
    expect([reflectedDate.getFullYear(), reflectedDate.getMonth(), reflectedDate.getDate()]).toEqual([2025, 0, 2]);

    component.employeeForm.get('joinedDateBS')?.setValue('2081-13-32');
    expect(component.employeeForm.get('joinedDateBS')?.hasError('invalidNepaliDate')).toBe(true);
    expect(component.employeeForm.get('joinedDate')?.value).toEqual(reflectedDate);
  });

  it('renders Angular Material Nepali date pickers for both B.S. dates', () => {
    expect(fixture.nativeElement.querySelectorAll('app-nepali-date-picker')).toHaveLength(2);
    expect(fixture.nativeElement.querySelector('#joinedDateBS')).toBeNull();
    expect(fixture.nativeElement.querySelector('#dateOfBirthBS')).toBeNull();
  });
});
