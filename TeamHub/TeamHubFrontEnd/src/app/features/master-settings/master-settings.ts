import { HttpClient, HttpParams } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { AfterViewInit, ChangeDetectorRef, Component, DestroyRef, ElementRef, inject, OnInit, ViewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { TopbarComponent } from '../../shared/components/topbar/topbar.component';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';
import { PagedResult } from '../../shared/models/paged-result.model';
import { ToastService } from '../../shared/services/toast.service';
import { getApiErrorMessage } from '../../shared/services/api-error-message';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/auth/auth.service';

type MasterKey =
  | 'department'
  | 'designation'
  | 'business-unit'
  | 'dress-size'
  | 'cotivity-department'
  | 'cotivity-designation'
  | 'cotivity-employee'
  | 'role'
  | 'permission'
  | 'address-type'
  | 'blood-group'
  | 'skills'
  | 'job-type'
  | 'team'
  | 'gender';

interface MasterField {
  key: string;
  label: string;
  type: 'text' | 'number' | 'checkbox' | 'select';
  required?: boolean;
  minLength?: number;
  maxLength?: number;
  min?: number;
  max?: number;
  integer?: boolean;
  displayKey?: string;
  optionsSource?: 'cotivityDepartments' | 'cotivityDesignations' | 'cotivityEmployees';
}

interface MasterConfig {
  title: string;
  singular: string;
  endpoint: string;
  idField: string;
  fields: MasterField[];
}

interface ApiResponse<T> {
  data: T;
  message: string;
  success: boolean;
}

interface LookupItem {
  id: number;
  name: string;
}

const nameAndActive = (key: string, label: string, maxLength = 100, minLength?: number): MasterField[] => [
  { key, label, type: 'text', minLength, maxLength },
  { key: 'isActive', label: 'Active', type: 'checkbox' }
];

const masterConfigs: Record<MasterKey, MasterConfig> = {
  department: {
    title: 'Departments', singular: 'Department', endpoint: 'departments', idField: 'departmentId',
    fields: nameAndActive('departmentName', 'Department name', 100, 2)
  },
  designation: {
    title: 'Designations', singular: 'Designation', endpoint: 'designations', idField: 'designationId',
    fields: [
      { key: 'designationName', label: 'Designation name', type: 'text', minLength: 2, maxLength: 100 },
      { key: 'rank', label: 'Rank', type: 'number', min: 0, max: 2147483647, integer: true },
      { key: 'isActive', label: 'Active', type: 'checkbox' }
    ]
  },
  'business-unit': {
    title: 'Business Units', singular: 'Business unit', endpoint: 'business-units', idField: 'businessUnitId',
    fields: nameAndActive('businessUnitName', 'Business unit name', 150)
  },
  'dress-size': {
    title: 'Dress Sizes', singular: 'Dress size', endpoint: 'dress-sizes', idField: 'dressId',
    fields: [
      { key: 'dressName', label: 'Dress name', type: 'text', maxLength: 100 },
      { key: 'size', label: 'Size', type: 'text', maxLength: 10 },
      { key: 'isActive', label: 'Active', type: 'checkbox' }
    ]
  },
  'cotivity-department': {
    title: 'Cotivity Departments', singular: 'Cotivity department', endpoint: 'cotivitydepartments', idField: 'cotivityDepartmentId',
    fields: nameAndActive('cotivityDepartmentName', 'Cotivity department name', 100, 2)
  },
  'cotivity-designation': {
    title: 'Cotivity Designations', singular: 'Cotivity designation', endpoint: 'cotivitydesignations', idField: 'cotivityDesignationId',
    fields: [
      { key: 'cotivityDesignationName', label: 'Cotivity designation name', type: 'text', minLength: 2, maxLength: 100 },
      { key: 'cotivityRank', label: 'Rank', type: 'number', min: 0, max: 2147483647, integer: true },
      { key: 'isActive', label: 'Active', type: 'checkbox' }
    ]
  },
  'cotivity-employee': {
    title: 'Cotivity Employees', singular: 'Cotivity employee', endpoint: 'cotivityemployees', idField: 'cotivityEmployeeId',
    fields: [
      { key: 'cotivityEmployeeName', label: 'Cotivity employee name', type: 'text', minLength: 2, maxLength: 150 },
      {
        key: 'cotivityDepartmentId', label: 'Cotivity department', type: 'select',
        displayKey: 'cotivityDepartmentName', optionsSource: 'cotivityDepartments'
      },
      {
        key: 'cotivityDepartmentDesignationId', label: 'Cotivity designation', type: 'select',
        displayKey: 'cotivityDepartmentDesignationName', optionsSource: 'cotivityDesignations'
      },
      {
        key: 'reportsToCotivityEmployeeId', label: 'Reports to Cotivity employee', type: 'select',
        required: false, optionsSource: 'cotivityEmployees', displayKey: 'reportsToCotivityEmployeeName'
      },
      { key: 'isActive', label: 'Active', type: 'checkbox' }
    ]
  },
  role: {
    title: 'Roles', singular: 'Role', endpoint: 'roles', idField: 'roleId',
    fields: nameAndActive('roleName', 'Role name', 100)
  },
  permission: {
    title: 'Permissions', singular: 'Permission', endpoint: 'permissions', idField: 'permissionId',
    fields: [
      { key: 'permissionCode', label: 'Permission code', type: 'text', minLength: 2, maxLength: 100 },
      { key: 'permissionName', label: 'Permission name', type: 'text', minLength: 2, maxLength: 150 },
      { key: 'module', label: 'Module', type: 'text', required: false, maxLength: 100 },
      { key: 'description', label: 'Description', type: 'text', required: false, maxLength: 500 },
      { key: 'isActive', label: 'Active', type: 'checkbox' }
    ]
  },
  'address-type': {
    title: 'Address Types', singular: 'Address type', endpoint: 'address-types', idField: 'addressTypeId',
    fields: nameAndActive('addressTypeName', 'Address type name', 100)
  },
  'blood-group': {
    title: 'Blood Groups', singular: 'Blood group', endpoint: 'bloodgroups', idField: 'bloodGroupId',
    fields: nameAndActive('bloodGroupName', 'Blood group name', 5)
  },
  skills: {
    title: 'Skills', singular: 'Skill', endpoint: 'skills', idField: 'skillId',
    fields: nameAndActive('skillName', 'Skill name', 100)
  },
  'job-type': {
    title: 'Job Types', singular: 'Job type', endpoint: 'job-types', idField: 'jobTypeId',
    fields: nameAndActive('jobTypeName', 'Job type name', 100)
  },
  team: {
    title: 'Teams', singular: 'Team', endpoint: 'teams', idField: 'teamId',
    fields: nameAndActive('teamName', 'Team name', 100)
  },
  gender: {
    title: 'Genders', singular: 'Gender', endpoint: 'genders', idField: 'genderId',
    fields: nameAndActive('genderName', 'Gender name', 50)
  }
};

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TopbarComponent, PaginationComponent],
  selector: 'app-master-settings',
  styleUrl: './master-settings.css',
  templateUrl: './master-settings.html'
})
export class MasterSettings implements OnInit, AfterViewInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  private readonly toastService = inject(ToastService);
  private readonly authService = inject(AuthService);
  private readonly apiBaseUrl = `${environment.apiUrl}api`;
  @ViewChild('masterForm') private masterForm?: ElementRef<HTMLFormElement>;

  config?: MasterConfig;
  fields: MasterField[] = [];
  rows: Record<string, unknown>[] = [];
  cotivityDepartments: LookupItem[] = [];
  cotivityDesignations: LookupItem[] = [];
  cotivityEmployees: LookupItem[] = [];
  pageNumber = 1;
  pageSize = 5;
  totalCount = 0;
  form: FormGroup = this.formBuilder.group({});
  editingId: number | null = null;
  isLoading = false;
  isSaving = false;
  isReadingImage = false;
  errorMessage = '';
  selectedImageBase64: string | null = null;
  currentImageBase64: string | null = null;
  imageFileName = '';
  private currentMasterKey: MasterKey | null = null;

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const rawKey = params.get('masterKey');
      const key = rawKey && Object.prototype.hasOwnProperty.call(masterConfigs, rawKey)
        ? rawKey as MasterKey
        : null;
      this.currentMasterKey = key;
      this.config = key ? masterConfigs[key] : undefined;
      this.fields = this.config?.fields ?? [];
      this.rows = [];
      this.pageNumber = 1;
      this.totalCount = 0;
      this.errorMessage = this.config ? '' : 'This master-data page is not available.';
      this.resetForm();
      setTimeout(() => this.normalizeRenderedNullInputs());
      if (this.config) {
        this.loadRows();
        if (key === 'cotivity-employee') {
          this.loadCotivityLookups();
        }
      }
    });
  }

  ngAfterViewInit(): void {
    this.normalizeRenderedNullInputs();
  }

  goBack(): void {
    this.router.navigate(['/administration']);
  }

  save(): void {
    if (!this.config || this.isSaving) {
      return;
    }

    const action = this.editingId === null ? 'create' : 'update';
    if (!this.hasMasterActionPermission(action)) {
      this.toastService.error(`You do not have permission to ${action} ${this.config.singular.toLowerCase()}.`);
      return;
    }

    this.normalizeNullFormValues();
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage = this.fields
        .map((field) => this.fieldError(field.key))
        .filter((message): message is string => message !== null)
        .join(' ') || 'Please correct the invalid fields.';
      this.toastService.error(this.errorMessage);
      return;
    }

    const endpoint = this.getEndpoint();
    const rawValues = this.form.getRawValue() as Record<string, unknown>;
    const payload = Object.fromEntries(this.fields.map((field) => [
      field.key,
      this.normalizeMasterValue(
        rawValues[field.key],
        field.type === 'number' || field.type === 'select' ? null : ''
      )
    ]));
    if (this.config.endpoint === 'cotivityemployees' && this.selectedImageBase64) {
      payload['imageBase64'] = this.selectedImageBase64;
    }
    const isCreating = this.editingId === null;
    this.isSaving = true;
    this.errorMessage = '';

    const request = isCreating
      ? this.http.post(endpoint, payload)
      : this.http.put(`${endpoint}/${this.editingId}`, payload);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toastService.success(`${this.config?.singular} ${isCreating ? 'created' : 'updated'}.`);
        this.isSaving = false;
        this.resetForm();
        this.loadRows();
        if (this.cotivityEmployeeSettings) {
          this.loadCotivityLookups();
        }
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        this.toastService.error(getApiErrorMessage(error, `Could not save ${this.config?.singular?.toLowerCase()}.`));
        this.isSaving = false;
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  edit(row: Record<string, unknown>): void {
    if (!this.config) {
      return;
    }
    if (!this.canUpdate) {
      this.toastService.error(`You do not have permission to update ${this.config.singular.toLowerCase()}.`);
      return;
    }

    this.editingId = Number(row[this.config.idField]);
    const value = Object.fromEntries(this.fields.map((field) => [
      field.key,
      this.normalizeMasterValue(
        row[field.key],
        field.type === 'checkbox' ? true : field.type === 'number' || field.type === 'select' ? null : ''
      )
    ]));
    this.form.reset(value);
    this.selectedImageBase64 = null;
    this.currentImageBase64 = typeof row['imageBase64'] === 'string' ? row['imageBase64'] : null;
    this.imageFileName = '';
    this.errorMessage = '';
  }

  delete(row: Record<string, unknown>): void {
    if (!this.config) {
      return;
    }
    if (!this.canDelete) {
      this.toastService.error(`You do not have permission to delete ${this.config.singular.toLowerCase()}.`);
      return;
    }

    const label = String(row[this.config.fields[0].key] ?? this.config.singular);
    const id = Number(row[this.config.idField]);
    const endpoint = `${this.getEndpoint()}/${id}`;
    const singular = this.config.singular;
    this.toastService.confirm(`Delete ${label}?`, () => this.deleteConfirmed(endpoint, id, label, singular));
  }

  private deleteConfirmed(endpoint: string, id: number, label: string, singular: string): void {
    this.http.delete(endpoint).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toastService.success(`${singular} deleted.`);
        this.loadRows();
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        this.toastService.error(getApiErrorMessage(error, `Could not delete ${label}.`));
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  resetForm(): void {
    this.editingId = null;
    this.selectedImageBase64 = null;
    this.currentImageBase64 = null;
    this.imageFileName = '';
    this.form = this.formBuilder.group({});
    for (const field of this.fields) {
      const validators: ValidatorFn[] = [];
      if (field.type !== 'checkbox' && field.required !== false) {
        validators.push(Validators.required);
      }
      if (field.maxLength !== undefined) {
        validators.push(Validators.maxLength(field.maxLength));
      }
      if (field.minLength !== undefined) {
        validators.push(Validators.minLength(field.minLength));
      }
      if (field.min !== undefined) {
        validators.push(Validators.min(field.min));
      }
      if (field.max !== undefined) {
        validators.push(Validators.max(field.max));
      }
      if (field.integer) {
        validators.push(Validators.pattern(/^\d+$/));
      }
      const initialValue = field.type === 'checkbox'
        ? true
        : field.type === 'number'
          ? null
          : field.type === 'select' ? null : '';
      this.form.addControl(field.key, this.formBuilder.control(initialValue, validators));
    }
  }

  fieldError(key: string): string | null {
    const control = this.form.get(key);
    const field = this.fields.find((item) => item.key === key);
    if (!control || !field || !control.touched || control.valid) {
      return null;
    }

    if (control.hasError('required')) {
      return `${field.label} is required.`;
    }
    if (control.hasError('minlength') && field.minLength !== undefined) {
      return `${field.label} must be at least ${field.minLength} characters.`;
    }
    if (control.hasError('maxlength') && field.maxLength !== undefined) {
      return `${field.label} must be ${field.maxLength} characters or fewer.`;
    }
    if (control.hasError('min') && field.min !== undefined) {
      return `${field.label} must be at least ${field.min}.`;
    }
    if (control.hasError('max') && field.max !== undefined) {
      return `${field.label} must be ${field.max} or less.`;
    }
    if (control.hasError('pattern') && field.integer) {
      return `${field.label} must be a whole number.`;
    }
    return `${field.label} is invalid.`;
  }

  normalizeNullInput(event: Event, controlName: string): void {
    const input = event.target;
    if (!(input instanceof HTMLInputElement || input instanceof HTMLSelectElement) ||
        (input instanceof HTMLInputElement && (input.type === 'checkbox' || input.type === 'file'))) {
      return;
    }

    if (input.value.trim().toLowerCase() !== 'null') {
      return;
    }

    this.clearNullInput(input, controlName);
  }

  private normalizeRenderedNullInputs(): void {
    this.normalizeNullFormValues();
    const inputs = this.masterForm?.nativeElement.querySelectorAll<HTMLInputElement | HTMLSelectElement>('[formControlName]');
    inputs?.forEach((input) => {
      if (input.value.trim().toLowerCase() === 'null') {
        const controlName = input.getAttribute('formControlName');
        if (controlName) {
          this.clearNullInput(input, controlName);
        }
      }
    });
  }

  private clearNullInput(input: HTMLInputElement | HTMLSelectElement, controlName: string): void {
    input.value = '';
    const type = input instanceof HTMLInputElement ? input.type : 'select';
    this.form.get(controlName)?.setValue(type === 'number' || type === 'select' ? null : '', { emitEvent: false });
  }

  normalizeNullFormValues(): void {
    for (const field of this.fields) {
      const control = this.form.get(field.key);
      if (!control || field.type === 'checkbox') {
        continue;
      }

      const fallback = field.type === 'number' || field.type === 'select' ? null : '';
      const normalized = this.normalizeMasterValue(control.value, fallback);
      if (normalized !== control.value) {
        control.setValue(normalized, { emitEvent: false });
      }
    }
  }

  showFieldError(controlName: string): void {
    const control = this.form.get(controlName);
    if (!control) {
      return;
    }

    this.normalizeNullFormValues();
    control.markAsTouched();
    const message = this.fieldError(controlName);
    if (message) {
      this.toastService.error(message);
    }
  }

  get cotivityEmployeeSettings(): boolean {
    return this.config?.endpoint === 'cotivityemployees';
  }

  get canCreate(): boolean {
    return this.hasMasterActionPermission('create');
  }

  get canUpdate(): boolean {
    return this.hasMasterActionPermission('update');
  }

  get canDelete(): boolean {
    return this.hasMasterActionPermission('delete');
  }

  optionsFor(field: MasterField): LookupItem[] {
    const options = field.optionsSource ? this[field.optionsSource] : [];
    return field.key === 'reportsToCotivityEmployeeId' && this.editingId !== null
      ? options.filter((option) => option.id !== this.editingId)
      : options;
  }

  displayValue(row: Record<string, unknown>, field: MasterField): unknown {
    if (field.key === 'reportsToCotivityEmployeeId') {
      const name = row['reportsToCotivityEmployeeName'];
      if (typeof name === 'string' && name.trim()) {
        return name;
      }

      const id = Number(row[field.key]);
      return this.cotivityEmployees.find((employee) => employee.id === id)?.name ?? '-';
    }

    return this.normalizeMasterValue(row[field.displayKey ?? field.key], '-');
  }

  private normalizeMasterValue(value: unknown, fallback: unknown): unknown {
    if (value === null || value === undefined) {
      return fallback;
    }

    if (typeof value === 'string' && value.trim().toLowerCase() === 'null') {
      return fallback;
    }

    return value;
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    if (!['image/jpeg', 'image/png', 'image/gif'].includes(file.type)) {
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

      this.selectedImageBase64 = reader.result;
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

  clearSelectedImage(input: HTMLInputElement): void {
    this.selectedImageBase64 = null;
    this.imageFileName = '';
    input.value = '';
  }

  changePage(pageNumber: number): void {
    this.loadRows(pageNumber, this.pageSize);
  }

  changePageSize(pageSize: number): void {
    this.loadRows(1, pageSize);
  }

  private loadRows(pageNumber = this.pageNumber, pageSize = this.pageSize): void {
    if (!this.config) {
      return;
    }

    this.pageNumber = pageNumber;
    this.pageSize = pageSize;
    this.isLoading = true;
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    this.http.get<ApiResponse<PagedResult<Record<string, unknown>>>>(this.getEndpoint(), { params })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          const page = response.data;
          this.rows = page?.items ?? [];
          this.totalCount = page?.totalCount ?? 0;
          this.pageNumber = page?.pageNumber ?? pageNumber;
          this.pageSize = page?.pageSize ?? pageSize;
          this.isLoading = false;
          this.changeDetectorRef.markForCheck();
        },
        error: (error) => {
          this.rows = [];
          this.totalCount = 0;
          this.isLoading = false;
          this.toastService.error(getApiErrorMessage(error, `Could not load ${this.config?.title.toLowerCase()}.`));
          this.changeDetectorRef.markForCheck();
        }
      });
  }

  private loadCotivityLookups(): void {
    const params = new HttpParams().set('pageNumber', 1).set('pageSize', 100);
    forkJoin({
      departments: this.http.get<ApiResponse<PagedResult<Record<string, unknown>>>>(
        `${this.apiBaseUrl}/cotivitydepartments`, { params }),
      designations: this.http.get<ApiResponse<PagedResult<Record<string, unknown>>>>(
        `${this.apiBaseUrl}/cotivitydesignations`, { params }),
      employees: this.http.get<ApiResponse<PagedResult<Record<string, unknown>>>>(
        `${this.apiBaseUrl}/cotivityemployees`, { params })
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ departments, designations, employees }) => {
        this.cotivityDepartments = (departments.data?.items ?? []).map((item) => ({
          id: Number(item['cotivityDepartmentId']),
          name: String(item['cotivityDepartmentName'] ?? '')
        }));
        this.cotivityDesignations = (designations.data?.items ?? []).map((item) => ({
          id: Number(item['cotivityDesignationId']),
          name: String(item['cotivityDesignationName'] ?? '')
        }));
        this.cotivityEmployees = (employees.data?.items ?? []).map((item) => ({
          id: Number(item['cotivityEmployeeId']),
          name: String(item['cotivityEmployeeName'] ?? '')
        }));
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        this.toastService.error(getApiErrorMessage(error, 'Could not load Cotivity departments and designations.'));
      }
    });
  }

  private getEndpoint(): string {
    return `${this.apiBaseUrl}/${this.config?.endpoint}`;
  }

  private hasMasterActionPermission(action: 'create' | 'update' | 'delete'): boolean {
    if (!this.currentMasterKey) {
      return false;
    }

    const actionCode = action === 'update' ? 'edit' : action;
    const specializedMaster = {
      'cotivity-department': 'cotivity.department',
      'cotivity-designation': 'cotivity.designation',
      'cotivity-employee': 'cotivity.employee',
      department: 'organization.department',
      designation: 'organization.designation',
      'business-unit': 'organization.businessunit',
      'dress-size': 'organization.dresssize',
      'address-type': 'organization.addresstype',
      'blood-group': 'organization.bloodgroup',
      skills: 'organization.skills',
      'job-type': 'organization.jobtype',
      team: 'organization.team',
      gender: 'organization.gender'
    } as const;
    const permissionCode = this.currentMasterKey === 'permission'
      ? `setting.gear.manage.permissions.${actionCode}`
      : this.currentMasterKey === 'role'
        ? `setting.gear.role.${actionCode}`
        : this.currentMasterKey in specializedMaster
          ? `${specializedMaster[this.currentMasterKey as keyof typeof specializedMaster]}.${actionCode}`
          : `master.${this.currentMasterKey}.${action}`;
    return this.authService.hasPermission(permissionCode);
  }
}
