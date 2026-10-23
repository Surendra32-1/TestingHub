import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

import { getApiErrorMessage } from '../../../../shared/services/api-error-message';
import { ToastService } from '../../../../shared/services/toast.service';
import { EmployeeDetails, EmployeeService } from '../../services/employee.service';

@Component({
  selector: 'app-employee-details',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './employee-details.component.html',
  styleUrl: './employee-details.component.css'
})
export class EmployeeDetailsComponent implements OnInit {
  @Input({ required: true }) employeeId!: number;
  @Output() back = new EventEmitter<void>();

  employee: EmployeeDetails | null = null;
  isLoading = true;
  errorMessage = '';

  constructor(
    private readonly employeeService: EmployeeService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.employeeService.getEmployeeDetails(this.employeeId).subscribe({
      next: ({ data }) => {
        this.employee = data;
        this.isLoading = false;
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        this.errorMessage = getApiErrorMessage(error, 'Could not load employee details.');
        this.isLoading = false;
        this.toastService.error(this.errorMessage);
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  trackByAddressId(_index: number, address: EmployeeDetails['addresses'][number]): number {
    return address.addressId;
  }
}
