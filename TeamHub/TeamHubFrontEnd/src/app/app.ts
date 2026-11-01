import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { NavbarSelectionService } from './core/config/navbar-selection.service';
import { AdminLayoutComponent } from './shared/components/admin-layout/admin-layout.component';
import { ToastHostComponent } from './shared/components/toast-host/toast-host.component';
import { TopbarComponent } from './shared/components/topbar/topbar.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, TopbarComponent, AdminLayoutComponent, ToastHostComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  readonly navbarSelection = inject(NavbarSelectionService);
}
