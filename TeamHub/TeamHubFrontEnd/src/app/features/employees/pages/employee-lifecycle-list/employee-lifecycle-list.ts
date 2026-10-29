import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChangeDetectorRef, Component, DestroyRef, HostListener, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { Department } from '../../models/employee-master.model';
import { EmployeeReleaseRecord, EmployeeService, EmployeeTerminationRecord, ReleaseClearanceUpdate, ReleaseStatus, TerminationClearanceUpdate, TerminationStatus } from '../../services/employee.service';
import { TopbarComponent } from '../../../../shared/components/topbar/topbar.component';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { ToastService } from '../../../../shared/services/toast.service';
import { getApiErrorMessage } from '../../../../shared/services/api-error-message';
import { AuthService } from '../../../../core/auth/auth.service';

@Component({
  selector: 'app-employee-lifecycle-list',
  standalone: true,
  imports: [CommonModule, FormsModule, TopbarComponent, PaginationComponent],
  templateUrl: './employee-lifecycle-list.html',
  styleUrl: './employee-lifecycle-list.css'
})
export class EmployeeLifecycleListComponent implements OnInit {
  private readonly employeeService = inject(EmployeeService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly authService = inject(AuthService);
  private readonly searchSubject = new Subject<string>();

  readonly isResignation = this.route.snapshot.data['mode'] === 'resignation';
  readonly statuses = this.isResignation
    ? ['Draft', 'Submitted', 'PendingApproval', 'Approved', 'Released', 'Rejected', 'Cancelled']
    : ['Draft', 'Submitted', 'PendingApproval', 'Approved', 'Terminated', 'Rejected', 'Cancelled'];

  private readonly resignationTransitions: Record<ReleaseStatus, ReleaseStatus[]> = {
    Draft: ['Submitted', 'Cancelled'],
    Submitted: ['PendingApproval', 'Approved', 'Rejected', 'Cancelled'],
    PendingApproval: ['Approved', 'Rejected', 'Cancelled'],
    Approved: ['Released', 'Cancelled'],
    Released: [],
    Rejected: [],
    Cancelled: []
  };

  private readonly actionPermissions: Record<string, string> = {
    Submitted: 'resignations.action.submit',
    PendingApproval: 'resignations.action.sendforapproval',
    Approved: 'resignations.action.approve',
    Released: 'resignations.action.release',
    Rejected: 'resignations.action.reject',
    Cancelled: 'resignations.action.cancel'
  };

  private can(permission: string): boolean {
    return this.authService.hasPermission(permission);
  }

  get canAdd(): boolean {
    return this.can('resignations.add');
  }

  get canSearch(): boolean {
    return this.can(this.isResignation ? 'resignations.search' : 'terminations.search');
  }

  get canFilterByStatus(): boolean {
    return this.can(this.isResignation ? 'resignations.search.status' : 'terminations.search.status');
  }

  get canFilterByDepartment(): boolean {
    return this.can(this.isResignation ? 'resignations.search.department' : 'terminations.search.department');
  }

  get canManageClearance(): boolean {
    return this.can('resignations.action.exitchecklist');
  }

  get canAddTermination(): boolean {
    return this.can('terminations.add');
  }

  get canManageTerminations(): boolean {
    return this.can('terminations.manage');
  }

  private readonly terminationTransitions: Record<string, string[]> = {
    Draft: ['Submitted', 'Cancelled'],
    Submitted: ['PendingApproval', 'Approved', 'Rejected', 'Cancelled'],
    PendingApproval: ['Approved', 'Rejected', 'Cancelled'],
    Approved: ['Terminated', 'Cancelled'],
    Terminated: [],
    Rejected: [],
    Cancelled: []
  };

  private readonly actionLabels: Record<string, string> = {
    Draft: 'Draft',
    Submitted: 'Submit',
    PendingApproval: 'Send for Approval',
    Approved: 'Approve',
    Released: 'Release',
    Terminated: 'Terminate',
    Rejected: 'Reject',
    Cancelled: 'Cancel'
  };

  private readonly displayLabels: Record<string, string> = {
    PendingApproval: 'Pending Approval'
  };

  readonly clearanceFields: Array<{ key: keyof ReleaseClearanceUpdate & string; label: string }> = [
    { key: 'knowledgeTransferCompleted', label: 'Knowledge transfer' },
    { key: 'assetsReturned', label: 'Assets returned' },
    { key: 'exitInterviewCompleted', label: 'Exit interview' },
    { key: 'relievingLetterIssued', label: 'Relieving letter' },
    { key: 'experienceLetterIssued', label: 'Experience letter' }
  ];

  readonly terminationClearanceFields: Array<{ key: keyof TerminationClearanceUpdate & string; label: string }> = [
    { key: 'assetRecovered', label: 'Asset recovered' },
    { key: 'systemAccessRevoked', label: 'System access revoked' },
    { key: 'exitInterviewCompleted', label: 'Exit interview' },
    { key: 'terminationLetterIssued', label: 'Termination letter' }
  ];

  private readonly busyIds = new Set<number>();
  pageSize = 10;
  currentPage = 1;
  openMenuId: number | null = null;
  detailId: number | null = null;
  menuLeft = 0;
  menuTop = 0;
  departments: Department[] = [];
  releases: EmployeeReleaseRecord[] = [];
  terminations: EmployeeTerminationRecord[] = [];
  searchTerm = '';
  selectedDepartment = '';
  selectedStatus = '';
  isLoading = true;
  totalItems = 0;

  ngOnInit(): void {
    this.employeeService.getDepartments().subscribe({
      next: (departments) => {
        this.departments = departments;
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => this.toastService.error(getApiErrorMessage(error, 'Could not load departments.'))
    });

    this.searchSubject.pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.reload(1));

    this.loadPage();
  }

  private loadPage(): void {
    this.isLoading = true;
    const search = this.searchTerm.trim();
    const department = this.selectedDepartment || undefined;
    const status = this.selectedStatus || undefined;
    if (this.isResignation) {
      this.employeeService.getEmployeeReleasesPaged({ pageNumber: this.currentPage, pageSize: this.pageSize, search, department, status, releaseType: 'Resignation' }).subscribe({
        next: (response) => {
          this.releases = response.data.items;
          this.totalItems = response.data.totalCount;
          this.isLoading = false;
          this.changeDetectorRef.markForCheck();
        },
        error: (error: unknown) => this.handleLoadError(error)
      });
    } else {
      this.employeeService.getEmployeeTerminationsPaged({ pageNumber: this.currentPage, pageSize: this.pageSize, search, department, status }).subscribe({
        next: (response) => {
          this.terminations = response.data.items;
          this.totalItems = response.data.totalCount;
          this.isLoading = false;
          this.changeDetectorRef.markForCheck();
        },
        error: (error: unknown) => this.handleLoadError(error)
      });
    }
  }

  private reload(page: number): void {
    this.currentPage = page;
    this.loadPage();
  }

  private handleLoadError(error: unknown): void {
        this.isLoading = false;
        this.toastService.error(getApiErrorMessage(error, 'Could not load employee lifecycle records.'));
        this.changeDetectorRef.markForCheck();
  }

  get filteredReleases(): EmployeeReleaseRecord[] {
    const search = this.searchTerm.trim().toLowerCase();
    return this.releases.filter((record) =>
      (!search || record.employeeName.toLowerCase().includes(search) || String(record.employeeCode).includes(search)) &&
      (!this.selectedDepartment || record.department === this.selectedDepartment) &&
      (!this.selectedStatus || record.status === this.selectedStatus)
    );
  }

  get filteredTerminations(): EmployeeTerminationRecord[] {
    const search = this.searchTerm.trim().toLowerCase();
    return this.terminations.filter((record) =>
      (!search || record.employeeName.toLowerCase().includes(search) || String(record.employeeCode).includes(search)) &&
      (!this.selectedDepartment || record.department === this.selectedDepartment) &&
      (!this.selectedStatus || record.status === this.selectedStatus)
    );
  }

  get pagedReleases(): EmployeeReleaseRecord[] {
    return this.filteredReleases;
  }

  get pagedTerminations(): EmployeeTerminationRecord[] {
    return this.filteredTerminations;
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalItems / this.pageSize));
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.currentPage) return;
    this.reload(page);
  }

  onPageSizeChange(size: number): void {
    if (size <= 0 || size === this.pageSize) return;
    this.pageSize = size;
    this.reload(1);
  }

  resetPage(): void {
    this.reload(1);
  }

  onSearchChange(): void {
    this.searchSubject.next(this.searchTerm);
  }

  createRecord(): void {
    this.router.navigate([this.isResignation
      ? '/employees/resignations/new'
      : '/employees/terminations/new']);
  }

  statusLabel(status: string): string {
    return this.displayLabels[status] ?? status;
  }

  isBusy(releasedId: number): boolean {
    return this.busyIds.has(releasedId);
  }

  nextActions(record: EmployeeReleaseRecord): ReleaseStatus[] {
    const allowed = this.resignationTransitions[record.status] ?? [];
    return allowed.filter((action) => this.can(this.actionPermissions[action]));
  }

  hasResignationActions(record: EmployeeReleaseRecord): boolean {
    return this.nextActions(record).length > 0
      || (record.status === 'Approved' && this.canManageClearance);
  }

  nextTerminationActions(record: EmployeeTerminationRecord): string[] {
    if (!this.canManageTerminations) return [];
    return this.terminationTransitions[record.status] ?? [];
  }

  actionLabel(status: string): string {
    return this.actionLabels[status] ?? status;
  }

  isCleared(record: EmployeeReleaseRecord): boolean {
    return this.clearanceFields.every((field) => !!record[field.key as keyof EmployeeReleaseRecord]);
  }

  clearedCount(record: EmployeeReleaseRecord): number {
    return this.clearanceFields.filter((field) => !!record[field.key as keyof EmployeeReleaseRecord]).length;
  }

  isTerminationCleared(record: EmployeeTerminationRecord): boolean {
    return this.terminationClearanceFields.every((field) => !!record[field.key as keyof EmployeeTerminationRecord]);
  }

  clearedTerminationCount(record: EmployeeTerminationRecord): number {
    return this.terminationClearanceFields.filter((field) => !!record[field.key as keyof EmployeeTerminationRecord]).length;
  }

  get detailRelease(): EmployeeReleaseRecord | null {
    return this.releases.find((record) => record.releasedId === this.detailId) ?? null;
  }

  get detailTermination(): EmployeeTerminationRecord | null {
    return this.terminations.find((record) => record.terminationId === this.detailId) ?? null;
  }

  openDetails(id: number): void {
    this.closeMenu();
    this.detailId = id;
    this.changeDetectorRef.markForCheck();
  }

  closeDetails(): void {
    this.detailId = null;
    this.changeDetectorRef.markForCheck();
  }

  yesNo(value: boolean | null | undefined): string {
    if (value === null || value === undefined) return '—';
    return value ? 'Yes' : 'No';
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.detailId !== null) this.closeDetails();
    else if (this.openMenuId !== null) this.closeMenu();
  }

  primaryActions(record: EmployeeReleaseRecord): ReleaseStatus[] {
    return this.nextActions(record).filter((action) => action !== 'Rejected' && action !== 'Cancelled');
  }

  dangerActions(record: EmployeeReleaseRecord): ReleaseStatus[] {
    return this.nextActions(record).filter((action) => action === 'Rejected' || action === 'Cancelled');
  }

  actionIcon(action: string): string {
    const icons: Record<string, string> = {
      Draft: '✎',
      Submitted: '➤',
      PendingApproval: '➤',
      Approved: '✓',
      Released: '⏏',
      Terminated: '⏏',
      Rejected: '✕',
      Cancelled: '⊘'
    };
    return icons[action] ?? '';
  }

  toggleMenu(releasedId: number, event: MouseEvent): void {
    if (this.openMenuId === releasedId) {
      this.closeMenu();
      return;
    }
    const trigger = (event.target as HTMLElement).closest('.menu-trigger') as HTMLElement;
    const rect = trigger.getBoundingClientRect();
    const menuWidth = 200;
    const gap = 8;
    const fitsRight = window.innerWidth - rect.right >= menuWidth + gap + 8;
    this.menuLeft = fitsRight ? rect.right + gap : Math.max(8, rect.left - menuWidth - gap);
    this.menuTop = Math.max(8, Math.min(rect.top, window.innerHeight - 260));
    this.openMenuId = releasedId;
  }

  closeMenu(): void {
    this.openMenuId = null;
  }

  @HostListener('window:scroll.capture')
  @HostListener('window:resize')
  onViewportChange(): void {
    if (this.openMenuId !== null) this.closeMenu();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.openMenuId === null) return;
    if (!(event.target as HTMLElement).closest('.action-menu')) {
      this.closeMenu();
    }
  }

  transition(record: EmployeeReleaseRecord, to: ReleaseStatus): void {
    this.closeMenu();
    if (this.isBusy(record.releasedId)) return;
    const isFinal = (this.resignationTransitions[to] ?? []).length === 0;
    this.toastService.confirm(
      this.confirmTransitionMessage('resignation', record.employeeName, to, isFinal),
      () => this.applyTransition(record, to),
      'Confirm'
    );
  }

  private applyTransition(record: EmployeeReleaseRecord, to: ReleaseStatus): void {
    if (this.isBusy(record.releasedId)) return;
    this.busyIds.add(record.releasedId);
    this.employeeService.updateEmployeeReleaseStatus(record.releasedId, to).subscribe({
      next: (response) => {
        this.replaceRelease(response.data);
        this.toastService.success(response.message || `Status updated to ${this.statusLabel(to)}.`);
        this.finish(record.releasedId);
      },
      error: (error) => {
        this.toastService.error(getApiErrorMessage(error, 'Could not update the resignation status.'));
        this.finish(record.releasedId);
      }
    });
  }

  private confirmTransitionMessage(
    kind: 'resignation' | 'termination',
    employeeName: string,
    to: string,
    isFinal: boolean
  ): string {
    const finalNote = isFinal ? ' This is a final state and cannot be reversed.' : '';
    return `${this.actionLabel(to)} \u2014 move ${employeeName}'s ${kind} to "${this.statusLabel(to)}"?${finalNote}`;
  }

  toggleClearance(record: EmployeeReleaseRecord, key: keyof ReleaseClearanceUpdate & string, checked: boolean): void {
    if (this.isBusy(record.releasedId)) return;
    this.busyIds.add(record.releasedId);
    const payload: ReleaseClearanceUpdate = {
      knowledgeTransferCompleted: key === 'knowledgeTransferCompleted' ? checked : record.knowledgeTransferCompleted,
      assetsReturned: key === 'assetsReturned' ? checked : record.assetsReturned,
      exitInterviewCompleted: key === 'exitInterviewCompleted' ? checked : record.exitInterviewCompleted,
      relievingLetterIssued: key === 'relievingLetterIssued' ? checked : record.relievingLetterIssued,
      experienceLetterIssued: key === 'experienceLetterIssued' ? checked : record.experienceLetterIssued
    };
    this.employeeService.updateEmployeeReleaseClearance(record.releasedId, payload).subscribe({
      next: (response) => {
        this.replaceRelease(response.data);
        this.finish(record.releasedId);
      },
      error: (error) => {
        this.toastService.error(getApiErrorMessage(error, 'Could not update the clearance checklist.'));
        this.finish(record.releasedId);
      }
    });
  }

  private replaceRelease(updated: EmployeeReleaseRecord): void {
    const index = this.releases.findIndex((record) => record.releasedId === updated.releasedId);
    if (index >= 0) {
      this.releases[index] = updated;
    }
    this.changeDetectorRef.markForCheck();
  }

  transitionTermination(record: EmployeeTerminationRecord, to: string): void {
    this.closeMenu();
    if (this.isBusy(record.terminationId)) return;
    const isFinal = (this.terminationTransitions[to] ?? []).length === 0;
    this.toastService.confirm(
      this.confirmTransitionMessage('termination', record.employeeName, to, isFinal),
      () => this.applyTerminationTransition(record, to),
      'Confirm'
    );
  }

  private applyTerminationTransition(record: EmployeeTerminationRecord, to: string): void {
    if (this.isBusy(record.terminationId)) return;
    this.busyIds.add(record.terminationId);
    this.employeeService.updateEmployeeTerminationStatus(record.terminationId, to as TerminationStatus).subscribe({
      next: (response) => {
        this.replaceTermination(response.data);
        this.toastService.success(response.message || `Status updated to ${this.statusLabel(to)}.`);
        this.finish(record.terminationId);
      },
      error: (error) => {
        this.toastService.error(getApiErrorMessage(error, 'Could not update the termination status.'));
        this.finish(record.terminationId);
      }
    });
  }

  toggleTerminationClearance(record: EmployeeTerminationRecord, key: keyof TerminationClearanceUpdate & string, checked: boolean): void {
    if (this.isBusy(record.terminationId)) return;
    this.busyIds.add(record.terminationId);
    const payload: TerminationClearanceUpdate = {
      assetRecovered: key === 'assetRecovered' ? checked : record.assetRecovered,
      systemAccessRevoked: key === 'systemAccessRevoked' ? checked : record.systemAccessRevoked,
      exitInterviewCompleted: key === 'exitInterviewCompleted' ? checked : record.exitInterviewCompleted,
      terminationLetterIssued: key === 'terminationLetterIssued' ? checked : record.terminationLetterIssued
    };
    this.employeeService.updateEmployeeTerminationClearance(record.terminationId, payload).subscribe({
      next: (response) => {
        this.replaceTermination(response.data);
        this.finish(record.terminationId);
      },
      error: (error) => {
        this.toastService.error(getApiErrorMessage(error, 'Could not update the clearance checklist.'));
        this.finish(record.terminationId);
      }
    });
  }

  private replaceTermination(updated: EmployeeTerminationRecord): void {
    const index = this.terminations.findIndex((record) => record.terminationId === updated.terminationId);
    if (index >= 0) {
      this.terminations[index] = updated;
    }
    this.changeDetectorRef.markForCheck();
  }

  private finish(releasedId: number): void {
    this.busyIds.delete(releasedId);
    this.changeDetectorRef.markForCheck();
  }
}
