import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { EMPTY, expand, reduce } from 'rxjs';
import { Employee } from '../../models/employee.model';
import {
  EmployeeDetails,
  EmployeeReleaseRequest,
  EmployeeService,
  EmployeeTerminationRequest
} from '../../services/employee.service';
import { TopbarComponent } from '../../../../shared/components/topbar/topbar.component';
import { ToastService } from '../../../../shared/services/toast.service';
import { getApiErrorMessage } from '../../../../shared/services/api-error-message';
import { AuthService } from '../../../../core/auth/auth.service';

@Component({
  selector: 'app-employee-lifecycle-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TopbarComponent],
  templateUrl: './employee-lifecycle-form.html',
  styleUrl: './employee-lifecycle-form.css'
})
export class EmployeeLifecycleFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly employeeService = inject(EmployeeService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  private readonly authService = inject(AuthService);

  readonly mode = this.route.snapshot.data['mode'] as string;
  readonly isRelease = this.mode === 'release' || this.mode === 'resignation';
  readonly isSelfService = this.mode === 'resignation';
  readonly isOffboarding = this.mode === 'release';
  readonly employeeLocked = this.isSelfService || this.isOffboarding;
  readonly employeeIdFromRoute = Number(this.route.snapshot.paramMap.get('employeeId'));
  readonly maxTerminationDate = this.toDateInputValue(new Date());
  readonly terminationTypes = ['Performance', 'Policy Violation', 'Misconduct', 'Layoff', 'Contract End', 'Attendance', 'Other'];
  readonly offboardingReleaseTypes = ['Retirement', 'Contract End', 'Internal Transfer', 'Mutual Separation'];
  employees: Employee[] = [];
  selectedEmployee: EmployeeDetails | null = null;
  lockedEmployeeLabel = '';
  form: FormGroup = this.fb.group({});
  isSubmitting = false;
  isLoadingEmployees = true;

  get requiredNoticePeriodDays(): number | null {
    return this.toNullableNumber(this.form.get('noticePeriodDays')?.value);
  }

  get servedNoticeDays(): number | null {
    return this.calculateServedNoticeDays(
      this.form.get('resignationDate')?.value,
      this.form.get('lastWorkingDate')?.value
    );
  }

  get noticePeriodShortfallDays(): number | null {
    const requiredDays = this.requiredNoticePeriodDays;
    return requiredDays === null || this.servedNoticeDays === null
      ? null
      : Math.max(requiredDays - this.servedNoticeDays, 0);
  }

  get expectedReleaseDate(): Date | null {
    const resignationDate = this.parseDateInputValue(this.form.get('resignationDate')?.value);
    const noticePeriodDays = this.requiredNoticePeriodDays;
    if (!resignationDate || noticePeriodDays === null || noticePeriodDays < 0) return null;
    resignationDate.setDate(resignationDate.getDate() + noticePeriodDays);
    return resignationDate;
  }

  get isEarlyRelease(): boolean {
    const noticePeriodDays = this.requiredNoticePeriodDays;
    return noticePeriodDays !== null &&
      this.servedNoticeDays !== null &&
      this.servedNoticeDays < noticePeriodDays;
  }

  get pageLabel(): string {
    if (this.isOffboarding) {
      return 'Release Employee';
    }
    return this.isRelease ? 'New Resignation' : 'New Termination';
  }

  ngOnInit(): void {
    this.form = this.isRelease ? this.createReleaseForm() : this.createTerminationForm();
    if (this.isSelfService) {
      this.initializeSelfServiceRelease();
      return;
    }
    if (this.isOffboarding) {
      this.initializeOffboardingRelease();
      return;
    }
    this.loadEmployees();
  }

  private initializeOffboardingRelease(): void {
    this.isLoadingEmployees = false;
    if (!Number.isInteger(this.employeeIdFromRoute) || this.employeeIdFromRoute < 1) {
      this.toastService.error('No employee was selected for release.');
      this.changeDetectorRef.markForCheck();
      return;
    }
    this.form.patchValue({ employeeId: this.employeeIdFromRoute });
    this.loadSelectedEmployee();
  }

  private initializeSelfServiceRelease(): void {
    this.authService.getCurrentUser().subscribe({
      next: (user) => {
        if (!Number.isInteger(user.employeeId) || user.employeeId < 1) {
          this.isLoadingEmployees = false;
          this.toastService.error('Your account is not linked to an employee record, so you cannot file a resignation. Please contact HR.');
          this.changeDetectorRef.markForCheck();
          return;
        }
        this.form.patchValue({ employeeId: user.employeeId });
        this.loadSelectedEmployee();
      },
      error: (error) => {
        this.isLoadingEmployees = false;
        this.toastService.error(getApiErrorMessage(error, 'Could not load your profile.'));
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  loadSelectedEmployee(): void {
    const employeeId = Number(this.form.get('employeeId')?.value);
    this.selectedEmployee = null;
    if (!Number.isInteger(employeeId) || employeeId < 1) {
      this.changeDetectorRef.markForCheck();
      return;
    }

    this.employeeService.getEmployeeDetails(employeeId).subscribe({
      next: (response) => {
        this.selectedEmployee = response.data;
        if (this.employeeLocked) {
          const fullName = [response.data.firstName, response.data.middleName, response.data.lastName]
            .filter(Boolean)
            .join(' ');
          this.lockedEmployeeLabel = `${fullName} — ${response.data.designationName || 'No designation'}`;
        }
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        this.toastService.error(getApiErrorMessage(error, 'Could not load the selected employee.'));
      }
    });
  }

  saveDraft(): void {
    this.save('Draft');
  }

  submitWorkflow(): void {
    this.save('Submitted');
  }

  cancel(): void {
    if (this.mode === 'release') {
      this.router.navigate(['/people']);
      return;
    }
    this.router.navigate([this.isRelease ? '/employees/resignations' : '/employees/terminations']);
  }

  private save(status: string): void {
    if (this.isSubmitting) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.toastService.error(this.getValidationMessage());
      return;
    }

    const employeeId = Number(this.form.getRawValue().employeeId);
    const value = this.form.getRawValue();
    this.isSubmitting = true;

    if (this.isRelease) {
      if (this.isEarlyRelease) {
        this.toastService.info(
          `Employee has served only ${this.servedNoticeDays} days against required ${value.noticePeriodDays} days. Please provide the Early Release Reason.`
        );
      }
      const request: EmployeeReleaseRequest = {
        releaseType: this.isOffboarding ? value.releaseType : 'Resignation',
        resignationDate: value.resignationDate || null,
        noticeDate: value.resignationDate || null,
        lastWorkingDate: value.lastWorkingDate || null,
        noticePeriodDays: this.toNullableNumber(value.noticePeriodDays),
        servedNoticeDays: this.calculateServedNoticeDays(value.resignationDate, value.lastWorkingDate),
        reason: value.reason?.trim() || null,
        earlyReleaseReason: value.earlyReleaseReason?.trim() || null,
        remarks: value.remarks?.trim() || null,
        knowledgeTransferCompleted: false,
        assetsReturned: false,
        exitInterviewCompleted: false,
        settlementAmount: null,
        relievingLetterIssued: false,
        experienceLetterIssued: false,
        status: status as EmployeeReleaseRequest['status']
      };
      const destination = this.isOffboarding ? '/people' : '/employees/resignations';
      const successMessage = this.isOffboarding ? 'Employee release created successfully.' : 'Resignation saved successfully.';
      const failureMessage = this.isOffboarding ? 'Could not save the employee release.' : 'Could not save the resignation.';
      this.employeeService.createEmployeeRelease(employeeId, request).subscribe({
        next: () => this.complete(successMessage, destination),
        error: (error) => this.fail(error, failureMessage)
      });
      return;
    }

    const request: EmployeeTerminationRequest = {
      terminationType: value.terminationType as EmployeeTerminationRequest['terminationType'],
      effectiveDate: value.terminationDate || null,
      investigationRequired: false,
      investigationSummary: null,
      warningCount: null,
      lastWarningDate: null,
      reason: value.reason?.trim() || null,
      remarks: value.remarks?.trim() || null,
      assetRecovered: false,
      systemAccessRevoked: false,
      exitInterviewCompleted: false,
      settlementAmount: null,
      eligibleForRehire: false,
      terminationLetterIssued: false,
      terminationLetterDate: null,
      status: status as EmployeeTerminationRequest['status']
    };
    this.employeeService.createEmployeeTermination(employeeId, request).subscribe({
      next: () => this.complete('Termination saved successfully.', '/employees/terminations'),
      error: (error) => this.fail(error, 'Could not save the termination.')
    });
  }

  private loadEmployees(): void {
    const getPage = (pageNumber: number) => this.employeeService.getEmployees(pageNumber, 100);
    getPage(1).pipe(
      expand((page) => page.hasNextPage ? getPage(page.pageNumber + 1) : EMPTY),
      reduce((employees, page) => employees.concat(page.items), [] as Employee[])
    ).subscribe({
      next: (employees) => {
        this.employees = employees;
        this.isLoadingEmployees = false;
        if (Number.isInteger(this.employeeIdFromRoute) && this.employeeIdFromRoute > 0) {
          this.form.patchValue({ employeeId: this.employeeIdFromRoute });
          this.loadSelectedEmployee();
        }
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        this.isLoadingEmployees = false;
        this.toastService.error(getApiErrorMessage(error, 'Could not load employees.'));
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  private createReleaseForm(): FormGroup {
    return this.fb.group({
      employeeId: ['', Validators.required],
      releaseType: [this.isSelfService ? 'Resignation' : '', this.isOffboarding ? [Validators.required] : []],
      resignationDate: ['', Validators.required],
      noticePeriodDays: ['', [Validators.required, Validators.min(0), Validators.max(180), Validators.pattern(/^\d*$/)]],
      lastWorkingDate: ['', Validators.required],
      reason: ['', Validators.maxLength(500)],
      earlyReleaseReason: ['', Validators.maxLength(500)],
      remarks: ['']
    }, { validators: [this.lastWorkingDateValidator(), this.reasonOrEarlyReleaseValidator()] });
  }

  private reasonOrEarlyReleaseValidator(): ValidatorFn {
    return (group): ValidationErrors | null => {
      const reason = String(group.get('reason')?.value ?? '').trim();
      const earlyReleaseReason = String(group.get('earlyReleaseReason')?.value ?? '').trim();
      if (earlyReleaseReason || reason.length >= 10) return null;
      return { eitherReasonRequired: true };
    };
  }

  private createTerminationForm(): FormGroup {
    return this.fb.group({
      employeeId: ['', Validators.required],
      terminationDate: ['', [Validators.required, this.notFutureDateValidator()]],
      terminationType: ['', Validators.required],
      reason: ['', [this.trimmedReasonValidator(1), Validators.maxLength(500)]],
      remarks: ['']
    });
  }

  private toNullableNumber(value: string | number | null | undefined): number | null {
    if (value === null || value === undefined || value === '') return null;
    const numberValue = Number(value);
    return Number.isFinite(numberValue) ? numberValue : null;
  }

  private getValidationMessage(): string {
    if (this.form.get('employeeId')?.hasError('required')) {
      return 'Select an employee.';
    }

    if (this.isRelease) {
      if (this.isOffboarding && this.form.get('releaseType')?.hasError('required')) {
        return 'Select a release type.';
      }
      if (this.form.get('resignationDate')?.hasError('required')) {
        return 'Resignation Date is required.';
      }
      if (this.form.get('lastWorkingDate')?.hasError('required')) {
        return 'Last Working Day is required.';
      }
      if (this.form.hasError('lastWorkingBeforeResignation')) {
        return 'Last Working Day must be greater than or equal to Resignation Date.';
      }
      if (this.form.get('noticePeriodDays')?.hasError('required')) {
        return 'Notice Period Days is required so early release can be detected.';
      }
      if (this.form.get('noticePeriodDays')?.hasError('max')) {
        return 'Notice Period Days must be between 0 and 180.';
      }
      if (this.form.get('noticePeriodDays')?.invalid) {
        return 'Notice Period Days must be a whole number greater than or equal to 0.';
      }
      const reason = String(this.form.get('reason')?.value ?? '').trim();
      if (this.form.hasError('eitherReasonRequired')) {
        return this.isEarlyRelease
          ? 'Early Release Reason is required when the employee does not serve the full notice period.'
          : 'Reason is required (at least 10 characters).';
      }
      if (reason.length > 500) {
        return 'Reason cannot exceed 500 characters.';
      }
      return 'Complete all required resignation fields.';
    }

    if (this.form.get('terminationDate')?.hasError('required')) {
      return 'Termination Date is required.';
    }
    if (this.form.get('terminationDate')?.hasError('futureDate')) {
      return 'Termination Date cannot be in the future.';
    }
    if (this.form.get('terminationType')?.hasError('required')) {
      return 'Termination Type is required.';
    }
    if (!String(this.form.get('reason')?.value ?? '').trim()) {
      return 'Reason is required.';
    }
    if (String(this.form.get('reason')?.value ?? '').length > 500) {
      return 'Reason cannot exceed 500 characters.';
    }
    return 'Complete all required termination fields.';
  }

  private trimmedReasonValidator(minimumLength = 10): ValidatorFn {
    return (control): ValidationErrors | null => {
      const reason = String(control.value ?? '').trim();
      if (!reason) return { required: true };
      return reason.length < minimumLength
        ? { minlength: { requiredLength: minimumLength, actualLength: reason.length } }
        : null;
    };
  }

  private lastWorkingDateValidator(): ValidatorFn {
    return (control): ValidationErrors | null => {
      const resignationDate = control.get('resignationDate')?.value as string | null;
      const lastWorkingDate = control.get('lastWorkingDate')?.value as string | null;
      return resignationDate && lastWorkingDate && lastWorkingDate < resignationDate
        ? { lastWorkingBeforeResignation: true }
        : null;
    };
  }

  private notFutureDateValidator(): ValidatorFn {
    return (control): ValidationErrors | null => {
      const value = control.value as string | null;
      return value && value > this.toDateInputValue(new Date())
        ? { futureDate: true }
        : null;
    };
  }

  private toDateInputValue(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private calculateServedNoticeDays(
    resignationDate: string | null | undefined,
    lastWorkingDate: string | null | undefined
  ): number | null {
    if (!resignationDate || !lastWorkingDate) return null;
    const resignation = Date.parse(`${resignationDate}T00:00:00Z`);
    const lastWorking = Date.parse(`${lastWorkingDate}T00:00:00Z`);
    if (Number.isNaN(resignation) || Number.isNaN(lastWorking)) return null;
    return Math.floor((lastWorking - resignation) / 86_400_000);
  }

  private parseDateInputValue(value: string | null | undefined): Date | null {
    if (!value) return null;
    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
    if (!match) return null;
    const date = new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
    return date.getFullYear() === Number(match[1]) &&
      date.getMonth() === Number(match[2]) - 1 &&
      date.getDate() === Number(match[3])
      ? date
      : null;
  }

  private complete(message: string, destination: string): void {
    this.toastService.success(message);
    this.router.navigate([destination]);
  }

  private fail(error: unknown, fallback: string): void {
    this.toastService.error(getApiErrorMessage(error, fallback));
    this.isSubmitting = false;
  }
}
