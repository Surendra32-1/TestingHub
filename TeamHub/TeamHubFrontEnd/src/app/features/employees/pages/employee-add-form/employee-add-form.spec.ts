import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNativeDateAdapter } from '@angular/material/core';
import { of } from 'rxjs';
import { EmployeeService } from '../../services/employee.service';
import { ProfileService } from '../../../profile/profile.service';
import { EmployeeAddFormComponent } from './employee-add-form';
import { ToastService } from '../../../../shared/services/toast.service';

const employeeMastersResponse = {
  departments: [{ departmentId: 1, departmentName: 'Engineering', isActive: true }],
  designations: [{ designationId: 1, designationName: 'Developer', rank: 1, isActive: true }],
  jobTypes: [{ jobTypeId: 1, jobTypeName: 'Full-Time', isActive: true }],
  bloodGroups: [{ bloodGroupId: 1, bloodGroupName: 'A+', isActive: true }],
  genders: [{ genderId: 1, genderName: 'Male', isActive: true }],
  roles: [
    { roleId: 1, roleName: 'Admin', isActive: true },
    { roleId: 2, roleName: 'HR', isActive: true },
    { roleId: 3, roleName: 'Manager', isActive: true }
  ],
  teams: [{ teamId: 1, teamName: 'Web', isActive: true }],
  skills: [
    { skillId: 1, skillName: 'Angular', isActive: true },
    { skillId: 2, skillName: '.NET', isActive: true }
  ],
  recruitments: [{ recruitmentId: 1, recruitmentName: 'Campus', isActive: true }],
  employees: [{ reportingManagerId: 1, reportingManagerName: 'Emily Davis' }],
  cotivityEmployees: [],
  dressSizes: [
    { dressId: 5, dressName: 'Jersey', size: 'L', isActive: true },
    { dressId: 6, dressName: 'Shirt', size: 'M', isActive: true }
  ],
  businesses: [{ businessUnitId: 1, businessUnitName: 'TeamHub', isActive: true }],
  addressTypes: [{ addressTypeId: 1, addressTypeName: 'Permanent' }]
};

const employeeEditResponse = {
  employeeId: 1006,
  eNumber: 1006,
  firstName: 'Priya',
  middleName: null,
  lastName: 'Sharma',
  joinedDate: '2025-01-01',
  joinedDateBS: '2081-09-17',
  dateOfBirth: null,
  dateOfBirthBS: null,
  email: 'priya@teamhub.test',
  alternateEmail: null,
  phoneNumber: null,
  mobileNumber: null,
  emergencyContactNumber: null,
  emergencyContactPerson: null,
  fatherName: null,
  motherName: null,
  spouseName: null,
  citizenshipNumber: null,
  panNumber: null,
  lockerNumber: null,
  stationNumber: null,
  extensionNumber: 101,
  departmentId: 99,
  departmentName: 'Legacy Ops',
  designationId: 1,
  designationName: 'Developer',
  jobTypeId: 1,
  jobTypeName: 'Full-Time',
  bloodGroupId: 1,
  bloodGroupName: 'A+',
  genderId: 1,
  genderName: 'Male',
  teamId: 1,
  teamName: 'Web',
  roleIds: [1, 3],
  roleName: 'Admin, Manager',
  recruitmentId: 1,
  recruitmentName: 'Campus',
  reportsToEmployeeId: 1,
  reportsToEmployeeName: 'Emily Davis',
  leadEmployeeId: null,
  leadEmployeeName: null,
  cotivityEmployeeId: null,
  cotivityEmployeeName: null,
  imageBase64: null,
  isActive: true,
  addresses: [{ addressId: 1, addressTypeId: 7, addressTypeName: 'Temporary', addressLine1: 'Street 1', addressLine2: null, city: 'Kathmandu', state: 'Bagmati', latitude: 27.7, longitude: 85.3, isPrimary: true }],
  skills: [{ employeeSkillId: 1, skillId: 2, skillName: '.NET', skillLevel: 4, isPrimary: false }],
  dressSizes: [{ dressId: 6, dressName: 'Shirt', size: 'M', isActive: true }]
};

