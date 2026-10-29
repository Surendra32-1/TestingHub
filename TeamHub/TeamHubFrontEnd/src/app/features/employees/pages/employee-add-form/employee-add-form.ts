import { ChangeDetectorRef, Component, DestroyRef, HostListener, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  AbstractControl,
  ValidationErrors,
  ReactiveFormsModule
} from '@angular/forms';
import { Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, map } from 'rxjs';
type NepaliDateConstructor = typeof import('nepali-date-converter').default;
import { ToastService } from '../../../../shared/services/toast.service';
import { getApiErrorMessage } from '../../../../shared/services/api-error-message';

import { CreateEmployeeRequest, EmployeeEditResponse, EmployeeService } from '../../services/employee.service';
import { EmployeeProfile, EmployeeProfileEmployee, EmployeeProfilePatch, ProfileService } from '../../../profile/profile.service';
import { TopbarComponent } from '../../../../shared/components/topbar/topbar.component';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AuthService } from '../../../../core/auth/auth.service';
import { NepaliDatePickerComponent } from '../../components/nepali-date-picker/nepali-date-picker.component';

@Component({
  selector: 'app-employee-add-form',
  standalone: true,
  imports: [
    CommonModule,
    TopbarComponent,
    ReactiveFormsModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    NepaliDatePickerComponent
  ],
  templateUrl: './employee-add-form.html',
  styleUrl: './employee-add-form.css'
})

export class EmployeeAddFormComponent implements OnInit {

