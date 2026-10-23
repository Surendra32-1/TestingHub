import { Routes } from '@angular/router';

import { EmployeeDirectoryComponent } from './features/employees/pages/employee-directory/employee-directory.component';
import { EmployeeAddFormComponent } from  './features/employees/pages/employee-add-form/employee-add-form'
import { EmployeeLifecycleFormComponent } from './features/employees/pages/employee-lifecycle-form/employee-lifecycle-form';
import { EmployeeLifecycleListComponent } from './features/employees/pages/employee-lifecycle-list/employee-lifecycle-list';
import { MasterSettings } from './features/master-settings/master-settings';
import { authGuard, authenticatedGuard, employeeRoleMatrixGuard, masterGuard, peopleCreateGuard, peopleReleaseGuard, peopleUpdateGuard, resignationAddGuard, resignationViewGuard, rolePermissionMatrixGuard, signedInLandingGuard, terminationAddGuard, terminationViewGuard } from './core/auth/auth.guard';
import { LoginComponent } from './features/auth/login/login.component';


export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
    canActivate: [signedInLandingGuard],
    title: 'Sign in | TeamHub'
  },
  {
    path: '',
    component: EmployeeDirectoryComponent,
    canActivate: [signedInLandingGuard],
    pathMatch: 'full',
    data: { readOnlyHome: true },
    title: 'Home | TeamHub'
  },
  {
    path: 'people',
    component: EmployeeDirectoryComponent,
    canActivate: [authenticatedGuard],
    data: { layout: 'table' },
    title: 'Employees | TeamHub'
  },
  {
    path: 'home',
    component: EmployeeDirectoryComponent,
    canActivate: [signedInLandingGuard],
    data: { readOnlyHome: true },
    title: 'Home | TeamHub'
  },
  {
    path: 'employees/add',
    component: EmployeeAddFormComponent,
    canActivate: [authGuard, peopleCreateGuard],
    title: 'AddEmployee | TeamHub'
  },
  {
    path: 'employees/edit/:employeeId',
    component: EmployeeAddFormComponent,
    canActivate: [authGuard, peopleUpdateGuard],
    title: 'Edit Employee | TeamHub'
  },
  {
    path: 'employees/release/:employeeId',
    component: EmployeeLifecycleFormComponent,
    canActivate: [authGuard, peopleReleaseGuard],
    title: 'Release Employee | TeamHub',
    data: { mode: 'release' }
  },
  {
    path: 'employees/resignations/new',
    component: EmployeeLifecycleFormComponent,
    canActivate: [authGuard, resignationAddGuard],
    title: 'New Resignation | TeamHub',
    data: { mode: 'resignation' }
  },
  {
    path: 'employees/terminations/new',
    component: EmployeeLifecycleFormComponent,
    canActivate: [authGuard, terminationAddGuard],
    title: 'New Termination | TeamHub',
    data: { mode: 'termination' }
  },
  {
    path: 'employees/resignations',
    component: EmployeeLifecycleListComponent,
    canActivate: [authGuard, resignationViewGuard],
    title: 'Resignations | TeamHub',
    data: { mode: 'resignation' }
  },
  {
    path: 'employees/terminations',
    component: EmployeeLifecycleListComponent,
    canActivate: [authGuard, terminationViewGuard],
    title: 'Terminations | TeamHub',
    data: { mode: 'termination' }
  },
  {
    path: 'employees/terminate/:employeeId',
    component: EmployeeLifecycleFormComponent,
    canActivate: [authGuard, terminationAddGuard],
    title: 'Terminate Employee | TeamHub',
    data: { mode: 'termination' }
  },
  {
    path: 'master/:masterKey',
    component: MasterSettings,
    canActivate: [authGuard, masterGuard],
    title: 'Master Settings | TeamHub'
  },

  {
    path: 'settings/employee-role-matrix',
    loadComponent: () => import('./features/rbac/employee-role-matrix/employee-role-matrix.component')
      .then((m) => m.EmployeeRoleMatrixComponent),
    canActivate: [authGuard, employeeRoleMatrixGuard],
    title: 'Employee Role Matrix | TeamHub'
  },
  {
    path: 'settings/role-permission-matrix',
    loadComponent: () => import('./features/rbac/role-permission-matrix/role-permission-matrix.component')
      .then((m) => m.RolePermissionMatrixComponent),
    canActivate: [authGuard, rolePermissionMatrixGuard],
    title: 'Role Permission Matrix | TeamHub'
  },

  {
    path: '**',
    redirectTo: ''
  }
];
