import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { MenuItem } from '../../../core/config/menu.config';
import { NavMenuComponent } from './nav-menu.component';

@Component({ selector: 'app-blank-page', standalone: true, template: '' })
class BlankPageComponent {}

const TREE: readonly MenuItem[] = [
  { id: 'people', label: 'People', route: '/people', visibility: 'authenticated' },
  {
    id: 'manage',
    label: 'Manage',
    children: [
      { id: 'department', label: 'Department', route: '/master/department' },
      {
        id: 'reference-data',
        label: 'Reference Data',
        children: [
          { id: 'gender', label: 'Gender', route: '/master/gender' },
          {
            id: 'deep',
            label: 'Deeper Still',
            children: [{ id: 'blood-group', label: 'Blood Group', route: '/master/blood-group' }]
          }
        ]
      }
    ]
  },
  { id: 'login', label: 'Login', route: '/login', alignEnd: true }
];

describe('NavMenuComponent', () => {
  let fixture: ComponentFixture<NavMenuComponent>;
  let router: Router;

  const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const labelOf = (element: Element): string =>
    (element.textContent ?? '').replace(/[▼›]/g, '').replace(/\s+/g, ' ').trim();
  const childLabels = (scope: Element): string[] =>
    Array.from(scope.querySelectorAll(':scope > app-nav-menu > .menu-level > *')).map((element) => {
      const toggle = element.matches('.menu-toggle') ? element : element.querySelector(':scope > .menu-toggle');
      return labelOf(toggle ?? element);
    });
  const branch = (label: string, root: Element = host()): HTMLElement => {
    const match = Array.from(root.querySelectorAll<HTMLElement>('.menu-branch'))
      .find((element) => labelOf(element.querySelector(':scope > .menu-toggle')!) === label);
    if (!match) {
      throw new Error(`No menu group labelled "${label}".`);
    }
    return match;
  };
  const panelOf = (branchElement: Element): Element => branchElement.querySelector(':scope > .dropdown-menu')!;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [provideRouter([
        { path: 'people', component: BlankPageComponent },
        { path: 'login', component: BlankPageComponent },
        { path: 'master/department', component: BlankPageComponent },
        { path: 'master/gender', component: BlankPageComponent },
        { path: 'master/blood-group', component: BlankPageComponent }
      ])]
    }).compileComponents();

    router = TestBed.inject(Router);
    fixture = TestBed.createComponent(NavMenuComponent);
    fixture.componentRef.setInput('items', TREE);
    fixture.detectChanges();
    await router.navigateByUrl('/login');
  });

  it('renders leaves as router links and groups as expandable branches', () => {
    const barItems = Array.from(host().querySelectorAll(':scope > .menu-level > .nav-item')).map(labelOf);

    expect(barItems).toEqual(['People', 'Login']);
    expect(labelOf(branch('Manage').querySelector('.menu-toggle')!)).toBe('Manage');
    expect(childLabels(panelOf(branch('Manage')))).toEqual(['Department', 'Reference Data']);
  });

  it('renders unlimited nesting through the same component', () => {
    const referenceData = branch('Reference Data', panelOf(branch('Manage')));
    const deeperStill = branch('Deeper Still', panelOf(referenceData));

    expect(childLabels(panelOf(referenceData))).toEqual(['Gender', 'Deeper Still']);
    expect(childLabels(panelOf(deeperStill))).toEqual(['Blood Group']);
    expect(host().querySelectorAll('app-nav-menu').length).toBe(3);
  });

  it('navigates with the Angular router when an item is clicked', async () => {
    host().querySelector<HTMLAnchorElement>('a.nav-item')!.click();
    await fixture.whenStable();

    expect(router.url).toBe('/people');
  });

  it('highlights the active leaf and every ancestor group', async () => {
    await router.navigateByUrl('/master/blood-group');
    await fixture.whenStable();

    const activeLeaf = host().querySelector('a.menu-row.active');
    expect(activeLeaf ? labelOf(activeLeaf) : null).toBe('Blood Group');

    const activeGroups = Array.from(host().querySelectorAll('.menu-branch--active'))
      .map((element) => labelOf(element.querySelector(':scope > .menu-toggle')!));
    expect(activeGroups).toEqual(['Manage', 'Reference Data', 'Deeper Still']);
  });

  it('pushes an aligned item to the end of the bar', () => {
    const aligned = host().querySelector('a.nav-item--end');

    expect(aligned ? labelOf(aligned) : null).toBe('Login');
  });

  it('renders nothing when the filtered tree is empty', () => {
    fixture.componentRef.setInput('items', []);
    fixture.detectChanges();

    expect(host().querySelectorAll('.nav-item, .menu-branch').length).toBe(0);
  });
});
