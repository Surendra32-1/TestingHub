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
import { RoleOption, RolePermissionMatrixRow, SaveRolePermissionsRequest } from '../rbac.models';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, TopbarComponent, PaginationComponent],
  selector: 'app-role-permission-matrix',
  templateUrl: './role-permission-matrix.component.html',
  styleUrl: '../rbac-matrix.css'
})
export class RolePermissionMatrixComponent implements OnInit {
  private readonly rbac = inject(RbacService);
  private readonly auth = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  roles: RoleOption[] = [];
  rows: RolePermissionMatrixRow[] = [];

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
    return this.auth.hasPermission('setting.gear.RolePermission');
  }

  ngOnInit(): void {
    this.load();
  }

  isSelected(permissionId: number, roleId: number): boolean {
    return this.selections.get(permissionId)?.has(roleId) === true;
  }

  toggle(permissionId: number, roleId: number, event: Event): void {
    if (!this.canEdit || this.isSaving) {
      return;
    }

    const checked = (event.target as HTMLInputElement).checked;
    const set = this.selections.get(permissionId);
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
    if (!this.canEdit || this.isSaving || !this.hasPendingChanges) {
      return;
    }

    const assignments = this.buildPermissionAssignments();
    if (assignments.length === 0) {
      this.hasPendingChanges = false;
      return;
    }

    this.toastService.confirm(
      `Save changes to ${assignments.length} role permission assignment(s)?`,
      () => this.saveAssignments(assignments),
      'Save'
    );
  }

  cancel(): void {
    this.selections.clear();
    this.originals.forEach((set, permissionId) => this.selections.set(permissionId, new Set(set)));
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

  trackByPermission(_index: number, row: RolePermissionMatrixRow): number {
    return row.permissionId;
  }

  trackByRole(_index: number, role: RoleOption): number {
    return role.roleId;
  }

  private saveAssignments(assignments: SaveRolePermissionsRequest['assignments']): void {
    const request: SaveRolePermissionsRequest = { assignments };
    this.isSaving = true;
    this.rbac.saveRolePermissionMatrix(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toastService.success('Permission assignments saved.');
          this.isSaving = false;
          this.originals.clear();
          this.selections.clear();
          this.hasPendingChanges = false;
          this.load();
          this.auth.refreshAccessToken()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              error: () => this.toastService.error(
                'Permissions were saved, but this account session could not be refreshed. Sign out and back in to see updated navigation.'
              )
            });
        },
        error: (error) => {
          this.toastService.error(getApiErrorMessage(error, 'Could not save permission assignments.'));
          this.isSaving = false;
          this.changeDetectorRef.markForCheck();
        }
      });
  }

  private buildPermissionAssignments(): SaveRolePermissionsRequest['assignments'] {
    return Array.from(this.originals.keys())
      .filter((permissionId) => !this.setsEqual(this.selections.get(permissionId), this.originals.get(permissionId)))
      .map((permissionId) => ({
        permissionId,
        roleIds: Array.from(this.selections.get(permissionId) ?? [])
      }));
  }

  private load(): void {
    this.isLoading = true;
    this.rbac.getRolePermissionMatrix({ pageNumber: this.pageNumber, pageSize: this.pageSize, search: this.searchTerm.trim() || undefined })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (matrix) => {
          this.roles = matrix?.roles ?? [];
          this.rows = matrix?.rows ?? [];
          this.totalCount = matrix?.totalCount ?? 0;
          this.pageNumber = matrix?.pageNumber ?? this.pageNumber;
          for (const row of this.rows) {
            if (this.originals.has(row.permissionId)) {
              continue;
            }
            const set = new Set(row.roleIds ?? []);
            this.originals.set(row.permissionId, new Set(set));
            this.selections.set(row.permissionId, set);
          }
          this.isLoading = false;
          this.refreshDirtyState();
          this.changeDetectorRef.markForCheck();
        },
        error: (error) => {
          this.roles = [];
          this.rows = [];
          this.isLoading = false;
          this.toastService.error(getApiErrorMessage(error, 'Could not load the role permission matrix.'));
          this.changeDetectorRef.markForCheck();
        }
      });
  }

  private refreshDirtyState(): void {
    this.hasPendingChanges = Array.from(this.originals.keys()).some(
      (permissionId) => !this.setsEqual(this.selections.get(permissionId), this.originals.get(permissionId))
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
