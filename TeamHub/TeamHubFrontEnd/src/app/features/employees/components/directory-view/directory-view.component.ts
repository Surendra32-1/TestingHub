import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, EventEmitter, HostListener, Input, Output, inject } from '@angular/core';

import { DirectoryControlsComponent, EmployeeSearchField } from '../directory-controls/directory-controls.component';
import { ResultsHeaderComponent } from '../results-header/results-header.component';
import { Department } from '../../models/employee-master.model';
import { Employee } from '../../models/employee.model';
import { EmployeeDetails, EmployeeService, PublicEmployeeDetails } from '../../services/employee.service';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-directory-view',
  standalone: true,
  imports: [CommonModule, DirectoryControlsComponent, ResultsHeaderComponent, PaginationComponent],
  templateUrl: './directory-view.component.html',
  styleUrl: './directory-view.component.css'
})
export class DirectoryViewComponent {
  private readonly toastService = inject(ToastService);
  private readonly employeeService = inject(EmployeeService);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  private readonly previewCache = new Map<number, EmployeeDetails>();
  private showPreviewTimer?: ReturnType<typeof setTimeout>;
  private hidePreviewTimer?: ReturnType<typeof setTimeout>;
  @Input() employees: Employee[] = [];
  @Input() tableLayout = false;
  @Input() managementLayout = false;
  @Input() readOnlyHome = false;
  @Input() canEditEmployee = false;
  @Input() canDeleteEmployee = false;
  @Input() canViewCard = false;
  @Input() canViewHierarchy = true;
  @Input() canViewEmployeeDetails = true;
  @Input() canReleaseEmployee = false;
  @Input() canTerminateEmployee = false;
  @Input() showAddButton = true;
  @Input() showControls = true;
  @Input() isLoading = false;
  @Input() pageNumber = 1;
  @Input() pageSize = 10;
  @Input() totalCount = 0;
  @Input() resultsLabel = 'All Employees';
  @Input() searchTerm = '';
  @Input() searchField: EmployeeSearchField = 'all';
  @Input() departmentFilterEnabled = true;
  @Input() departmentFilterName: string | null = null;
  @Input() departmentOptions: Department[] | null = null;
  @Input() selectedDepartmentId: number | null = null;
  @Output() pageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();
  @Output() searchTermChange = new EventEmitter<string>();
  @Output() searchFieldChange = new EventEmitter<EmployeeSearchField>();
  @Output() departmentFilterChange = new EventEmitter<string>();
  @Output() clearDepartmentFilter = new EventEmitter<void>();
  @Output() departmentChange = new EventEmitter<number | null>();
  @Output() editEmployee = new EventEmitter<number>();
  @Output() deleteEmployee = new EventEmitter<number>();
  @Output() releaseEmployee = new EventEmitter<number>();
  @Output() terminateEmployee = new EventEmitter<number>();
  @Output() viewEmployeeHierarchy = new EventEmitter<number>();
  @Output() viewEmployeeDirectHierarchy = new EventEmitter<number>();
  @Output() viewEmployeeDetails = new EventEmitter<number>();
  openMenuEmployeeId: number | null = null;
  menuLeft = 0;
  menuTop = 0;

  previewEmployeeId: number | null = null;
  previewEmployee: EmployeeDetails | null = null;
  previewPublicEmployee: PublicEmployeeDetails | null = null;
  previewLoading = false;
  previewVisible = false;
  previewLeft = 0;
  previewTop = 0;

  onEmployeeHover(employeeId: number, event: MouseEvent): void {
    if (!this.readOnlyHome && !this.canViewEmployeeDetails) return;
    if (this.hidePreviewTimer) {
      clearTimeout(this.hidePreviewTimer);
      this.hidePreviewTimer = undefined;
    }
    if (this.previewEmployeeId === employeeId && this.previewVisible) {
      return;
    }
    const anchor = (event.currentTarget as HTMLElement) ?? (event.target as HTMLElement);
    if (this.showPreviewTimer) {
      clearTimeout(this.showPreviewTimer);
    }
    this.showPreviewTimer = setTimeout(() => this.openPreview(employeeId, anchor), 350);
  }

  onEmployeeLeave(): void {
    if (this.showPreviewTimer) {
      clearTimeout(this.showPreviewTimer);
      this.showPreviewTimer = undefined;
    }
    if (!this.previewVisible) {
      this.previewEmployeeId = null;
      return;
    }
    if (this.hidePreviewTimer) {
      clearTimeout(this.hidePreviewTimer);
    }
    this.hidePreviewTimer = setTimeout(() => this.closePreview(), 260);
  }

  keepPreview(): void {
    if (this.hidePreviewTimer) {
      clearTimeout(this.hidePreviewTimer);
      this.hidePreviewTimer = undefined;
    }
  }