describe('EmployeeAddFormComponent', () => {
  let component: EmployeeAddFormComponent;
  let fixture: ComponentFixture<EmployeeAddFormComponent>;
  let lastProfilePatch: Record<string, unknown> | null;

  const profileEmployee = {
    ...employeeEditResponse,
    skills: [
      { employeeSkillId: 1, skillId: 1, skillName: 'Angular', skillLevel: 1, isPrimary: false },
      { employeeSkillId: 2, skillId: 2, skillName: '.NET', skillLevel: 1, isPrimary: false }
    ]
  };
  const profileResponse = {
    username: 'priya.sharma',
    employee: profileEmployee,
    bloodGroups: [],
    roles: [],
    skillOptions: employeeMastersResponse.skills,
    dressSizeOptions: employeeMastersResponse.dressSizes
  };

  beforeEach(async () => {
    lastProfilePatch = null;
    await TestBed.configureTestingModule({
      imports: [EmployeeAddFormComponent],
      providers: [
        provideRouter([]),
        provideNativeDateAdapter(),
        {
          provide: EmployeeService,
          useValue: {
            getEmployeeMasters: () => of({ data: employeeMastersResponse }),
            getActiveInactiveEmployeeById: () => of({ data: employeeEditResponse }),
            createEmployee: () => of({ success: true, data: 1, message: 'created' })
          }
        },
        {
          provide: ProfileService,
          useValue: {
            getMyProfile: () => of(profileResponse),
            updateMyProfile: (patch: Record<string, unknown>) => {
              lastProfilePatch = patch;
              return of(profileResponse);
            }
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

    component.chooseSkillLevel(1, 5, new MouseEvent('click'));
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

  it('reads several roles from the employee-master API response and offers only unassigned roles', () => {
    expect(component.roles).toEqual(employeeMastersResponse.roles);

    component.employeeForm.patchValue({ roleIds: ['1', '3'] });

    expect(component.selectedRoles.map((role) => role.roleName)).toEqual(['Admin', 'Manager']);
    expect(component.availableRoles.map((role) => role.roleName)).toEqual(['HR']);

    const addSelect = { value: '2' } as HTMLSelectElement;
    component.addRole({ target: addSelect } as unknown as Event);

    expect(component.employeeForm.get('roleIds')?.value).toEqual(['1', '3', '2']);
    expect(component.availableRoles).toEqual([]);

    component.removeRole(1);

    expect(component.employeeForm.get('roleIds')?.value).toEqual(['3', '2']);
    expect(component.selectedRoles.map((role) => role.roleName)).toEqual(['HR', 'Manager']);
  });

  it('loads skills and dress sizes from the employee-master API response', () => {
    expect(component.skills).toEqual(employeeMastersResponse.skills);
    expect(component.dressSizes).toEqual(employeeMastersResponse.dressSizes);
  });

  it('keeps dropdown values from the database when their master row is inactive', () => {
    component.loadMasters(employeeEditResponse.employeeId);

    expect(component.employeeForm.get('departmentId')?.value).toBe('99');
    expect(component.departments).toContainEqual(employeeMastersResponse.departments[0]);
    expect(component.departments).toContainEqual({ departmentId: 99, departmentName: 'Legacy Ops' });
    expect(component.addressTypes).toContainEqual({ addressTypeId: 7, addressTypeName: 'Temporary' });
    expect(component.employees).toEqual(employeeMastersResponse.employees);
    expect(component.employeeForm.get('addressTypeId')?.value).toBe('7');
  });

  it('adds and removes skills with a default level, and dress sizes as chips', () => {
    component.employeeForm.patchValue({ skillIds: ['1'], dressIds: ['5'] });
    component.skillLevels = { 1: 3 };

    expect(component.selectedSkills).toEqual([{ skillId: 1, skillName: 'Angular', skillLevel: 3 }]);
    expect(component.availableSkills.map((skill) => skill.skillName)).toEqual(['.NET']);
    expect(component.selectedDressSizes.map((dress) => dress.dressName)).toEqual(['Jersey']);
    expect(component.availableDressSizes.map((dress) => dress.dressName)).toEqual(['Shirt']);

    component.addSkill({ target: { value: '2' } } as unknown as Event);
    expect(component.employeeForm.get('skillIds')?.value).toEqual(['1', '2']);
    expect(component.skillLevelFor(2)).toBe(1);

    component.addDress({ target: { value: '6' } } as unknown as Event);
    expect(component.employeeForm.get('dressIds')?.value).toEqual(['5', '6']);
    expect(component.availableDressSizes).toEqual([]);

    component.removeSkill(1);
    component.removeDress(5);

    expect(component.employeeForm.get('skillIds')?.value).toEqual(['2']);
    expect(component.employeeForm.get('dressIds')?.value).toEqual(['6']);
    expect(component.skillLevels[1]).toBeUndefined();
  });

  it('applies the chosen level when adding a skill and keeps it adjustable on the chip', () => {
    component.updateNewSkillLevel(4);
    expect(component.newSkillLevel).toBe(4);

    component.addSkill({ target: { value: '1' } } as unknown as Event);

    expect(component.employeeForm.get('skillIds')?.value).toEqual(['1']);
    expect(component.selectedSkills).toEqual([{ skillId: 1, skillName: 'Angular', skillLevel: 4 }]);

    component.chooseSkillLevel(1, 2, new MouseEvent('click'));
    expect(component.selectedSkills).toEqual([{ skillId: 1, skillName: 'Angular', skillLevel: 2 }]);

    component.updateNewSkillLevel(9);
    expect(component.newSkillLevel).toBe(1);
  });

  it('re-levels the skills already selected when the level row changes', () => {
    component.addSkill({ target: { value: '1' } } as unknown as Event);
    component.addSkill({ target: { value: '2' } } as unknown as Event);
    expect(component.selectedSkills.map((skill) => skill.skillLevel)).toEqual([1, 1]);

    component.updateNewSkillLevel(4);

    expect(component.skillLevels).toEqual({ 1: 4, 2: 4 });
    expect(component.selectedSkills.map((skill) => skill.skillLevel)).toEqual([4, 4]);
    expect(component.employeeForm.get('skillIds')?.dirty).toBe(true);
  });

  it('keeps the level row in step with the chips', () => {
    component.employeeForm.patchValue({ skillIds: ['1', '2'] });
    component.skillLevels = { 1: 4, 2: 4 };
    expect(component.levelRowValue).toBe(4);

    component.skillLevels = { 1: 4, 2: 2 };
    expect(component.levelRowValue).toBe(1);

    component.skillLevels = {};
    component.employeeForm.patchValue({ skillIds: [] });
    component.updateNewSkillLevel(3);
    expect(component.levelRowValue).toBe(3);
  });

  it('saves the level chosen in the level row through the profile update payload', () => {
    component.isProfileMode = true;
    component.profileData = profileResponse as never;
    component.employeeForm.patchValue({ skillIds: ['1', '2'], bloodGroupId: '1' });
    component.skillLevels = { 1: 1, 2: 1 };

    component.updateNewSkillLevel(4);
    expect(component.levelRowValue).toBe(4);

    component.onSubmit();

    expect(lastProfilePatch).not.toBeNull();
    expect(lastProfilePatch?.['skills']).toEqual([
      { skillId: 1, skillLevel: 4 },
      { skillId: 2, skillLevel: 4 }
    ]);
  });

  it('saves the level picked from a skill chip menu', () => {
    component.isProfileMode = true;
    component.profileData = profileResponse as never;
    component.employeeForm.patchValue({ skillIds: ['1'], bloodGroupId: '1' });
    component.skillLevels = { 1: 1 };
    fixture.detectChanges();

    const toggle = fixture.nativeElement.querySelector('.chip .level-toggle') as HTMLButtonElement;
    toggle.click();
    fixture.detectChanges();

    const options = Array.from(fixture.nativeElement.querySelectorAll('.chip .level-option')) as HTMLButtonElement[];
    const levelFour = options.find((option) => option.textContent?.trim() === '4');
    expect(levelFour).toBeTruthy();
    levelFour!.click();
    fixture.detectChanges();

    expect(component.skillLevelFor(1)).toBe(4);
    expect((fixture.nativeElement.querySelector('.chip .level-toggle') as HTMLButtonElement).textContent?.trim()).toBe('4');

    component.onSubmit();

    expect(lastProfilePatch?.['skills']).toEqual([{ skillId: 1, skillLevel: 4 }]);
  });
});
