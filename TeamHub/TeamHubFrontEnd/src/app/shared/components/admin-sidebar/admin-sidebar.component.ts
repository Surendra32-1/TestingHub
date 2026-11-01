import { Component, EventEmitter, HostBinding, Input, OnDestroy, OnInit, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { Subscription, filter } from 'rxjs';

import { MenuService } from '../../../core/config/menu.service';
import { MenuItem } from '../../../core/config/menu.config';

@Component({
  selector: 'app-admin-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './admin-sidebar.component.html',
  styleUrl: './admin-sidebar.component.css'
})
export class AdminSidebarComponent implements OnInit, OnDestroy {
  @Input() collapsed = false;

  @HostBinding('class.sidebar-hidden') get isHidden(): boolean {
    return this.collapsed;
  }

  private readonly menu = inject(MenuService);
  private readonly router = inject(Router);
  private readonly subscription = new Subscription();

  openBranchId: string | null = null;

  get navItems(): MenuItem[] {
    return this.menu
      .visibleMenu()
      .filter((item) => item.id !== 'employee' && item.visibility !== 'anonymous');
  }

  ngOnInit(): void {
    this.syncOpenBranchFromRoute();
    this.subscription.add(
      this.router.events
        .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
        .subscribe(() => this.syncOpenBranchFromRoute())
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  isBranchOpen(item: MenuItem): boolean {
    return this.openBranchId === item.id;
  }

  toggleBranch(item: MenuItem): void {
    this.openBranchId = this.isBranchOpen(item) ? null : item.id;
  }

  closeBranch(): void {
    this.openBranchId = null;
  }

  private syncOpenBranchFromRoute(): void {
    const activeBranch = this.navItems.find(
      (item) => (item.children?.length ?? 0) > 0 && this.isActiveItem(item)
    );
    if (activeBranch) {
      this.openBranchId = activeBranch.id;
    }
  }

  @Output() readonly navigate = new EventEmitter<void>();

  onNavigate(): void {
    this.navigate.emit();
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
}
