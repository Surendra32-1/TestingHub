import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

import { Employee } from '../../models/employee.model';
import { PublicEmployeeDetails } from '../../services/employee.service';

@Component({
  selector: 'app-department-hierarchy',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './department-hierarchy.component.html',
  styleUrl: './department-hierarchy.component.css'
})
export class DepartmentHierarchyComponent {
  @Input() canViewEmployeeDetails = true;
  @Input() departmentHead: Employee | null = null;
  @Input() departmentHeadDetails: PublicEmployeeDetails | null = null;
  @Input() members: Employee[] = [];
  @Input() departmentName = 'Department Members';
  @Output() viewEmployeeDetails = new EventEmitter<number>();

  trackByEmployeeId(_: number, employee: Employee): number {
    return employee.id;
  }

  viewDetails(employeeId: number): void {
    if (this.canViewEmployeeDetails) {
      this.viewEmployeeDetails.emit(employeeId);
    }
  }

  onCardKeydown(event: KeyboardEvent, employeeId: number): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.viewDetails(employeeId);
    }
  }
}
