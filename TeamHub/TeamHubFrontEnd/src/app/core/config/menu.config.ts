
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

const masterPermissions = (prefix: string): readonly string[] => [
  `${prefix}.view`,
  `${prefix}.create`,
  `${prefix}.edit`,
  `${prefix}.delete`
];

export const MASTER_MENU_PERMISSIONS: Readonly<Record<string, readonly string[]>> = {
  department: masterPermissions('organization.department'),
  designation: masterPermissions('organization.designation'),
  'business-unit': masterPermissions('organization.businessunit'),
  'dress-size': masterPermissions('organization.dresssize'),
  'address-type': masterPermissions('organization.addresstype'),
  'blood-group': masterPermissions('organization.bloodgroup'),
  skills: masterPermissions('organization.skills'),
  'job-type': masterPermissions('organization.jobtype'),
  team: masterPermissions('organization.team'),
  gender: masterPermissions('organization.gender'),
  'cotivity-department': masterPermissions('cotivity.department'),
  'cotivity-designation': masterPermissions('cotivity.designation'),
  'cotivity-employee': masterPermissions('cotivity.employee')
};

export const APP_MENU: readonly MenuItem[] = [
  { id: 'home', label: 'Home', route: '/home', exact: true, visibility: 'anonymous' },
  {
    id: 'employee',
    label: 'People',
    route: '/people',
    exact: true,
    permission: 'people.allemployee.search.view',
    visibility: 'authenticated'
  },
  { id: 'administration', label: 'Administration', route: '/administration', exact: true, permission: 'administration.view', visibility: 'authenticated' },
  {
    id: 'resignations',
    label: 'Resignations',
    route: '/employees/resignations',
    permission: 'resignations.button',
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
    id: 'organization-master',
    label: 'Organization Master',
    visibility: 'authenticated',
    children: [
      { id: 'master.department', label: 'Department', route: '/master/department', requiresAny: MASTER_MENU_PERMISSIONS['department'] },
      { id: 'master.designation', label: 'Designation', route: '/master/designation', requiresAny: MASTER_MENU_PERMISSIONS['designation'] },
      { id: 'master.business-unit', label: 'Business Unit', route: '/master/business-unit', requiresAny: MASTER_MENU_PERMISSIONS['business-unit'] },
      { id: 'master.dress-size', label: 'Dress Size', route: '/master/dress-size', requiresAny: MASTER_MENU_PERMISSIONS['dress-size'] },
      { id: 'master.address-type', label: 'Address Type', route: '/master/address-type', requiresAny: MASTER_MENU_PERMISSIONS['address-type'] },
      { id: 'master.blood-group', label: 'Blood Group', route: '/master/blood-group', requiresAny: MASTER_MENU_PERMISSIONS['blood-group'] },
      { id: 'master.skills', label: 'Skills', route: '/master/skills', requiresAny: MASTER_MENU_PERMISSIONS['skills'] },
      { id: 'master.job-type', label: 'Job Type', route: '/master/job-type', requiresAny: MASTER_MENU_PERMISSIONS['job-type'] },
      { id: 'master.team', label: 'Team', route: '/master/team', requiresAny: MASTER_MENU_PERMISSIONS['team'] },
      { id: 'master.gender', label: 'Gender', route: '/master/gender', requiresAny: MASTER_MENU_PERMISSIONS['gender'] }
    ]
  },
  {
    id: 'cotivity',
    label: 'Cotivity',
    visibility: 'authenticated',
    children: [
      { id: 'master.cotivity-department', label: 'Cotivity Department', route: '/master/cotivity-department', requiresAny: MASTER_MENU_PERMISSIONS['cotivity-department'] },
      { id: 'master.cotivity-designation', label: 'Cotivity Designation', route: '/master/cotivity-designation', requiresAny: MASTER_MENU_PERMISSIONS['cotivity-designation'] },
      { id: 'master.cotivity-employee', label: 'Cotivity Employee', route: '/master/cotivity-employee', requiresAny: MASTER_MENU_PERMISSIONS['cotivity-employee'] }
    ]
  },
  { id: 'login', label: 'Login', route: '/login', exact: true, alignEnd: true, visibility: 'anonymous' }
];
