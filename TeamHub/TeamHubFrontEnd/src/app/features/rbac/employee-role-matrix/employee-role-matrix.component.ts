import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';

import { TopbarComponent } from '../../../shared/components/topbar/topbar.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { ToastService } from '../../../shared/services/toast.service';
import { getApiErrorMessage } from '../../../shared/services/api-error-message';
import { AuthService } from '../../../core/auth/auth.service';
import { RbacService } from '../rbac.service';
import { EmployeeRoleMatrixRow, RoleOption, SaveUserRolesRequest } from '../rbac.models';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, TopbarComponent, PaginationComponent],
  selector: 'app-employee-role-matrix',
  templateUrl: './employee-role-matrix.component.html',
  styleUrl: '../rbac-matrix.css'
})
export class EmployeeRoleMatrixComponent implements OnInit {
  private readonly rbac = inject(RbacService);
  private readonly auth = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  roles: RoleOption[] = [];
  rows: EmployeeRoleMatrixRow[] = [];

  private readonly selections = new Map<number, Set<number>>();
  private readonly originals = new Map<number, Set<number>>();

  isLoading = false;
  isSaving = false;
  hasPendingChanges = false;

  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;
  searchTerm = '';
  private searchTimeout?: ReturnType<typeof setTimeout>;

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize) || 1;
  }

  get canEdit(): boolean {
    return this.auth.hasPermission('setting.gear.EmployeeRole');
  }

  ngOnInit(): void {
    this.load();
  }

  isSelected(userId: number, roleId: number): boolean {
    return this.selections.get(userId)?.has(roleId) === true;
  }

  toggle(userId: number, roleId: number, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    const set = this.selections.get(userId);
    if (!set) {
      return;
    }

    if (checked) {
      set.add(roleId);
    } else {
      set.delete(roleId);
    }
    this.refreshDirtyState();
  }

  save(): void {
    if (this.isSaving || !this.hasPendingChanges) {
      return;
    }

    const assignments = Array.from(this.originals.keys())
      .filter((userId) => !this.setsEqual(this.selections.get(userId), this.originals.get(userId)))
      .map((userId) => ({ userId, roleIds: Array.from(this.selections.get(userId) ?? []) }));

    if (assignments.length === 0) {
      this.hasPendingChanges = false;
      return;
    }

    this.toastService.confirm(
      `Save changes to ${assignments.length} employee role assignment(s)?`,
      () => this.saveAssignments(assignments),
      'Save'
    );
  }

  private saveAssignments(assignments: SaveUserRolesRequest['assignments']): void {
    const request: SaveUserRolesRequest = { assignments };
    this.isSaving = true;
    this.rbac.saveEmployeeRoleMatrix(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toastService.success('Role assignments saved.');
          this.isSaving = false;
          this.originals.clear();
          this.selections.clear();
          this.hasPendingChanges = false;
          this.load();
          this.auth.refreshAccessToken()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              error: () => this.toastService.error(
                'Role assignments were saved, but this account session could not be refreshed. Sign out and back in to see updated navigation.'
              )
            });
        },
        error: (error) => {
          this.toastService.error(getApiErrorMessage(error, 'Could not save role assignments.'));
          this.isSaving = false;
          this.changeDetectorRef.markForCheck();
        }
      });
  }

  cancel(): void {
    this.selections.clear();
    this.originals.forEach((set, userId) => this.selections.set(userId, new Set(set)));
    this.refreshDirtyState();
  }

  onSearchChange(term: string): void {
    this.searchTerm = term;
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => {
      this.pageNumber = 1;
      this.load();
    }, 300);
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.pageNumber) {
      return;
    }
    this.pageNumber = page;
    this.load();
  }

  onPageSizeChange(size: number): void {
    if (size <= 0 || size === this.pageSize) {
      return;
    }
    this.pageSize = size;
    this.pageNumber = 1;
    this.load();
  }

  goBack(): void {
    void this.router.navigateByUrl('/administration');
  }

  trackByUser(_index: number, row: EmployeeRoleMatrixRow): number {
    return row.userId;
  }

  trackByRole(_index: number, role: RoleOption): number {
    return role.roleId;
  }

  private load(): void {
    this.isLoading = true;
    this.rbac.getEmployeeRoleMatrix({ pageNumber: this.pageNumber, pageSize: this.pageSize, search: this.searchTerm.trim() || undefined })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (matrix) => {
          this.roles = matrix?.roles ?? [];
          this.rows = matrix?.rows ?? [];
          this.totalCount = matrix?.totalCount ?? 0;
          this.pageNumber = matrix?.pageNumber ?? this.pageNumber;
          for (const row of this.rows) {
            if (this.originals.has(row.userId)) {
              continue;
            }
            const set = new Set(row.roleIds ?? []);
            this.originals.set(row.userId, new Set(set));
            this.selections.set(row.userId, set);
          }
          this.isLoading = false;
          this.refreshDirtyState();
          this.changeDetectorRef.markForCheck();
        },
        error: (error) => {
          this.roles = [];
          this.rows = [];
          this.isLoading = false;
          this.toastService.error(getApiErrorMessage(error, 'Could not load the employee role matrix.'));
          this.changeDetectorRef.markForCheck();
        }
      });
  }

  private refreshDirtyState(): void {
    this.hasPendingChanges = Array.from(this.originals.keys()).some(
      (userId) => !this.setsEqual(this.selections.get(userId), this.originals.get(userId))
    );
    this.changeDetectorRef.markForCheck();
  }

  private setsEqual(a?: Set<number>, b?: Set<number>): boolean {
    const left = a ?? new Set<number>();
    const right = b ?? new Set<number>();
    if (left.size !== right.size) {
      return false;
    }
    for (const value of left) {
      if (!right.has(value)) {
        return false;
      }
    }
    return true;
  }
}
