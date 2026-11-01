import { ChangeDetectorRef, Component, ElementRef, HostListener, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { MenuService } from '../../../core/config/menu.service';
import { NavbarSelectionService } from '../../../core/config/navbar-selection.service';
import { ProfileService } from '../../../features/profile/profile.service';
import { AdminNavbarComponent } from '../admin-navbar/admin-navbar.component';
import { NavMenuComponent } from '../nav-menu/nav-menu.component';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [
    NavMenuComponent,
    AdminNavbarComponent
  ],
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.css'
})
export class TopbarComponent implements OnInit {
  readonly menu = inject(MenuService);
  readonly navbarSelection = inject(NavbarSelectionService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly profileService = inject(ProfileService);
  private readonly elementRef = inject(ElementRef<HTMLElement>);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  profileImage: string | null = null;
  profileUsername: string | null = null;
  isProfileMenuOpen = false;
  isSettingsMenuOpen = false;

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
    // The admin navbar owns its own profile section; don't double-load the summary.
    if (this.navbarSelection.useAdminNavbar()) {
      return;
    }
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
  closeProfileMenuOnOutsideClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target as Node)) {
      this.isProfileMenuOpen = false;
      this.isSettingsMenuOpen = false;
    }
  }

  @HostListener('document:keydown.escape')
  closeMenusOnEscape(): void {
    this.isProfileMenuOpen = false;
    this.isSettingsMenuOpen = false;
  }

  toggleProfileMenu(): void {
    this.isProfileMenuOpen = !this.isProfileMenuOpen;
    this.isSettingsMenuOpen = false;
    if (this.isProfileMenuOpen) {
      this.loadProfileSummary();
    }
  }

  toggleSettingsMenu(): void {
    this.isSettingsMenuOpen = !this.isSettingsMenuOpen;
    this.isProfileMenuOpen = false;
  }

  openEmployeeRoleMatrix(): void {
    this.isSettingsMenuOpen = false;
    void this.router.navigateByUrl('/settings/employee-role-matrix');
  }

  openRolePermissionMatrix(): void {
    this.isSettingsMenuOpen = false;
    void this.router.navigateByUrl('/settings/role-permission-matrix');
  }

  openRoleMaster(): void {
    this.isSettingsMenuOpen = false;
    void this.router.navigateByUrl('/master/role');
  }

  openPermissionsMaster(): void {
    this.isSettingsMenuOpen = false;
    void this.router.navigateByUrl('/master/permission');
  }

  editProfile(): void {
    this.isProfileMenuOpen = false;
    void this.router.navigateByUrl('/profile/edit');
  }

  logout(): void {
    this.isProfileMenuOpen = false;
    this.isSettingsMenuOpen = false;
    this.authService.logout().subscribe({
      next: () => void this.router.navigateByUrl('/login'),
      error: () => void this.router.navigateByUrl('/login')
    });
  }
}
