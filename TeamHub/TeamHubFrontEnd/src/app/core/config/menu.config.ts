
export type MenuVisibility = 'any' | 'anonymous' | 'authenticated';

export interface MenuItem {
  readonly id: string;
  readonly label: string;
  readonly icon?: string;
  readonly route?: string;
  readonly exact?: boolean;
  readonly permission?: string;
  readonly requiresAll?: readonly string[];
  readonly requiresAny?: readonly string[];
  readonly hideIfPermission?: string;
  readonly visibility?: MenuVisibility;
  readonly alignEnd?: boolean;
  readonly children?: readonly MenuItem[];
}

export const APP_MENU: readonly MenuItem[] = [
  { id: 'home', label: 'Home', route: '/home', exact: true, visibility: 'anonymous' },
  { id: 'people', label: 'People', route: '/people', exact: true, visibility: 'authenticated' },
  {
    id: 'resignations.apply',
    label: 'Apply Resignation',
    route: '/employees/resignations/new',
    permission: 'resignations.add',
    hideIfPermission: 'resignations.view',
    visibility: 'authenticated'
  },
  {
    id: 'resignations',
    label: 'Resignations',
    route: '/employees/resignations',
    permission: 'resignations.view',
    visibility: 'authenticated'
  },
  {
    id: 'terminations',
    label: 'Terminations',
    route: '/employees/terminations',
    permission: 'terminations.view',
    visibility: 'authenticated'
  },
  {
    id: 'manage',
    label: 'Manage',
    icon: 'settings',
    visibility: 'authenticated',
    children: [
      { id: 'master.department', label: 'Department', route: '/master/department', permission: 'master.department' },
      { id: 'master.designation', label: 'Designation', route: '/master/designation', permission: 'master.designation' },
      { id: 'master.business-unit', label: 'Business Unit', route: '/master/business-unit', permission: 'master.business-unit' },
      { id: 'master.dress-size', label: 'Dress Size', route: '/master/dress-size', permission: 'master.dress-size' },
      {
        id: 'master.cotivity-department',
        label: 'Cotivity Department',
        route: '/master/cotivity-department',
        permission: 'master.cotivity-department'
      },
      {
        id: 'master.cotivity-designation',
        label: 'Cotivity Designation',
        route: '/master/cotivity-designation',
        permission: 'master.cotivity-designation'
      },
      {
        id: 'master.cotivity-employee',
        label: 'Cotivity Employee',
        route: '/master/cotivity-employee',
        permission: 'master.cotivity-employee'
      },
      { id: 'master.role', label: 'Role', route: '/master/role', permission: 'master.role' },
      { id: 'permissions.manage', label: 'Manage Permissions', route: '/master/permission', permission: 'permissions.manage' },
      { id: 'master.address-type', label: 'Address Type', route: '/master/address-type', permission: 'master.address-type' },
      { id: 'master.blood-group', label: 'Blood Group', route: '/master/blood-group', permission: 'master.blood-group' },
      { id: 'master.skills', label: 'Skills', route: '/master/skills', permission: 'master.skills' },
      { id: 'master.job-type', label: 'Job Type', route: '/master/job-type', permission: 'master.job-type' },
      { id: 'master.team', label: 'Team', route: '/master/team', permission: 'master.team' },
      { id: 'master.gender', label: 'Gender', route: '/master/gender', permission: 'master.gender' }
    ]
  },
  {
    id: 'settings',
    label: 'Settings',
    icon: 'shield',
    visibility: 'authenticated',
    children: [
      {
        id: 'settings.employee-role-matrix',
        label: 'Employee Role Matrix',
        route: '/settings/employee-role-matrix',
        permission: 'user-roles.assign'
      },
      {
        id: 'settings.role-permission-matrix',
        label: 'Role Permission Matrix',
        route: '/settings/role-permission-matrix',
        permission: 'role-permissions.manage'
      }
    ]
  },
  { id: 'login', label: 'Login', route: '/login', exact: true, alignEnd: true, visibility: 'anonymous' }
];