  private fb = inject(FormBuilder);
  private employeeService = inject(EmployeeService);
  private profileService = inject(ProfileService);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toastService = inject(ToastService);
  private changeDetectorRef = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);
  private nepaliDate: NepaliDateConstructor | null = null;

  employeeForm!: FormGroup;
  isSubmitting = false;
  isReadingImage = false;
  errorMessage = '';
  imageBase64: string | null = null;
  currentImageBase64: string | null = null;
  imageFileName = '';
  employeeId: number | null = null;
  isProfileMode = false;
  profileData: EmployeeProfile | null = null;
  readonly today = new Date(new Date().setHours(0, 0, 0, 0));
  readonly latestDateOfBirth = new Date(this.today.getTime() - 24 * 60 * 60 * 1000);
  readonly maxDateOfBirth = this.getLatestPastDate();

  departments: any[] = [];
  designations: any[] = [];
  jobTypes: any[] = [];
  bloodGroups: any[] = [];
  genders: any[] = [];
  roles: any[] = [];
  teams: any[] = [];
  skills: any[] = [];
  recruitments: any[] = [];
  employees: any[] = [];
  cotivityEmployees: any[] = [];
  businessUnits: any[] = [];
  dressSizes: any[] = [];
  addressTypes: any[] = [];
  skillLevels: Record<number, number> = {};

  newSkillLevel = 1;

  readonly levelOptions = [1, 2, 3, 4, 5];

  openSkillLevelId: number | null = null;

  levelRowOpen = false;

  @HostListener('document:click')
  closeLevelMenus(): void {
    this.openSkillLevelId = null;
    this.levelRowOpen = false;
  }

  get profileHasChanges(): boolean {
    if (!this.isProfileMode || !this.profileData) {
      return false;
    }

    const employee = this.profileData.employee;
    const values = this.employeeForm.getRawValue();
    const address = employee.addresses.find((item) => item.isPrimary) ?? employee.addresses[0] ?? null;
    const selectedSkillIds = (values.skillIds as Array<number | string> ?? []).map(Number).sort((a, b) => a - b);
    const currentSkillIds = employee.skills.map((skill) => skill.skillId).sort((a, b) => a - b);
    const selectedDressIds = (values.dressIds as Array<number | string> ?? []).map(Number).sort((a, b) => a - b);
    const currentDressIds = employee.dressSizes.map((dress) => dress.dressId).sort((a, b) => a - b);
    const skillsChanged = !this.sameIds(selectedSkillIds, currentSkillIds)
      || employee.skills.some((skill) =>
        selectedSkillIds.includes(skill.skillId) && this.skillLevelFor(skill.skillId) !== skill.skillLevel);
    return this.imageBase64 !== null
      || values.alternateEmail !== (employee.alternateEmail ?? '')
      || Number(values.bloodGroupId) !== employee.bloodGroupId
      || values.mobileNumber !== (employee.mobileNumber ?? '')
      || values.emergencyContactNumber !== (employee.emergencyContactNumber ?? '')
      || values.motherName !== (employee.motherName ?? '')
      || values.spouseName !== (employee.spouseName ?? '')
      || values.latitude !== address?.latitude
      || values.longitude !== address?.longitude
      || skillsChanged
      || !this.sameIds(selectedDressIds, currentDressIds);
  }

  get employeeDisplayName(): string {
    if (!this.employeeForm) {
      return '';
    }

    const values = this.employeeForm.getRawValue();
    return [values.firstName, values.middleName, values.lastName]
      .filter((part): part is string => typeof part === 'string' && part.trim().length > 0)
      .map((part) => part.trim())
      .join(' ');
  }

  get selectedDesignationName(): string {
    return this.selectedOptionName(this.designations, 'designationId', 'designationName', 'designationId');
  }

  get selectedDepartmentName(): string {
    return this.selectedOptionName(this.departments, 'departmentId', 'departmentName', 'departmentId');
  }

  get levelRowValue(): number {
    const levels = this.selectedIds('skillIds').map((skillId) => this.skillLevelFor(skillId));
    return levels.length > 0 && levels.every((level) => level === levels[0])
      ? levels[0]
      : this.newSkillLevel;
  }

  private selectedOptionName(
    options: any[],
    idField: string,
    nameField: string,
    controlName: string
  ): string {
    const selectedId = this.employeeForm?.get(controlName)?.value;
    const match = (options ?? []).find((option) => String(option[idField]) === String(selectedId ?? ''));
    return match?.[nameField] ?? '';
  }

  ngOnInit(): void {
    this.initializeForm();
    this.isProfileMode = this.route.snapshot.data['mode'] === 'profile';
    if (!this.isProfileMode) {
      this.loadNepaliDateConverter();
    }
    const employeeId = Number(this.route.snapshot.paramMap.get('employeeId'));
    if (Number.isInteger(employeeId) && employeeId > 0) {
      this.employeeId = employeeId;
    }
    if (this.isProfileMode) {
      this.loadMyProfile();
    } else {
      this.loadMasters(this.employeeId ?? undefined);
    }
  }

  initializeForm(): void {

    this.employeeForm = this.fb.group({
      eNumber: [null, [Validators.required, Validators.min(1), Validators.max(2147483647), Validators.pattern(/^\d+$/)]],
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      middleName: ['', Validators.maxLength(100)],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],

      email: ['', [Validators.required, Validators.email, Validators.maxLength(150)]],
      alternateEmail: ['', [Validators.email, Validators.maxLength(150)]],
      phoneNumber: ['', Validators.maxLength(20)],
      mobileNumber: ['', Validators.maxLength(20)],
      emergencyContactNumber: ['', Validators.maxLength(20)],
      emergencyContactPerson: ['', [Validators.required, Validators.maxLength(150)]],
      fatherName: ['', [Validators.required, Validators.maxLength(150)]],
      motherName: ['', Validators.maxLength(150)],
      spouseName: ['', Validators.maxLength(150)],

      extensionNumber: [null, [Validators.required, Validators.min(0), Validators.max(2147483647), Validators.pattern(/^\d+$/)]],
      citizenshipNumber: ['', [Validators.required, Validators.maxLength(50)]],
      panNumber: ['', [Validators.required, Validators.maxLength(50)]],
      lockerNumber: ['', Validators.maxLength(20)],
      stationNumber: ['', Validators.maxLength(20)],

      joinedDate: [new Date(), Validators.required],
      joinedDateBS: [this.formatNepaliDate(new Date()), [Validators.required, Validators.pattern(/^\d{4}-\d{2}-\d{2}$/), this.validateNepaliDate]],
      dateOfBirth: [null],
      dateOfBirthBS: ['', [Validators.pattern(/^\d{4}-\d{2}-\d{2}$/), this.validateNepaliDate]],

      address: ['', Validators.maxLength(160)],
      addressLine2: ['', Validators.maxLength(160)],
      city: ['', Validators.maxLength(100)],
      state: ['', Validators.maxLength(100)],
      addressTypeId: [''],

      departmentId: ['', Validators.required],
      designationId: ['', Validators.required],
      jobTypeId: ['', Validators.required],

      bloodGroupId: ['', Validators.required],
      genderId: ['', Validators.required],
      roleIds: [[]],
      teamId: ['', Validators.required],

      recruitmentId: [''],
      leadEmployeeId: [''],
      reportToEmployeeId: [''],
      cotivityEmployeeId: [''],
      skillIds: [[]],
      dressIds: [[]],
      latitude: [null as number | null, [Validators.min(-90), Validators.max(90)]],
      longitude: [null as number | null, [Validators.min(-180), Validators.max(180)]],
      isActive: [true],
      createLoginAccount: [false],
      username: [''],
      initialPassword: [''],
      confirmInitialPassword: ['']
    });
    this.watchDatePair('joinedDate', 'joinedDateBS');
    this.watchDatePair('dateOfBirth', 'dateOfBirthBS');
  }

  private getLatestPastDate(): Date {
    const latestPastDate = new Date();
    latestPastDate.setHours(0, 0, 0, 0);
    latestPastDate.setDate(latestPastDate.getDate() - 1);
    return latestPastDate;
  }

  private loadEmployee(employeeId: number): void {
    this.employeeService.getActiveInactiveEmployeeById(employeeId).subscribe({
      next: ({ data }) => {
        this.mergeAssignedEmployeeOptions(data);
        this.patchEmployee(data);
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        this.errorMessage = getApiErrorMessage(error, 'Could not load employee details.');
        this.toastService.error(this.errorMessage);
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  private mergeAssignedEmployeeOptions(data: EmployeeEditResponse): void {
    this.skills = this.mergeById(
      this.skills ?? [],
      (data.skills ?? []).map((skill) => ({
        skillId: skill.skillId,
        skillName: skill.skillName,
        isActive: false
      })),
      'skillId'
    );
    this.dressSizes = this.mergeById(this.dressSizes ?? [], data.dressSizes ?? [], 'dressId');

    this.departments = this.withCurrentOption(
      this.departments, 'departmentId', data.departmentId, 'departmentName', data.departmentName);
    this.designations = this.withCurrentOption(
      this.designations, 'designationId', data.designationId, 'designationName', data.designationName);
    this.jobTypes = this.withCurrentOption(
      this.jobTypes, 'jobTypeId', data.jobTypeId, 'jobTypeName', data.jobTypeName);
    this.bloodGroups = this.withCurrentOption(
      this.bloodGroups, 'bloodGroupId', data.bloodGroupId, 'bloodGroupName', data.bloodGroupName);
    this.genders = this.withCurrentOption(
      this.genders, 'genderId', data.genderId, 'genderName', data.genderName);
    this.teams = this.withCurrentOption(
      this.teams, 'teamId', data.teamId, 'teamName', data.teamName);
    this.recruitments = this.withCurrentOption(
      this.recruitments, 'recruitmentId', data.recruitmentId, 'recruitmentName', data.recruitmentName);
    this.employees = this.withCurrentOption(
      this.withCurrentOption(
        this.employees, 'reportingManagerId', data.reportsToEmployeeId, 'reportingManagerName', data.reportsToEmployeeName),
      'reportingManagerId', data.leadEmployeeId, 'reportingManagerName', data.leadEmployeeName);
    this.cotivityEmployees = this.withCurrentOption(
      this.cotivityEmployees, 'reportingManagerId', data.cotivityEmployeeId, 'reportingManagerName', data.cotivityEmployeeName);

    const primaryAddress = (data.addresses ?? [])[0];
    this.addressTypes = this.withCurrentOption(
      this.addressTypes, 'addressTypeId', primaryAddress?.addressTypeId, 'addressTypeName', primaryAddress?.addressTypeName);
  }

  private withCurrentOption(
    options: any[],
    idField: string,
    id: number | null | undefined,
    nameField: string,
    name: string | null | undefined
  ): any[] {
    const list = options ?? [];
    const optionId = Number(id);
    const optionName = name?.trim();
    if (!Number.isInteger(optionId) || optionId <= 0 || !optionName) {
      return list;
    }
    if (list.some((option) => Number(option[idField]) === optionId)) {
      return list;
    }

    return [...list, { [idField]: optionId, [nameField]: optionName }];
  }

  private patchEmployee(data: EmployeeEditResponse | EmployeeProfileEmployee): void {
    const joinedDate = this.parseApiDate(data.joinedDate);
    const dateOfBirth = data.dateOfBirth ? this.parseApiDate(data.dateOfBirth) : null;
    const addresses = data.addresses ?? [];
    const skills = data.skills ?? [];
    const dressSizes = data.dressSizes ?? [];
    if (!joinedDate || (data.dateOfBirth && !dateOfBirth)) {
      this.errorMessage = 'The employee API returned an invalid date.';
      this.toastService.error(this.errorMessage);
      return;
    }
    this.employeeForm.patchValue({
      eNumber: data.eNumber,
      firstName: this.normalizeFormText(data.firstName),
      middleName: this.normalizeFormText(data.middleName),
      lastName: this.normalizeFormText(data.lastName),
      joinedDate,
      joinedDateBS: data.joinedDateBS || this.formatNepaliDate(joinedDate),
      email: this.normalizeFormText(data.email),
      alternateEmail: this.normalizeFormText(data.alternateEmail),
      phoneNumber: this.normalizeFormText(data.phoneNumber),
      mobileNumber: this.normalizeFormText(data.mobileNumber),
      emergencyContactNumber: this.normalizeFormText(data.emergencyContactNumber),
      emergencyContactPerson: this.normalizeFormText(data.emergencyContactPerson),
      fatherName: this.normalizeFormText(data.fatherName),
      motherName: this.normalizeFormText(data.motherName),
      spouseName: this.normalizeFormText(data.spouseName),
      extensionNumber: data.extensionNumber,
      citizenshipNumber: this.normalizeFormText(data.citizenshipNumber),
      panNumber: this.normalizeFormText(data.panNumber),
      lockerNumber: this.normalizeFormText(data.lockerNumber),
      stationNumber: this.normalizeFormText(data.stationNumber),
      dateOfBirth,
      dateOfBirthBS: data.dateOfBirthBS || (dateOfBirth ? this.formatNepaliDate(dateOfBirth) : ''),
      departmentId: this.normalizeFormId(data.departmentId),
      designationId: this.normalizeFormId(data.designationId),
      jobTypeId: this.normalizeFormId(data.jobTypeId),
      bloodGroupId: this.normalizeFormId(data.bloodGroupId),
      genderId: this.normalizeFormId(data.genderId),
      roleIds: (data.roleIds ?? []).map(String),
      teamId: this.normalizeFormId(data.teamId),
      recruitmentId: data.recruitmentId ? String(data.recruitmentId) : '',
      leadEmployeeId: data.leadEmployeeId ? String(data.leadEmployeeId) : '',
      reportToEmployeeId: data.reportsToEmployeeId ? String(data.reportsToEmployeeId) : '',
      cotivityEmployeeId: data.cotivityEmployeeId ? String(data.cotivityEmployeeId) : '',
      skillIds: skills.map((skill) => String(skill.skillId)),
      dressIds: dressSizes.map((dress) => String(dress.dressId)),
      address: this.normalizeFormText(addresses[0]?.addressLine1),
      addressLine2: this.normalizeFormText(addresses[0]?.addressLine2),
      city: this.normalizeFormText(addresses[0]?.city),
      state: this.normalizeFormText(addresses[0]?.state),
      addressTypeId: addresses[0]?.addressTypeId ? String(addresses[0].addressTypeId) : '',
      isActive: data.isActive
    }, { emitEvent: false });
    this.skillLevels = Object.fromEntries(skills.map((skill) => [skill.skillId, skill.skillLevel]));
    this.newSkillLevel = skills[0]?.skillLevel ?? 1;
    this.currentImageBase64 = data.imageBase64;
  }

  private loadMyProfile(): void {
    forkJoin({
      profile: this.profileService.getMyProfile(),
      masters: this.employeeService.getEmployeeMasters()
    }).subscribe({
      next: ({ profile, masters: response }) => {
        const masters = response.data;
        this.profileData = profile;
        this.bloodGroups = profile.bloodGroups;
        const employee = profile.employee;
        const skillOptions = masters.skills ?? profile.skillOptions;
        const dressSizeOptions = masters.dressSizes ?? profile.dressSizeOptions;
        this.skills = this.mergeById(
          skillOptions,
          employee.skills.map((skill) => ({
            skillId: skill.skillId,
            skillName: skill.skillName,
            isActive: false
          })),
          'skillId'
        );
        this.dressSizes = this.mergeById(dressSizeOptions, employee.dressSizes, 'dressId');
        this.departments = employee.departmentName
          ? [{ departmentId: employee.departmentId, departmentName: employee.departmentName }]
          : [];
        this.designations = employee.designationName
          ? [{ designationId: employee.designationId, designationName: employee.designationName }]
          : [];
        this.jobTypes = employee.jobTypeName
          ? [{ jobTypeId: employee.jobTypeId, jobTypeName: employee.jobTypeName }]
          : [];
        this.genders = employee.genderName
          ? [{ genderId: employee.genderId, genderName: employee.genderName }]
          : [];
        this.teams = employee.teamName
          ? [{ teamId: employee.teamId, teamName: employee.teamName }]
          : [];
        this.recruitments = employee.recruitmentName && employee.recruitmentId
          ? [{ recruitmentId: employee.recruitmentId, recruitmentName: employee.recruitmentName }]
          : [];
        this.employees = [
          ...(employee.leadEmployeeId && employee.leadEmployeeName
            ? [{ reportingManagerId: employee.leadEmployeeId, reportingManagerName: employee.leadEmployeeName }]
            : []),
          ...(employee.reportsToEmployeeId && employee.reportsToEmployeeName
            ? [{ reportingManagerId: employee.reportsToEmployeeId, reportingManagerName: employee.reportsToEmployeeName }]
            : [])
        ].filter((item, index, list) =>
          list.findIndex((candidate) => candidate.reportingManagerId === item.reportingManagerId) === index);
        this.cotivityEmployees = employee.cotivityEmployeeId && employee.cotivityEmployeeName
          ? [{ reportingManagerId: employee.cotivityEmployeeId, reportingManagerName: employee.cotivityEmployeeName }]
          : [];
        const assignedRoleIds = new Set([
          ...(employee.roleIds ?? []),
          ...(profile.roles ?? []).map((role) => role.roleId)
        ]);
        const assignedRoleOptions = (masters.roles ?? []).filter((role: { roleId: number }) =>
          assignedRoleIds.has(role.roleId));
        this.roles = this.mergeById(assignedRoleOptions, profile.roles ?? [], 'roleId');
        this.addressTypes = employee.addresses.length
          ? [{ addressTypeId: employee.addresses[0].addressTypeId, addressTypeName: employee.addresses[0].addressTypeName }]
          : [];
        this.skillLevels = Object.fromEntries(employee.skills.map((skill) => [skill.skillId, skill.skillLevel]));
        this.patchEmployee(profile.employee);
        const primaryAddress = profile.employee.addresses.find((address) => address.isPrimary)
          ?? profile.employee.addresses[0]
          ?? null;
        this.employeeForm.patchValue({
          latitude: primaryAddress?.latitude ?? null,
          longitude: primaryAddress?.longitude ?? null
        }, { emitEvent: false });
        this.setProfileModeControlState();
        this.changeDetectorRef.markForCheck();
      },
      error: (error: unknown) => {
        this.errorMessage = getApiErrorMessage(error, 'Could not load your profile and employee options.');
        this.toastService.error(this.errorMessage);
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  private mergeById<T extends object>(
    primary: T[],
    additional: T[],
    idField: keyof T
  ): T[] {
    const options = new Map<unknown, T>();
    for (const option of [...primary, ...additional]) {
      options.set(option[idField], option);
    }
    return [...options.values()];
  }

  private setProfileModeControlState(): void {
    const editableFields = new Set([
      'alternateEmail',
      'bloodGroupId',
      'mobileNumber',
      'emergencyContactNumber',
      'motherName',
      'spouseName',
      'latitude',
      'longitude',
      'skillIds',
      'dressIds'
    ]);

    for (const controlName of Object.keys(this.employeeForm.controls)) {
      const control = this.employeeForm.get(controlName);
      if (editableFields.has(controlName)) {
        control?.enable({ emitEvent: false });
      } else {
        control?.disable({ emitEvent: false });
      }
    }
    this.employeeForm.get('mobileNumber')?.setValidators([
      Validators.maxLength(20),
      Validators.pattern(/^\+?[0-9][0-9\s().-]{5,19}$/)
    ]);
    this.employeeForm.get('emergencyContactNumber')?.setValidators([
      Validators.maxLength(20),
      Validators.pattern(/^\+?[0-9][0-9\s().-]{5,19}$/)
    ]);
    this.employeeForm.updateValueAndValidity({ emitEvent: false });
  }

  loadMasters(employeeId?: number): void {
    this.employeeService.getEmployeeMasters().subscribe({
      next: (response: any) => {

        const data = response.data;

        this.departments = data.departments;
        this.designations = data.designations;
        this.jobTypes = data.jobTypes;
        this.bloodGroups = data.bloodGroups;
        this.genders = data.genders;
        this.roles = data.roles;
        this.teams = data.teams;
        this.skills = data.skills;
        this.recruitments = data.recruitments;
        this.employees = data.employees;
        this.cotivityEmployees = data.cotivityEmployees;
        this.dressSizes = data.dressSizes;
        this.businessUnits = data.businesses;
        this.addressTypes = data.addressTypes;
        if (employeeId !== undefined) {
          this.loadEmployee(employeeId);
        }
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        this.errorMessage = getApiErrorMessage(error, 'Could not load employee form options.');
        this.toastService.error(this.errorMessage);
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  private parseApiDate(value: string): Date | null {
    const date = new Date(`${value.slice(0, 10)}T00:00:00`);
    return Number.isNaN(date.getTime()) ? null : date;
  }

  goBack(): void {
    this.router.navigate(['/people']);
  }

  onSubmit(): void {
    this.errorMessage = '';
    if (this.isSubmitting) {
      return;
    }

    if (this.isProfileMode) {
      this.saveMyProfile();
      return;
    }

    const value = this.employeeForm.getRawValue();
    this.employeeForm.markAllAsTouched();
    const invalidMessages = Object.keys(this.employeeForm.controls)
      .map((controlName) => this.validationMessage(controlName))
      .filter((message): message is string => message !== null);
    if (invalidMessages.length > 0) {
      this.showValidationMessages(invalidMessages);
      return;
    }

    const hasAddress = [value.address, value.addressLine2, value.city, value.state, value.addressTypeId]
      .some((item) => item !== null && item !== undefined && item !== '');

    if (hasAddress && (!value.address || !value.addressTypeId || !value.city || !value.state)) {
      this.errorMessage = 'Complete the address line, address type, city, and state, or leave the address blank.';
      this.toastService.error(this.errorMessage);
      return;
    }

    const request: CreateEmployeeRequest = {
      eNumber: Number(value.eNumber),
      firstName: value.firstName.trim(),
      middleName: value.middleName?.trim() || null,
      lastName: value.lastName.trim(),
      joinedDate: this.toDateOnly(value.joinedDate),
      joinedDateBS: value.joinedDateBS,
      email: value.email.trim(),
      alternateEmail: value.alternateEmail?.trim() || null,
      phoneNumber: value.phoneNumber?.trim() || null,
      mobileNumber: value.mobileNumber?.trim() || null,
      emergencyContactNumber: value.emergencyContactNumber?.trim() || null,
      emergencyContactPerson: value.emergencyContactPerson.trim(),
      fatherName: value.fatherName.trim(),
      motherName: value.motherName?.trim() || null,
      spouseName: value.spouseName?.trim() || null,
      designationId: Number(value.designationId),
      departmentId: Number(value.departmentId),
      jobTypeId: Number(value.jobTypeId),
      bloodGroupId: Number(value.bloodGroupId),
      genderId: Number(value.genderId),
      reportsToEmployeeId: this.toNullablePositiveId(value.reportToEmployeeId),
      leadEmployeeId: this.toNullablePositiveId(value.leadEmployeeId),
      stationNumber: value.stationNumber?.trim() || null,
      recruitmentId: value.recruitmentId ? Number(value.recruitmentId) : null,
      extensionNumber: Number(value.extensionNumber),
      citizenshipNumber: value.citizenshipNumber.trim(),
      dateOfBirth: value.dateOfBirth ? this.toDateOnly(value.dateOfBirth) : null,
      dateOfBirthBS: value.dateOfBirthBS || null,
      panNumber: value.panNumber.trim(),
      lockerNumber: value.lockerNumber?.trim() || null,
      roleIds: (value.roleIds as Array<number | string>).map(Number),
      teamId: Number(value.teamId),
      cotivityEmployeeId: this.toNullablePositiveId(value.cotivityEmployeeId),
      imageBase64: this.imageBase64,
      isActive: value.isActive,
      addresses: hasAddress ? [{
        addressTypeId: Number(value.addressTypeId),
        addressLine1: value.address.trim(),
        addressLine2: value.addressLine2?.trim() || null,
        city: value.city.trim(),
        state: value.state.trim(),
        latitude: 0,
        longitude: 0,
        isPrimary: true
      }] : [],
      skills: (value.skillIds as Array<number | string>).map((skillId) => ({
        skillId: Number(skillId),
        skillLevel: this.skillLevelFor(Number(skillId)),
        isPrimary: false
      })),
      dressSizes: this.dressSizes
        .filter((dress) => (value.dressIds as Array<number | string>).map(Number).includes(Number(dress.dressId)))
        .map((dress) => ({
          dressId: Number(dress.dressId),
          dressName: dress.dressName,
          size: dress.size,
          isActive: dress.isActive
        }))
    };

    this.isSubmitting = true;
    const saveRequest = this.employeeId === null
      ? this.employeeService.createEmployee(request).pipe(map(() => {}))
      : this.employeeService.updateActiveInactiveEmployee(this.employeeId, request);
    saveRequest.subscribe({
      next: () => {
        this.toastService.success(`Employee ${this.employeeId === null ? 'created' : 'updated'} successfully.`);
        this.router.navigate(['/people']);
      },
      error: (error) => {
        this.errorMessage = getApiErrorMessage(error, 'Could not save the employee. Please try again.');
        this.toastService.error(this.errorMessage);
        this.isSubmitting = false;
      }
    });
  }

  private saveMyProfile(): void {
    if (!this.profileData) {
      return;
    }

    const value = this.employeeForm.getRawValue();
    this.employeeForm.markAllAsTouched();
    const editableFields = [
      'alternateEmail',
      'bloodGroupId',
      'mobileNumber',
      'emergencyContactNumber',
      'motherName',
      'spouseName',
      'latitude',
      'longitude'
    ];
    const invalidMessages = editableFields
      .map((field) => this.validationMessage(field))
      .filter((message): message is string => message !== null);
    if (invalidMessages.length > 0) {
      this.showValidationMessages(invalidMessages);
      return;
    }

    const employee = this.profileData.employee;
    const primaryAddress = employee.addresses.find((address) => address.isPrimary)
      ?? employee.addresses[0]
      ?? null;
    const patch: EmployeeProfilePatch = {};
    if (value.alternateEmail !== (employee.alternateEmail ?? '')) {
      patch.alternateEmail = value.alternateEmail?.trim() || null;
    }
    if (Number(value.bloodGroupId) !== employee.bloodGroupId) {
      patch.bloodGroupId = Number(value.bloodGroupId);
    }
    if (value.mobileNumber !== (employee.mobileNumber ?? '')) {
      patch.mobileNumber = value.mobileNumber?.trim() || null;
    }
    if (value.emergencyContactNumber !== (employee.emergencyContactNumber ?? '')) {
      patch.emergencyContactNumber = value.emergencyContactNumber?.trim() || null;
    }
    if (value.motherName !== (employee.motherName ?? '')) {
      patch.motherName = value.motherName?.trim() || null;
    }
    if (value.spouseName !== (employee.spouseName ?? '')) {
      patch.spouseName = value.spouseName?.trim() || null;
    }
    if (value.latitude !== primaryAddress?.latitude && value.latitude !== null) {
      patch.latitude = value.latitude;
    }
    if (value.longitude !== primaryAddress?.longitude && value.longitude !== null) {
      patch.longitude = value.longitude;
    }
    const selectedSkillIds = (value.skillIds as Array<number | string> ?? []).map(Number);
    const currentSkillIds = employee.skills.map((skill) => skill.skillId);
    if (!this.sameIds(selectedSkillIds, currentSkillIds)
      || employee.skills.some((skill) =>
        selectedSkillIds.includes(skill.skillId) && this.skillLevelFor(skill.skillId) !== skill.skillLevel)) {
      patch.skills = selectedSkillIds.map((skillId) => {
        return {
          skillId,
          skillLevel: this.skillLevelFor(skillId)
        };
      });
    }
    const selectedDressIds = (value.dressIds as Array<number | string> ?? []).map(Number);
    const currentDressIds = employee.dressSizes.map((dress) => dress.dressId);
    if (!this.sameIds(selectedDressIds, currentDressIds)) {
      patch.dressIds = selectedDressIds;
    }
    if (this.imageBase64) {
      patch.imageBase64 = this.imageBase64;
    }
    if (Object.keys(patch).length === 0) {
      return;
    }

    this.isSubmitting = true;
    this.profileService.updateMyProfile(patch).subscribe({
      next: (profile) => {
        this.profileData = profile;
        this.bloodGroups = profile.bloodGroups;
        this.skills = profile.skillOptions;
        this.dressSizes = profile.dressSizeOptions;
        this.imageBase64 = null;
        this.imageFileName = '';
        this.patchEmployee(profile.employee);
        const updatedAddress = profile.employee.addresses.find((address) => address.isPrimary)
          ?? profile.employee.addresses[0]
          ?? null;
        this.employeeForm.patchValue({
          latitude: updatedAddress?.latitude ?? null,
          longitude: updatedAddress?.longitude ?? null
        }, { emitEvent: false });
        this.setProfileModeControlState();
        this.employeeForm.markAsPristine();
        this.isSubmitting = false;
        this.toastService.success('Profile updated successfully.');
        this.changeDetectorRef.markForCheck();
      },
      error: (error: unknown) => {
        this.errorMessage = getApiErrorMessage(error, 'Could not update your profile.');
        this.toastService.error(this.errorMessage);
        this.isSubmitting = false;
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  validationMessage(controlName: string): string | null {
    const control = this.employeeForm.get(controlName);
    if (!control?.touched || !control.errors) {
      return null;
    }

    const labels: Record<string, string> = {
      eNumber: 'Employee number',
      firstName: 'First name',
      middleName: 'Middle name',
      lastName: 'Last name',
      email: 'Email',
      alternateEmail: 'Alternate email',
      extensionNumber: 'Extension number',
      citizenshipNumber: 'Citizenship number',
      panNumber: 'PAN number',
      lockerNumber: 'Locker number',
      stationNumber: 'Station number',
      joinedDate: 'Date of joining',
      dateOfBirth: 'Date of birth',
      joinedDateBS: 'Joined date (B.S.)',
      dateOfBirthBS: 'Date of birth (B.S.)',
      phoneNumber: 'Phone number',
      mobileNumber: 'Mobile number',
      latitude: 'Latitude',
      longitude: 'Longitude',
      emergencyContactNumber: 'Emergency contact number',
      emergencyContactPerson: 'Emergency contact person',
      fatherName: 'Father name',
      motherName: 'Mother name',
      spouseName: 'Spouse name',
      address: 'Address line 1',
      addressLine2: 'Address line 2',
      city: 'City',
      state: 'State',
      departmentId: 'Department',
      designationId: 'Designation',
      jobTypeId: 'Job type',
      bloodGroupId: 'Blood group',
      genderId: 'Gender',
      roleId: 'Role',
      teamId: 'Team'
    };
    const label = labels[controlName] ?? controlName;
    const errors = control.errors;

    if (errors['required']) return `${label} is required.`;
    if (errors['email']) return `${label} must be a valid email address.`;
    if (errors['maxlength']) return `${label} must be ${errors['maxlength'].requiredLength} characters or fewer.`;
    if (errors['min']) return `${label} must be at least ${errors['min'].min}.`;
    if (errors['max']) return `${label} must be ${errors['max'].max} or less.`;
    if (errors['pattern']) return `${label} must be a whole number.`;
    if (errors['matDatepickerMin']) return `${label} must be today or later.`;
    if (errors['matDatepickerMax']) return `${label} must be before today.`;
    return `${label} is invalid.`;
  }

  normalizeNullInput(event: Event, controlName: string): void {
    const input = event.target;
    if (!(input instanceof HTMLInputElement || input instanceof HTMLTextAreaElement) ||
        (input instanceof HTMLInputElement && (input.type === 'checkbox' || input.type === 'file'))) {
      return;
    }

    if (input.value.trim().toLowerCase() !== 'null') {
      return;
    }

    input.value = '';
    this.employeeForm.get(controlName)?.setValue(
      input instanceof HTMLInputElement && input.type === 'number' ? null : '',
      { emitEvent: false }
    );
  }

  showFieldError(controlName: string): void {
    const control = this.employeeForm.get(controlName);
    if (!control) {
      return;
    }

    control.markAsTouched();
    const message = this.validationMessage(controlName!);
    if (message) {
      this.toastService.error(message);
    }
  }

  isSkillSelected(skillId: number): boolean {
    return (this.employeeForm.get('skillIds')?.value as Array<number | string> ?? [])
      .some((selectedId) => Number(selectedId) === skillId);
  }

  get selectedSkills(): Array<{ skillId: number; skillName: string; skillLevel: number }> {
    const selectedIds = this.selectedIds('skillIds');
    return (this.skills ?? [])
      .filter((skill) => selectedIds.includes(Number(skill.skillId)))
      .map((skill) => ({
        skillId: Number(skill.skillId),
        skillName: skill.skillName,
        skillLevel: this.skillLevelFor(Number(skill.skillId))
      }));
  }

  get availableSkills(): Array<{ skillId: number; skillName: string }> {
    const selectedIds = this.selectedIds('skillIds');
    return (this.skills ?? [])
      .filter((skill) => !selectedIds.includes(Number(skill.skillId)))
      .map((skill) => ({ skillId: Number(skill.skillId), skillName: skill.skillName }));
  }

  addSkill(event: Event): void {
    const skillId = this.consumeOptionId(event);
    if (skillId === null) {
      return;
    }

    this.toggleSelectedId('skillIds', skillId, true);
    if (this.skillLevels[skillId] === undefined) {
      this.skillLevels[skillId] = this.newSkillLevel;
    }
  }

  toggleSkillLevelMenu(skillId: number, event: Event): void {
    event.stopPropagation();
    this.levelRowOpen = false;
    this.openSkillLevelId = this.openSkillLevelId === skillId ? null : skillId;
  }

  chooseSkillLevel(skillId: number, level: number, event: Event): void {
    event.stopPropagation();
    this.openSkillLevelId = null;
    this.skillLevels[skillId] = level;
    this.newSkillLevel = level;
    this.employeeForm.get('skillIds')?.markAsDirty();
  }

  toggleLevelRow(event: Event): void {
    event.stopPropagation();
    this.openSkillLevelId = null;
    this.levelRowOpen = !this.levelRowOpen;
  }

  chooseLevelRow(level: number, event: Event): void {
    event.stopPropagation();
    this.levelRowOpen = false;
    this.updateNewSkillLevel(level);
  }

  updateNewSkillLevel(level: number): void {
    this.newSkillLevel = Number.isInteger(level) && level >= 1 && level <= 5 ? level : 1;

    const selectedSkillIds = this.selectedIds('skillIds');
    if (!selectedSkillIds.length) {
      return;
    }

    selectedSkillIds.forEach((skillId) => {
      this.skillLevels[skillId] = this.newSkillLevel;
    });
    this.employeeForm.get('skillIds')?.markAsDirty();
  }

  removeSkill(skillId: number): void {
    this.toggleSelectedId('skillIds', skillId, false);
    const { [skillId]: removed, ...remaining } = this.skillLevels;
    void removed;
    this.skillLevels = remaining;
  }

  skillLevelFor(skillId: number): number {
    return this.skillLevels[skillId] ?? 1;
  }

  isDressSelected(dressId: number): boolean {
    return (this.employeeForm.get('dressIds')?.value as Array<number | string> ?? [])
      .some((selectedId) => Number(selectedId) === dressId);
  }

  get selectedDressSizes(): Array<{ dressId: number; dressName: string; size: string }> {
    const selectedIds = this.selectedIds('dressIds');
    return (this.dressSizes ?? [])
      .filter((dress) => selectedIds.includes(Number(dress.dressId)))
      .map((dress) => ({ dressId: Number(dress.dressId), dressName: dress.dressName, size: dress.size }));
  }

  get availableDressSizes(): Array<{ dressId: number; dressName: string; size: string }> {
    const selectedIds = this.selectedIds('dressIds');
    return (this.dressSizes ?? [])
      .filter((dress) => !selectedIds.includes(Number(dress.dressId)))
      .map((dress) => ({ dressId: Number(dress.dressId), dressName: dress.dressName, size: dress.size }));
  }

  addDress(event: Event): void {
    const dressId = this.consumeOptionId(event);
    if (dressId === null) {
      return;
    }

    this.toggleSelectedId('dressIds', dressId, true);
  }

  removeDress(dressId: number): void {
    this.toggleSelectedId('dressIds', dressId, false);
  }

  get selectedRoles(): Array<{ roleId: number; roleName: string }> {
    const selectedIds = this.selectedIds('roleIds');
    return (this.roles ?? [])
      .filter((role) => selectedIds.includes(Number(role.roleId)))
      .map((role) => ({ roleId: Number(role.roleId), roleName: role.roleName }));
  }

  get availableRoles(): Array<{ roleId: number; roleName: string }> {
    const selectedIds = this.selectedIds('roleIds');
    return (this.roles ?? [])
      .filter((role) => !selectedIds.includes(Number(role.roleId)))
      .map((role) => ({ roleId: Number(role.roleId), roleName: role.roleName }));
  }

  addRole(event: Event): void {
    const roleId = this.consumeOptionId(event);
    if (roleId === null) {
      return;
    }

    this.toggleSelectedId('roleIds', roleId, true);
  }

  removeRole(roleId: number): void {
    this.toggleSelectedId('roleIds', roleId, false);
  }

  private selectedIds(controlName: string): number[] {
    return (this.employeeForm?.get(controlName)?.value as Array<number | string> ?? []).map(Number);
  }

  private consumeOptionId(event: Event): number | null {
    const select = event.target as HTMLSelectElement;
    const id = Number(select.value);
    select.value = '';
    return Number.isInteger(id) && id > 0 ? id : null;
  }

  private toggleSelectedId(controlName: string, id: number, selected: boolean): void {
    const control = this.employeeForm.get(controlName);
    const currentIds = (control?.value as Array<number | string> ?? []).map(Number);
    const nextIds = selected
      ? [...new Set([...currentIds, id])]
      : currentIds.filter((currentId) => currentId !== id);
    control?.setValue(nextIds.map(String));
    control?.markAsDirty();
  }

  private sameIds(first: number[], second: number[]): boolean {
    if (first.length !== second.length) {
      return false;
    }

    const sortedFirst = [...first].sort((a, b) => a - b);
    const sortedSecond = [...second].sort((a, b) => a - b);
    return sortedFirst.every((id, index) => id === sortedSecond[index]);
  }

  private showValidationMessages(messages: string[]): void {
    this.toastService.error([...new Set(messages)].join(' '));
  }

  private normalizeFormText(value: string | null | undefined): string {
    return value?.trim().toLowerCase() === 'null' ? '' : value ?? '';
  }

  private normalizeFormId(value: number | null | undefined): string {
    return value === null || value === undefined ? '' : String(value);
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    const supportedTypes = ['image/jpeg', 'image/png', 'image/gif'];
    if (!supportedTypes.includes(file.type)) {
      this.toastService.error('Choose a JPEG, PNG, or GIF image.');
      input.value = '';
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      this.toastService.error('Choose an image smaller than 5 MB.');
      input.value = '';
      return;
    }

    this.isReadingImage = true;
    const reader = new FileReader();
    reader.onload = () => {
      this.isReadingImage = false;
      if (typeof reader.result !== 'string') {
        this.toastService.error('Could not read the selected image.');
        this.changeDetectorRef.markForCheck();
        return;
      }

      this.imageBase64 = reader.result;
      this.imageFileName = file.name;
      this.changeDetectorRef.markForCheck();
    };
    reader.onerror = () => {
      this.isReadingImage = false;
      this.toastService.error('Could not read the selected image.');
      this.changeDetectorRef.markForCheck();
    };
    reader.readAsDataURL(file);
  }

  clearImage(input: HTMLInputElement): void {
    this.imageBase64 = null;
    this.imageFileName = '';
    input.value = '';
  }

  private toNullablePositiveId(value: unknown): number | null {
    if (value === null || value === undefined || value === '') {
      return null;
    }

    const id = Number(value);
    return Number.isInteger(id) && id > 0 ? id : null;
  }

  private toDateOnly(value: Date | string): string {
    const date = value instanceof Date ? value : new Date(value);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private watchDatePair(adControlName: string, bsControlName: string): void {
    const adControl = this.employeeForm.get(adControlName);
    const bsControl = this.employeeForm.get(bsControlName);
    if (!adControl || !bsControl) return;

    adControl.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((value: Date | null) => {
      if (!value) {
        bsControl.setValue('', { emitEvent: false });
        return;
      }
      bsControl.setValue(this.formatNepaliDate(value), { emitEvent: false });
    });

    bsControl.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((value: string | null) => {
      if (!value) {
        adControl.setValue(null, { emitEvent: false });
        return;
      }
      if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return;
      try {
        if (!this.nepaliDate) return;
        const nepaliDate = new this.nepaliDate(value);
        if (nepaliDate.format('YYYY-MM-DD') !== value) return;
        const adDate = nepaliDate.toJsDate();
        if (!Number.isNaN(adDate.getTime())) {
          adControl.setValue(adDate, { emitEvent: false });
        }
      } catch {
        return;
      }
    });
  }

  private formatNepaliDate(value: Date): string {
    if (!this.nepaliDate) return '';
    try {
      return new this.nepaliDate(value).format('YYYY-MM-DD');
    } catch {
      return '';
    }
  }

  private readonly validateNepaliDate = (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) return null;
    if (!this.nepaliDate) return { conversionUnavailable: true };
    try {
      const inputDate = String(control.value);
      const nepaliDate = new this.nepaliDate(inputDate);
      const convertedDate = nepaliDate.toJsDate();
      return nepaliDate.format('YYYY-MM-DD') !== inputDate || Number.isNaN(convertedDate.getTime())
        ? { invalidNepaliDate: true }
        : null;
    } catch {
      return { invalidNepaliDate: true };
    }
  };

  private loadNepaliDateConverter(): void {
    import('nepali-date-converter')
      .then(({ default: NepaliDate }) => {
        this.nepaliDate = NepaliDate;
        this.synchronizeDatePairAfterLoad('joinedDate', 'joinedDateBS');
        this.synchronizeDatePairAfterLoad('dateOfBirth', 'dateOfBirthBS');
        this.changeDetectorRef.markForCheck();
      })
      .catch((error: unknown) => {
        console.error('Could not load Nepali date conversion support.', error);
        this.toastService.error('Could not load Nepali date conversion support.');
      });
  }

  private synchronizeDatePairAfterLoad(adControlName: string, bsControlName: string): void {
    const adControl = this.employeeForm.get(adControlName);
    const bsControl = this.employeeForm.get(bsControlName);
    if (!adControl || !bsControl) return;

    const bsValue = String(bsControl.value ?? '').trim();
    const adValue = adControl.value as Date | null;
    if (bsValue) {
      try {
        const nepaliDate = new this.nepaliDate!(bsValue);
        const convertedDate = nepaliDate.toJsDate();
        if (nepaliDate.format('YYYY-MM-DD') === bsValue && !Number.isNaN(convertedDate.getTime())) {
          adControl.setValue(convertedDate, { emitEvent: false });
        } else {
          bsControl.setValue(adValue ? this.formatNepaliDate(adValue) : '', { emitEvent: false });
        }
      } catch {
        bsControl.setValue(adValue ? this.formatNepaliDate(adValue) : '', { emitEvent: false });
      }
    } else if (adValue) {
      bsControl.setValue(this.formatNepaliDate(adValue), { emitEvent: false });
    }
    bsControl.updateValueAndValidity();
  }
}