  private openPreview(employeeId: number, anchor: HTMLElement): void {
    this.previewEmployeeId = employeeId;
    this.positionPreview(anchor);
    if (this.readOnlyHome) {
      this.previewPublicEmployee = null;
      this.previewEmployee = null;
      this.previewLoading = true;
      this.previewVisible = true;
      this.changeDetectorRef.markForCheck();
      this.employeeService.getPublicEmployeeDetails(employeeId).subscribe({
        next: ({ data }) => {
          if (this.previewEmployeeId === employeeId) {
            this.previewPublicEmployee = data;
            this.previewLoading = false;
            this.changeDetectorRef.markForCheck();
          }
        },
        error: () => {
          if (this.previewEmployeeId === employeeId) {
            this.previewLoading = false;
            this.previewVisible = false;
            this.changeDetectorRef.markForCheck();
          }
        }
      });
      return;
    }

    this.previewPublicEmployee = null;
    const cached = this.previewCache.get(employeeId);
    if (cached) {
      this.previewEmployee = cached;
      this.previewLoading = false;
      this.previewVisible = true;
      this.changeDetectorRef.markForCheck();
      return;
    }
    this.previewEmployee = null;
    this.previewLoading = true;
    this.previewVisible = true;
    this.changeDetectorRef.markForCheck();
    this.employeeService.getEmployeeDetails(employeeId).subscribe({
      next: ({ data }) => {
        this.previewCache.set(employeeId, data);
        if (this.previewEmployeeId === employeeId) {
          this.previewEmployee = data;
          this.previewLoading = false;
          this.changeDetectorRef.markForCheck();
        }
      },
      error: () => {
        if (this.previewEmployeeId === employeeId) {
          this.previewLoading = false;
          this.previewVisible = false;
          this.changeDetectorRef.markForCheck();
        }
      }
    });
  }

  private positionPreview(anchor: HTMLElement): void {
    const rect = anchor.getBoundingClientRect();
    const width = 320;
    const height = 420;
    const gap = 12;
    const spaceRight = window.innerWidth - rect.right;
    let left = spaceRight >= width + gap + 8 ? rect.right + gap : Math.max(8, rect.left - width - gap);
    let top = rect.top;
    if (top + height > window.innerHeight - 8) {
      top = Math.max(8, window.innerHeight - height - 8);
    }
    this.previewLeft = left;
    this.previewTop = top;
  }

  closePreview(): void {
    this.previewVisible = false;
    this.previewEmployeeId = null;
    this.previewEmployee = null;
    this.previewPublicEmployee = null;
    this.previewLoading = false;
    this.changeDetectorRef.markForCheck();
  }

  toggleActionMenu(employeeId: number, event: MouseEvent): void {
    event.stopPropagation();
    if (this.openMenuEmployeeId === employeeId) {
      this.openMenuEmployeeId = null;
      return;
    }
    const trigger = (event.target as HTMLElement).closest('.menu-trigger') as HTMLElement;
    const rect = trigger.getBoundingClientRect();
    const menuWidth = 44;
    const gap = 8;
    const fitsLeft = rect.left - menuWidth - gap >= 8;
    this.menuLeft = fitsLeft
      ? rect.left - menuWidth - gap
      : Math.min(rect.right + gap, window.innerWidth - menuWidth - 8);
    this.menuTop = Math.max(8, Math.min(rect.top, window.innerHeight - 220));
    this.openMenuEmployeeId = employeeId;
  }

  edit(employeeId: number): void {
    if (!this.canEditEmployee) return;
    this.openMenuEmployeeId = null;
    this.editEmployee.emit(employeeId);
  }

  showCard(employeeId: number): void {
    if (!this.canViewCard) return;
    this.openMenuEmployeeId = null;
    this.viewEmployeeDetails.emit(employeeId);
  }

  requestDelete(employeeId: number): void {
    if (!this.canDeleteEmployee) return;
    const employeeName = this.employees.find((item) => item.id === employeeId)?.name ?? 'this employee';
    this.toastService.confirm(
      `Soft-delete ${employeeName}? The employee record will be retained for history.`,
      () => this.deleteEmployee.emit(employeeId),
      'Delete'
    );
  }

  release(employeeId: number): void {
    this.openMenuEmployeeId = null;
    const employee = this.employees.find((item) => item.id === employeeId);
    const employeeName = employee?.name ?? 'this employee';
    this.toastService.confirm(
      `Create an offboarding release record for ${employeeName}? Choose a release type on the form that follows.`,
      () => this.releaseEmployee.emit(employeeId),
      'Release'
    );
  }

  terminate(employeeId: number): void {
    this.openMenuEmployeeId = null;
    const employee = this.employees.find((item) => item.id === employeeId);
    const employeeName = employee?.name ?? 'this employee';
    this.toastService.confirm(
      `Create a termination record for ${employeeName}?`,
      () => this.terminateEmployee.emit(employeeId),
      'Continue'
    );
  }

  showHierarchy(employeeId: number): void {
    this.openMenuEmployeeId = null;
    this.viewEmployeeHierarchy.emit(employeeId);
  }

  showDirectHierarchy(employeeId: number): void {
    this.openMenuEmployeeId = null;
    this.viewEmployeeDirectHierarchy.emit(employeeId);
  }

  showDetails(employeeId: number): void {
    this.viewEmployeeDetails.emit(employeeId);
  }

  @HostListener('document:click')
  closeActionMenu(): void {
    this.openMenuEmployeeId = null;
  }

  @HostListener('window:scroll.capture')
  @HostListener('window:resize')
  onViewportChange(): void {
    this.openMenuEmployeeId = null;
    this.closePreview();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.previewVisible) {
      this.closePreview();
    }
  }

}
