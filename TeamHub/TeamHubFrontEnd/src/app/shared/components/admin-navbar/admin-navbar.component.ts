import { ChangeDetectorRef, Component, ElementRef, HostListener, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { MenuService } from '../../../core/config/menu.service';
import { MenuItem } from '../../../core/config/menu.config';
import { ProfileService } from '../../../features/profile/profile.service';

/**
 * Navbar 2 (Administration). Fully independent from the employee TopbarComponent:
 * its own markup, styling, positioning, active highlighting, profile and settings
 * sections. It reuses shared services (MenuService/AuthService/ProfileService) so
 * visibility stays driven by the existing RolePermissions pipeline, and it drops
 * the People entry (People belongs to Navbar 1 only).
 */
@Component({
  selector: 'app-admin-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './admin-navbar.component.html',
  styleUrl: './admin-navbar.component.css'
})
export class AdminNavbarComponent implements OnInit {
  private readonly menu = inject(MenuService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly profileService = inject(ProfileService);
  private readonly elementRef = inject(ElementRef<HTMLElement>);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  profileImage: string | null = null;
  profileUsername: string | null = null;
  isProfileMenuOpen = false;
  isSettingsMenuOpen = false;
  openBranchId: string | null = null;

  /** Admin navigation = permission-visible menu minus People and anonymous entries. */
  get adminItems(): MenuItem[] {
    return this.menu
      .visibleMenu()
      .filter((item) => item.id !== 'employee' && item.visibility !== 'anonymous');
  }

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated;
  }

  get canEditProfile(): boolean {
    return this.authService.hasPermission('profile.edit');
  }

  get canOpenEmployeeRoleMatrix(): boolean {
    return this.authService.hasPermission('setting.gear.EmployeeRole');
  }

  get canOpenRolePermissionMatrix(): boolean {
    return this.authService.hasPermission('setting.gear.RolePermission');
  }

  get canOpenRoleMaster(): boolean {
    return this.authService.hasPermission('setting.gear.role.view');
  }

  get canOpenPermissionsMaster(): boolean {
    return this.authService.hasPermission('setting.gear.manage.permissions.view');
  }

  get canOpenSettingsMenu(): boolean {
    return this.canOpenEmployeeRoleMatrix
      || this.canOpenRolePermissionMatrix
      || this.canOpenRoleMaster
      || this.canOpenPermissionsMaster;
  }

  ngOnInit(): void {
    this.loadProfileSummary();
  }

  private loadProfileSummary(): void {
    if (!this.isAuthenticated) {
      return;
    }

    this.profileService.getMySummary().subscribe({
      next: (summary) => {
        this.profileImage = summary.imageBase64;
        this.profileUsername = summary.employeeName || 'My Account';
        this.changeDetectorRef.markForCheck();
      },
      error: () => {
        this.profileImage = null;
        this.profileUsername = 'My Account';
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  @HostListener('document:click', ['$event'])
  closeOnOutsideClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target as Node)) {
      this.closeAll();
    }
  }

  @HostListener('document:keydown.escape')
  closeOnEscape(): void {
    this.closeAll();
  }

  closeAll(): void {
    this.isProfileMenuOpen = false;
    this.isSettingsMenuOpen = false;
    this.openBranchId = null;
  }

  isBranchOpen(item: MenuItem): boolean {
    return this.openBranchId === item.id;
  }

  toggleBranch(item: MenuItem): void {
    this.openBranchId = this.isBranchOpen(item) ? null : item.id;
    this.isSettingsMenuOpen = false;
    this.isProfileMenuOpen = false;
  }

  isActiveItem(item: MenuItem): boolean {
    const url = this.stripUrl(this.router.url);
    if (item.route) {
      return this.routeMatches(item.route, url);
    }
    return (item.children ?? []).some((child) => child.route && this.routeMatches(child.route, url));
  }

  private routeMatches(route: string, url: string): boolean {
    const target = this.stripUrl(route);
    return url === target || url.startsWith(`${target}/`);
  }

  private stripUrl(value: string): string {
    return value.split('?')[0].split('#')[0];
  }

  toggleProfileMenu(): void {
    this.isProfileMenuOpen = !this.isProfileMenuOpen;
    this.isSettingsMenuOpen = false;
    this.openBranchId = null;
    if (this.isProfileMenuOpen) {
      this.loadProfileSummary();
    }
  }

  toggleSettingsMenu(): void {
    this.isSettingsMenuOpen = !this.isSettingsMenuOpen;
    this.isProfileMenuOpen = false;
    this.openBranchId = null;
  }

  openRoleMaster(): void {
    this.isSettingsMenuOpen = false;
    void this.router.navigateByUrl('/master/role');
  }

  openPermissionsMaster(): void {
    this.isSettingsMenuOpen = false;
    void this.router.navigateByUrl('/master/permission');
  }

  openEmployeeRoleMatrix(): void {
    this.isSettingsMenuOpen = false;
    void this.router.navigateByUrl('/settings/employee-role-matrix');
  }

  openRolePermissionMatrix(): void {
    this.isSettingsMenuOpen = false;
    void this.router.navigateByUrl('/settings/role-permission-matrix');
  }

  editProfile(): void {
    this.isProfileMenuOpen = false;
    void this.router.navigateByUrl('/profile/edit');
  }

  logout(): void {
    this.closeAll();
    this.authService.logout().subscribe({
      next: () => void this.router.navigateByUrl('/login'),
      error: () => void this.router.navigateByUrl('/login')
    });
  }
}
