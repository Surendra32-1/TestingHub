import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNativeDateAdapter } from '@angular/material/core';
import { of } from 'rxjs';
import { EmployeeService } from '../../services/employee.service';
import { ProfileService } from '../../../profile/profile.service';
import { EmployeeAddFormComponent } from './employee-add-form';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({ standalone: true, template: '' })
class EmptyTestPage {}

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
  let lastEmployeeRequest: Record<string, unknown> | null;
  let lastUpdatedEmployeeId: number | null;
  let activeInactiveEmployeeResponse = employeeEditResponse;

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
    lastEmployeeRequest = null;
    lastUpdatedEmployeeId = null;
    activeInactiveEmployeeResponse = employeeEditResponse;
    await TestBed.configureTestingModule({
      imports: [EmployeeAddFormComponent],
      providers: [
        provideRouter([{ path: 'administration', component: EmptyTestPage }]),
        provideNativeDateAdapter(),
        {
          provide: EmployeeService,
          useValue: {
            getEmployeeMasters: () => of({ data: employeeMastersResponse }),
            getActiveInactiveEmployeeById: () => of({ data: activeInactiveEmployeeResponse }),
            createEmployee: (request: Record<string, unknown>) => {
              lastEmployeeRequest = request;
              return of({ success: true, data: 1, message: 'created' });
            },
            updateActiveInactiveEmployee: (employeeId: number, request: Record<string, unknown>) => {
              lastUpdatedEmployeeId = employeeId;
              lastEmployeeRequest = request;
              return of(undefined);
            }
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

  const fillRequiredEmployeeFields = (): void => {
    component.employeeForm.patchValue({
      eNumber: 1001,
      firstName: 'Priya',
      lastName: 'Sharma',
      email: 'priya@teamhub.test',
      emergencyContactPerson: 'Emergency contact',
      fatherName: 'Parent',
      extensionNumber: 101,
      citizenshipNumber: 'CIT-1001',
      panNumber: 'PAN-1001',
      departmentId: '1',
      designationId: '1',
      jobTypeId: '1',
      bloodGroupId: '1',
      genderId: '1',
      teamId: '1'
    });
  };

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

    component.setSkillLevel(1, { target: { value: '5' } } as unknown as Event);
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

  it('synchronizes the date of birth between A.D. and B.S. fields', async () => {
    await import('nepali-date-converter');
    await new Promise((resolve) => setTimeout(resolve, 0));

    component.employeeForm.get('dateOfBirth')?.setValue(new Date(1990, 0, 1));
    expect(component.employeeForm.get('dateOfBirthBS')?.value).toBe('2046-09-17');

    component.employeeForm.get('dateOfBirthBS')?.setValue('2046-09-18');
    const reflectedDate = component.employeeForm.get('dateOfBirth')?.value as Date;
    expect([reflectedDate.getFullYear(), reflectedDate.getMonth(), reflectedDate.getDate()]).toEqual([1990, 0, 2]);
  });

  it('synchronizes education row dates between A.D. and B.S. fields', async () => {
    await import('nepali-date-converter');
    await new Promise((resolve) => setTimeout(resolve, 0));

    component.addEducation();
    const row = component.educations.at(0);

    row.get('joinedDate')?.setValue(new Date(2025, 0, 1));
    expect(row.get('joinedDateBS')?.value).toBe('2081-09-17');

    row.get('passoutDateBS')?.setValue('2081-09-18');
    const reflectedDate = row.get('passoutDate')?.value as Date;
    expect(reflectedDate).toBeInstanceOf(Date);
    expect([reflectedDate.getFullYear(), reflectedDate.getMonth(), reflectedDate.getDate()]).toEqual([2025, 0, 2]);
  });

  it('synchronizes training row dates between A.D. and B.S. fields', async () => {
    await import('nepali-date-converter');
    await new Promise((resolve) => setTimeout(resolve, 0));

    component.addTraining();
    const row = component.trainings.at(0);

    row.get('joinedDate')?.setValue(new Date(2025, 0, 1));
    expect(row.get('joinedDateBS')?.value).toBe('2081-09-17');

    row.get('passoutDateBS')?.setValue('2081-09-18');
    const reflectedDate = row.get('passoutDate')?.value as Date;
    expect([reflectedDate.getFullYear(), reflectedDate.getMonth(), reflectedDate.getDate()]).toEqual([2025, 0, 2]);
  });

  it('drops empty education and training rows and keeps only populated records', async () => {
    await import('nepali-date-converter');
    await new Promise((resolve) => setTimeout(resolve, 0));
    fillRequiredEmployeeFields();

    component.addEducation(); 
    component.addEducation();
    component.educations.at(1).patchValue({ qualification: 'B.Sc. CSIT' });
    component.addTraining(); 

    component.onSubmit();

    const educations = lastEmployeeRequest?.['educations'] as Array<Record<string, unknown>>;
    expect(educations.length).toBe(1);
    expect(educations[0]['qualification']).toBe('B.Sc. CSIT');
    expect(lastEmployeeRequest?.['trainings']).toEqual([]);
  });

  it('validates joining dates, minimum age, and date-of-birth ordering', async () => {
    await import('nepali-date-converter');
    await new Promise((resolve) => setTimeout(resolve, 0));
    const joiningDate = component.employeeForm.get('joinedDate')!;
    const dateOfBirth = component.employeeForm.get('dateOfBirth')!;

    expect(dateOfBirth.errors).toBeNull();

    const futureJoiningDate = new Date(component.today);
    futureJoiningDate.setDate(futureJoiningDate.getDate() + 1);
    joiningDate.setValue(futureJoiningDate);
    expect(joiningDate.hasError('futureJoiningDate')).toBe(true);

    joiningDate.setValue(new Date(2000, 0, 1));
    dateOfBirth.setValue(new Date(2000, 0, 1));
    expect(dateOfBirth.hasError('dateOfBirthNotBeforeJoining')).toBe(true);

    joiningDate.setValue(component.today);
    const underageDateOfBirth = new Date(component.maxDateOfBirth);
    underageDateOfBirth.setDate(underageDateOfBirth.getDate() + 1);
    dateOfBirth.setValue(underageDateOfBirth);
    expect(dateOfBirth.hasError('minimumAge')).toBe(true);

    const futureDateOfBirth = new Date(component.today);
    futureDateOfBirth.setDate(futureDateOfBirth.getDate() + 1);
    dateOfBirth.setValue(futureDateOfBirth);
    expect(dateOfBirth.hasError('futureDateOfBirth')).toBe(true);

    dateOfBirth.setValue(component.maxDateOfBirth);
    expect(dateOfBirth.errors).toBeNull();
  });

  it('shows a toast immediately when an A.D. or B.S. date change is invalid', async () => {
    await import('nepali-date-converter');
    await new Promise((resolve) => setTimeout(resolve, 0));
    const toastService = TestBed.inject(ToastService);
    const toastMessages: string[] = [];
    const subscription = toastService.messages$.subscribe((message) => toastMessages.push(message.message));
    const futureJoiningDate = new Date(component.today);
    futureJoiningDate.setDate(futureJoiningDate.getDate() + 1);

    component.employeeForm.get('joinedDate')?.setValue(futureJoiningDate);
    expect(toastMessages.at(-1)).toContain('Date of joining cannot be in the future.');

    component.employeeForm.get('joinedDate')?.setValue(new Date(2010, 0, 1));
    component.employeeForm.get('dateOfBirth')?.setValue(new Date(2004, 0, 1));
    expect(toastMessages.at(-1)).toContain('at least 18 years old on the date of joining');

    component.employeeForm.get('dateOfBirthBS')?.setValue('not-a-date');
    expect(toastMessages.at(-1)).toContain('Date of birth (B.S.) must be a valid date in YYYY-MM-DD format.');
    subscription.unsubscribe();
  });

  it('blocks profile updates and shows a toast when the employee was under 18 at joining', async () => {
    await import('nepali-date-converter');
    component.isProfileMode = true;
    component['loadMyProfile']();
    await fixture.whenStable();
    await new Promise((resolve) => setTimeout(resolve, 0));

    const toastService = TestBed.inject(ToastService);
    let toastMessage = '';
    const subscription = toastService.messages$.subscribe((message) => {
      toastMessage = message.message;
    });

    component.employeeForm.get('joinedDate')?.setValue(new Date(2010, 0, 1));
    component.employeeForm.get('dateOfBirth')?.setValue(new Date(2004, 0, 1));
    expect(component.employeeForm.get('dateOfBirth')?.hasError('minimumAge')).toBe(true);

    component.onSubmit();

    expect(lastProfilePatch).toBeNull();
    expect(toastMessage).toContain('at least 18 years old on the date of joining');
    subscription.unsubscribe();
  });

  it('blocks employee edits and shows a toast when the employee was under 18 at joining', async () => {
    await import('nepali-date-converter');
    await new Promise((resolve) => setTimeout(resolve, 0));
    component.employeeId = employeeEditResponse.employeeId;
    component.loadMasters(employeeEditResponse.employeeId);
    fillRequiredEmployeeFields();

    const toastService = TestBed.inject(ToastService);
    let toastMessage = '';
    const subscription = toastService.messages$.subscribe((message) => {
      toastMessage = message.message;
    });

    component.employeeForm.get('joinedDate')?.setValue(new Date(2010, 0, 1));
    component.employeeForm.get('dateOfBirth')?.setValue(new Date(2004, 0, 1));
    expect(component.employeeForm.get('dateOfBirth')?.hasError('minimumAge')).toBe(true);

    component.onSubmit();

    expect(lastEmployeeRequest).toBeNull();
    expect(toastMessage).toContain('at least 18 years old on the date of joining');
    subscription.unsubscribe();
  });

  it('renders Angular Material Nepali date pickers for both B.S. dates', () => {
    expect(fixture.nativeElement.querySelectorAll('app-nepali-date-picker')).toHaveLength(2);
    expect(fixture.nativeElement.querySelector('#joinedDateBS')).toBeNull();
    expect(fixture.nativeElement.querySelector('#dateOfBirthBS')).toBeNull();
    expect((fixture.nativeElement.querySelector('#employeeImage') as HTMLInputElement).accept).toContain('image/png');
  });

  it('reads PNG uploads even when the browser does not report a MIME type', async () => {
    const input = document.createElement('input');
    const file = new File([new Uint8Array([137, 80, 78, 71])], 'employee.png', { type: '' });
    Object.defineProperty(input, 'files', { configurable: true, value: [file] });

    component.onImageSelected({ target: input } as unknown as Event);
    await new Promise((resolve) => {
      const check = () => (component.imageBase64 ? resolve(undefined) : setTimeout(check, 10));
      check();
      setTimeout(resolve, 500);
    });

    expect(component.imageBase64).toMatch(/^data:image\/png;base64,/);
    expect(component.imageFileName).toBe('employee.png');
  });

  it('allows profile date changes and includes both calendars in the profile patch', async () => {
    await import('nepali-date-converter');
    component.isProfileMode = true;
    component['loadMyProfile']();
    await fixture.whenStable();
    await new Promise((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();

    expect(component.employeeForm.get('joinedDate')?.enabled).toBe(true);
    expect(component.employeeForm.get('joinedDateBS')?.enabled).toBe(true);
    expect(component.employeeForm.get('dateOfBirth')?.enabled).toBe(true);
    expect(component.employeeForm.get('dateOfBirthBS')?.enabled).toBe(true);

    component.employeeForm.get('joinedDateBS')?.setValue('2081-09-18');
    component.employeeForm.get('dateOfBirthBS')?.setValue('2046-09-17');
    expect(component.profileHasChanges).toBe(true);
    expect(component.employeeForm.get('joinedDateBS')?.errors).toBeNull();
    expect(component.employeeForm.get('dateOfBirthBS')?.errors).toBeNull();
    expect(component.profileData).not.toBeNull();
    component.employeeForm.markAllAsTouched();
    const invalidProfileFields = [
      'alternateEmail', 'bloodGroupId', 'mobileNumber', 'emergencyContactNumber', 'motherName',
      'spouseName', 'joinedDate', 'joinedDateBS', 'dateOfBirth', 'dateOfBirthBS', 'latitude', 'longitude'
    ].map((field) => [field, component.validationMessage(field)] as const)
      .filter(([, message]) => message !== null);
    expect(invalidProfileFields).toEqual([]);
    component.onSubmit();

    expect(lastProfilePatch).toEqual(expect.objectContaining({
      joinedDate: '2025-01-02',
      joinedDateBS: '2081-09-18',
      dateOfBirth: '1990-01-01',
      dateOfBirthBS: '2046-09-17'
    }));
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

  it('adds and removes skills without assigning a level and keeps dress sizes as chips', () => {
    component.employeeForm.patchValue({ skillIds: ['1'], dressIds: ['5'] });
    component.skillLevels = { 1: 3 };

    expect(component.selectedSkills).toEqual([{ skillId: 1, skillName: 'Angular', skillLevel: 3 }]);
    expect(component.availableSkills.map((skill) => skill.skillName)).toEqual(['.NET']);
    expect(component.selectedDressSizes.map((dress) => dress.dressName)).toEqual(['Jersey']);
    expect(component.availableDressSizes.map((dress) => dress.dressName)).toEqual(['Shirt']);

    component.addSkill({ target: { value: '2' } } as unknown as Event);
    expect(component.employeeForm.get('skillIds')?.value).toEqual(['1', '2']);
    expect(component.skillLevelFor(2)).toBeNull();
    expect(component.selectedSkills[1].skillLevel).toBeNull();

    component.addDress({ target: { value: '6' } } as unknown as Event);
    expect(component.employeeForm.get('dressIds')?.value).toEqual(['5', '6']);
    expect(component.availableDressSizes).toEqual([]);

    component.removeSkill(1);
    component.removeDress(5);

    expect(component.employeeForm.get('skillIds')?.value).toEqual(['2']);
    expect(component.employeeForm.get('dressIds')?.value).toEqual(['6']);
    expect(component.skillLevels[1]).toBeUndefined();
  });

  it('requires an explicit level selection for each newly added skill', () => {
    component.addSkill({ target: { value: '1' } } as unknown as Event);

    expect(component.employeeForm.get('skillIds')?.value).toEqual(['1']);
    expect(component.selectedSkills).toEqual([{ skillId: 1, skillName: 'Angular', skillLevel: null }]);

    component.setSkillLevel(1, { target: { value: '2' } } as unknown as Event);
    expect(component.selectedSkills).toEqual([{ skillId: 1, skillName: 'Angular', skillLevel: 2 }]);
  });

  it('does not send a newly added skill until its level is explicitly selected', async () => {
    await import('nepali-date-converter');
    await new Promise((resolve) => setTimeout(resolve, 0));
    fillRequiredEmployeeFields();
    component.addSkill({ target: { value: '1' } } as unknown as Event);
    const toastService = TestBed.inject(ToastService);
    let toastMessage = '';
    const subscription = toastService.messages$.subscribe((message) => {
      toastMessage = message.message;
    });

    component.onSubmit();

    expect(lastEmployeeRequest).toBeNull();
    expect(toastMessage).toContain('Select a level for Angular before saving.');
    subscription.unsubscribe();
  });

  it('sends each individually selected skill level when creating an employee', async () => {
    await import('nepali-date-converter');
    await new Promise((resolve) => setTimeout(resolve, 0));
    fillRequiredEmployeeFields();
    component.employeeForm.patchValue({ skillIds: ['1', '2'] });
    component.setSkillLevel(1, { target: { value: '5' } } as unknown as Event);
    component.setSkillLevel(2, { target: { value: '3' } } as unknown as Event);
    expect(component.selectedSkills.map((skill) => skill.skillLevel)).toEqual([5, 3]);

    component.onSubmit();

    expect(lastEmployeeRequest?.['skills']).toEqual([
      { skillId: 1, skillLevel: 5, isPrimary: false },
      { skillId: 2, skillLevel: 3, isPrimary: false }
    ]);
  });

  it('saves each selected skill level through the profile update payload', async () => {
    await import('nepali-date-converter');
    await new Promise((resolve) => setTimeout(resolve, 0));
    component.isProfileMode = true;
    component.employeeId = 1;
    fixture.detectChanges();
    component.profileData = profileResponse as never;
    component.employeeForm.patchValue({ skillIds: ['1', '2'], bloodGroupId: '1' });
    component.skillLevels = { 1: 1, 2: 1 };

    component.setSkillLevel(1, { target: { value: '4' } } as unknown as Event);
    component.setSkillLevel(2, { target: { value: '4' } } as unknown as Event);
    expect(component.selectedSkills.map((skill) => skill.skillLevel)).toEqual([4, 4]);
    component.employeeForm.markAllAsTouched();
    const invalidProfileFields = [
      'alternateEmail', 'bloodGroupId', 'mobileNumber', 'emergencyContactNumber', 'motherName',
      'spouseName', 'joinedDate', 'joinedDateBS', 'dateOfBirth', 'dateOfBirthBS', 'latitude', 'longitude'
    ].map((field) => [field, component.validationMessage(field)] as const)
      .filter(([, message]) => message !== null);
    expect(invalidProfileFields).toEqual([]);

    component.onSubmit();

    expect(lastProfilePatch).not.toBeNull();
    expect(lastProfilePatch?.['skills']).toEqual([
      { skillId: 1, skillLevel: 4 },
      { skillId: 2, skillLevel: 4 }
    ]);
  });

  it('blocks profile updates when a newly added skill has no selected level', () => {
    component.isProfileMode = true;
    component.profileData = profileResponse as never;
    component.employeeForm.patchValue({ skillIds: ['1', '2'], bloodGroupId: '1' });
    component.skillLevels = { 1: 1 };
    const toastService = TestBed.inject(ToastService);
    let toastMessage = '';
    const subscription = toastService.messages$.subscribe((message) => {
      toastMessage = message.message;
    });

    component.onSubmit();

    expect(lastProfilePatch).toBeNull();
    expect(toastMessage).toContain('Select a level for .NET before saving.');
    subscription.unsubscribe();
  });

  it('saves the level selected for a skill', async () => {
    await import('nepali-date-converter');
    await new Promise((resolve) => setTimeout(resolve, 0));
    component.isProfileMode = true;
    component.employeeId = 1;
    fixture.detectChanges();
    component.profileData = profileResponse as never;
    component.employeeForm.patchValue({ skillIds: ['1'], bloodGroupId: '1' });
    component.skillLevels = { 1: 1 };
    fixture.detectChanges();

    const levelSelect = fixture.nativeElement.querySelector('[aria-label="Skill level for Angular"]') as HTMLSelectElement;
    levelSelect.value = '4';
    levelSelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(component.skillLevelFor(1)).toBe(4);
    expect(levelSelect.value).toBe('4');

    component.onSubmit();

    expect(lastProfilePatch?.['skills']).toEqual([{ skillId: 1, skillLevel: 4 }]);
  });

  it('does not show a default level control for newly selected skills', () => {
    component.addSkill({ target: { value: '1' } } as unknown as Event);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[aria-label="Default level for new skills"]')).toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('Apply level to selected skills');
    expect((fixture.nativeElement.querySelector('[aria-label="Skill level for Angular"]') as HTMLSelectElement).value).toBe('');
  });

  it('sends each selected skill level when creating an employee', async () => {
    await import('nepali-date-converter');
    await new Promise((resolve) => setTimeout(resolve, 0));
    fillRequiredEmployeeFields();
    component.employeeForm.patchValue({ skillIds: ['1', '2'] });
    component.skillLevels = { 1: 2, 2: 5 };
    expect(Object.entries(component.employeeForm.controls)
      .filter(([, control]) => control.invalid)
      .map(([name]) => name)).toEqual([]);

    component.onSubmit();

    expect(lastEmployeeRequest?.['skills']).toEqual([
      { skillId: 1, skillLevel: 2, isPrimary: false },
      { skillId: 2, skillLevel: 5, isPrimary: false }
    ]);
  });

  it('sends updated skill levels when editing an employee', async () => {
    await import('nepali-date-converter');
    await new Promise((resolve) => setTimeout(resolve, 0));
    component.employeeId = employeeEditResponse.employeeId;
    fillRequiredEmployeeFields();
    component.employeeForm.patchValue({ skillIds: ['2'] });
    component.skillLevels = { 2: 3 };
    expect(Object.entries(component.employeeForm.controls)
      .filter(([, control]) => control.invalid)
      .map(([name]) => name)).toEqual([]);

    component.onSubmit();

    expect(lastUpdatedEmployeeId).toBe(employeeEditResponse.employeeId);
    expect(lastEmployeeRequest?.['skills']).toEqual([
      { skillId: 2, skillLevel: 3, isPrimary: false }
    ]);
  });

  it('loads the persisted skill level before changing it in employee edit', async () => {
    await import('nepali-date-converter');
    await new Promise((resolve) => setTimeout(resolve, 0));
    component.employeeId = employeeEditResponse.employeeId;
    component.loadMasters(employeeEditResponse.employeeId);
    fillRequiredEmployeeFields();
    component.employeeForm.get('joinedDate')?.setValue(component.today);

    expect(component.skillLevelFor(2)).toBe(4);
    expect(component.employeeForm.get('skillIds')?.value).toEqual(['2']);
    component.setSkillLevel(2, { target: { value: '5' } } as unknown as Event);
    expect(Object.entries(component.employeeForm.controls)
      .filter(([, control]) => control.invalid)
      .map(([name]) => name)).toEqual([]);
    component.onSubmit();

    expect(lastUpdatedEmployeeId).toBe(employeeEditResponse.employeeId);
    expect(lastEmployeeRequest?.['skills']).toEqual([
      { skillId: 2, skillLevel: 5, isPrimary: false }
    ]);
  });

  it('loads and sends both persisted level-3 values in the employee update request', async () => {
    await import('nepali-date-converter');
    await new Promise((resolve) => setTimeout(resolve, 0));
    activeInactiveEmployeeResponse = {
      ...employeeEditResponse,
      skills: [
        { employeeSkillId: 1, skillId: 1, skillName: 'Angular', skillLevel: 3, isPrimary: false },
        { employeeSkillId: 2, skillId: 2, skillName: '.NET', skillLevel: 3, isPrimary: false }
      ]
    };
    component.employeeId = employeeEditResponse.employeeId;
    component.loadMasters(employeeEditResponse.employeeId);
    fillRequiredEmployeeFields();
    component.employeeForm.get('joinedDate')?.setValue(component.today);
    fixture.detectChanges();

    const levelSelectors = [1, 2].map((skillId) =>
      fixture.nativeElement.querySelector(
        `[aria-label="Skill level for ${skillId === 1 ? 'Angular' : '.NET'}"]`
      ) as HTMLSelectElement
    );
    expect(levelSelectors.map((select) => select.value)).toEqual(['3', '3']);

    component.onSubmit();

    expect(lastUpdatedEmployeeId).toBe(employeeEditResponse.employeeId);
    expect(lastEmployeeRequest?.['skills']).toEqual([
      { skillId: 1, skillLevel: 3, isPrimary: false },
      { skillId: 2, skillLevel: 3, isPrimary: false }
    ]);
  });
});
