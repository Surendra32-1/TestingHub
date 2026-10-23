import { APP_MENU, MenuItem } from './menu.config';
import { MenuContext, filterMenu } from './menu-filter';

const DEEP_TREE: readonly MenuItem[] = [
  { id: 'people', label: 'People', route: '/people', visibility: 'authenticated' },
  {
    id: 'manage',
    label: 'Manage',
    children: [
      {
        id: 'organization',
        label: 'Organization',
        children: [
          {
            id: 'cotivity',
            label: 'Cotivity',
            children: [
              {
                id: 'cotivity.employee',
                label: 'Cotivity Employee',
                route: '/master/cotivity-employee',
                permission: 'master.cotivity-employee'
              }
            ]
          }
        ]
      },
      { id: 'gender', label: 'Gender', route: '/master/gender', permission: 'master.gender' }
    ]
  },
  { id: 'login', label: 'Login', route: '/login', visibility: 'anonymous' }
];

const context = (granted: string[], isAuthenticated = true): MenuContext => ({
  isAuthenticated,
  hasPermission: (code) => granted.some((item) => item.toLowerCase() === code.toLowerCase())
});

const labels = (items: readonly MenuItem[]): string[] => items.map((item) => item.label);

describe('filterMenu', () => {
  it('shows an item only when its required permission is granted', () => {
    const visible = filterMenu(DEEP_TREE, context(['master.gender']));

    expect(labels(visible)).toEqual(['People', 'Manage']);
    expect(labels(visible[1].children!)).toEqual(['Gender']);
  });

  it('keeps every ancestor of an authorized descendant at any depth', () => {
    const visible = filterMenu(DEEP_TREE, context(['master.cotivity-employee']));
    const manage = visible[1];
    const organization = manage.children![0];
    const cotivity = organization.children![0];

    expect(labels(visible)).toEqual(['People', 'Manage']);
    expect(labels(manage.children!)).toEqual(['Organization']);
    expect(labels(organization.children!)).toEqual(['Cotivity']);
    expect(labels(cotivity.children!)).toEqual(['Cotivity Employee']);
  });

  it('removes a group whose descendants are all unauthorized', () => {
    expect(labels(filterMenu(DEEP_TREE, context([])))).toEqual(['People']);
  });

  it('removes a group that was configured empty', () => {
    const tree: readonly MenuItem[] = [
      { id: 'empty', label: 'Empty', children: [] },
      { id: 'leaf', label: 'Leaf' }
    ];

    expect(labels(filterMenu(tree, context([])))).toEqual(['Leaf']);
  });

  it('preserves the configured ordering at every level', () => {
    const visible = filterMenu(DEEP_TREE, context(['master.gender', 'master.cotivity-employee']));

    expect(labels(visible[1].children!)).toEqual(['Organization', 'Gender']);
    expect(labels(visible[1].children![0].children!)).toEqual(['Cotivity']);
  });

  it('requires every code listed in requiresAll', () => {
    const tree: readonly MenuItem[] = [{ id: 'all', label: 'All', requiresAll: ['a', 'b'] }];

    expect(labels(filterMenu(tree, context(['a'])))).toEqual([]);
    expect(labels(filterMenu(tree, context(['a', 'b'])))).toEqual(['All']);
  });

  it('accepts any code listed in requiresAny', () => {
    const tree: readonly MenuItem[] = [{ id: 'any', label: 'Any', requiresAny: ['a', 'b'] }];

    expect(labels(filterMenu(tree, context(['b'])))).toEqual(['Any']);
    expect(labels(filterMenu(tree, context(['c'])))).toEqual([]);
  });

  it('hides an item when hideIfPermission is granted (self-service vs. management)', () => {
    const tree: readonly MenuItem[] = [
      {
        id: 'resignations',
        label: 'Resignations',
        permission: 'resignations.view'
      },
      {
        id: 'apply',
        label: 'Apply Resignation',
        permission: 'resignations.add',
        hideIfPermission: 'resignations.view'
      }
    ];

    expect(labels(filterMenu(tree, context(['resignations.add'])))).toEqual(['Apply Resignation']);
    expect(labels(filterMenu(tree, context(['resignations.view'])))).toEqual(['Resignations']);
    expect(labels(filterMenu(tree, context(['resignations.add', 'resignations.view'])))).toEqual(['Resignations']);
  });

  it('gates items on the session state, never on a role name', () => {
    expect(labels(filterMenu(DEEP_TREE, context([], false)))).toEqual(['Login']);
    expect(labels(filterMenu(DEEP_TREE, context(['master.gender'], true)))).toEqual(['People', 'Manage']);
  });

  it('never mutates the original configuration', () => {
    const snapshot = JSON.stringify(DEEP_TREE);
    const frozen = JSON.parse(snapshot) as MenuItem[];
    frozen.forEach((item) => Object.freeze(item));

    const visible = filterMenu(frozen, context(['master.gender']));

    expect(JSON.stringify(frozen)).toBe(snapshot);
    expect(visible).not.toBe(frozen);
    expect(visible[1]).not.toBe(frozen[1]);
    expect(visible[1].children).not.toBe(frozen[1].children);
    expect(frozen[1].children!.length).toBe(2);
  });

  it('filters the real application menu down to a single-module grant', () => {
    const visible = filterMenu(APP_MENU, context(['master.department', 'user-roles.assign']));

    expect(labels(visible)).toEqual(['People', 'Manage', 'Settings']);
    expect(labels(visible[1].children!)).toEqual(['Department']);
    expect(labels(visible[2].children!)).toEqual(['Employee Role Matrix']);
  });
});
