import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { map } from 'rxjs';

import { MenuItem } from '../../../core/config/menu.config';

@Component({
  selector: 'app-nav-menu',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './nav-menu.component.html',
  styleUrl: './nav-menu.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NavMenuComponent {
  readonly items = input.required<readonly MenuItem[]>();

  readonly level = input(0);

  private readonly router = inject(Router);

  private readonly currentUrl = toSignal(this.router.events.pipe(map(() => this.router.url)), {
    initialValue: this.router.url
  });

  isBranchActive(item: MenuItem): boolean {
    const url = this.stripUrl(this.currentUrl());
    return (item.children ?? []).some((child) =>
      child.route ? this.routeMatches(child.route, url) : this.isBranchActive(child)
    );
  }

  private routeMatches(route: string, url: string): boolean {
    const target = this.stripUrl(route);
    return url === target || url.startsWith(`${target}/`);
  }

  private stripUrl(value: string): string {
    return value.split('?')[0].split('#')[0];
  }
}
