import { ChangeDetectorRef, Component, DestroyRef, OnInit, inject } from '@angular/core';
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
import { map } from 'rxjs';
type NepaliDateConstructor = typeof import('nepali-date-converter').default;
import { ToastService } from '../../../../shared/services/toast.service';
import { getApiErrorMessage } from '../../../../shared/services/api-error-message';

import { CreateEmployeeRequest, EmployeeEditResponse, EmployeeService } from '../../services/employee.service';
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

  ngOnInit(): void {
    this.initializeForm();
    this.loadNepaliDateConverter();
    const employeeId = Number(this.route.snapshot.paramMap.get('employeeId'));
    if (Number.isInteger(employeeId) && employeeId > 0) {
      this.employeeId = employeeId;
    }
    this.loadMasters(this.employeeId ?? undefined);
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
    this.employeeService.getEmployeeById(employeeId).subscribe({
      next: ({ data }) => {
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

  private patchEmployee(data: EmployeeEditResponse): void {
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
    this.currentImageBase64 = data.imageBase64;
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
      : this.employeeService.updateEmployee(this.employeeId, request);
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

  toggleSkill(skillId: number, event: Event): void {
    const selected = (event.target as HTMLInputElement).checked;
    this.toggleSelectedId('skillIds', skillId, selected);
    if (selected && this.skillLevels[skillId] === undefined) {
      this.skillLevels[skillId] = 1;
    } else if (!selected) {
      const { [skillId]: removed, ...remaining } = this.skillLevels;
      void removed;
      this.skillLevels = remaining;
    }
  }

  skillLevelFor(skillId: number): number {
    return this.skillLevels[skillId] ?? 1;
  }

  updateSkillLevel(skillId: number, event: Event): void {
    this.skillLevels[skillId] = Number((event.target as HTMLSelectElement).value);
  }

  isDressSelected(dressId: number): boolean {
    return (this.employeeForm.get('dressIds')?.value as Array<number | string> ?? [])
      .some((selectedId) => Number(selectedId) === dressId);
  }

  isRoleSelected(roleId: number): boolean {
    return (this.employeeForm.get('roleIds')?.value as Array<number | string> ?? [])
      .some((selectedId) => Number(selectedId) === roleId);
  }

  toggleRole(roleId: number, event: Event): void {
    this.toggleSelectedId('roleIds', roleId, (event.target as HTMLInputElement).checked);
  }

  toggleDress(dressId: number, event: Event): void {
    this.toggleSelectedId('dressIds', dressId, (event.target as HTMLInputElement).checked);
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