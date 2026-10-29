import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  Injector,
  afterNextRender,
  computed,
  effect,
  inject,
  input,
  model,
  output,
  signal
} from '@angular/core';
import { Subscription } from 'rxjs';

import { getApiErrorMessage } from '../../../../shared/services/api-error-message';
import { ToastService } from '../../../../shared/services/toast.service';
import {
  CotivityHierarchyPerson,
  EmployeeHierarchyPerson,
  EmployeeHierarchyResponse,
  EmployeeHierarchyTreeNode,
  HierarchyConnection,
  HierarchyNodeView
} from './models';
import { EmployeeHierarchyService } from './employee-hierarchy.service';
import { HierarchyConnectorPath, HierarchyConnectorService } from './hierarchy-connector.service';
import { HierarchyNodeComponent } from './hierarchy-node.component';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-employee-hierarchy',
  standalone: true,
  imports: [CommonModule, HierarchyNodeComponent],
  templateUrl: './employee-hierarchy.component.html',
  styleUrl: './employee-hierarchy.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmployeeHierarchyComponent implements AfterViewInit {
  readonly employeeId = model.required<number>();
  readonly directOnly = input(false);
  readonly publicDirectory = input(false);
  readonly back = output<void>();
  readonly hierarchy = signal<EmployeeHierarchyResponse | null>(null);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly connectors = signal<HierarchyConnectorPath[]>([]);
  readonly visibleManagers = computed(() => {
    const managers = this.hierarchy()?.employeeManagers ?? [];
    return this.directOnly() && managers.length > 1 ? managers.slice(-1) : managers;
  });
  readonly reportingTree = computed(() =>
    this.createReportingTree(this.hierarchy()?.reportingHierarchy ?? [], this.employeeId())
  );
  readonly cotivityNodes = computed(() =>
    this.directOnly()
      ? []
      : (this.hierarchy()?.cotivityHierarchy ?? []).map((person) => this.cotivityNode(person))
  );

  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);
  private resizeObserver: ResizeObserver | null = null;
  private refreshFrame: number | null = null;
  private readonly observedElements = new WeakSet<Element>();
  private readonly employeeNodeCache = new WeakMap<EmployeeHierarchyPerson, HierarchyNodeView>();

  constructor(
    private readonly hierarchyService: EmployeeHierarchyService,
    private readonly connectorService: HierarchyConnectorService,
    private readonly toastService: ToastService
  ) {
    effect((onCleanup) => {
      const employeeId = this.employeeId();
      if (!Number.isInteger(employeeId) || employeeId <= 0) {
        return;
      }

      this.hierarchy.set(null);
      this.errorMessage.set(null);
      this.isLoading.set(true);

      const request: Subscription = this.hierarchyService.getHierarchy(employeeId, this.publicDirectory()).subscribe({
        next: (hierarchy) => {
          this.hierarchy.set(hierarchy);
          this.isLoading.set(false);
        },
        error: (error) => {
          const message = getApiErrorMessage(error, 'Could not load the employee hierarchy.');
          this.errorMessage.set(message);
          this.isLoading.set(false);
          this.toastService.error(message);
        }
      });

      onCleanup(() => request.unsubscribe());
    });

    effect(() => {
      this.hierarchy();
      afterNextRender(() => this.scheduleConnectorRefresh(), { injector: this.injector });
    });

    this.destroyRef.onDestroy(() => {
      this.resizeObserver?.disconnect();
      if (this.refreshFrame !== null) {
        cancelAnimationFrame(this.refreshFrame);
      }
    });
  }

  ngAfterViewInit(): void {
    if (typeof ResizeObserver !== 'undefined') {
      this.resizeObserver = new ResizeObserver(() => this.scheduleConnectorRefresh());
      this.observeChartElements();
    }
    this.scheduleConnectorRefresh();
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    this.scheduleConnectorRefresh();
  }

  trackByEmployeeId(_: number, node: EmployeeHierarchyPerson): number {
    return node.employeeId;
  }

  trackByNodeId(_: number, node: HierarchyNodeView): string {
    return node.id;
  }

  selectEmployee(employeeId: number): void {
    if (employeeId !== this.employeeId()) {
      this.employeeId.set(employeeId);
    }
  }

  trackByConnectorId(_: number, connector: HierarchyConnectorPath): string {
    return connector.id;
  }

  employeeNode(person: EmployeeHierarchyPerson): HierarchyNodeView {
    const cached = this.employeeNodeCache.get(person);
    if (cached) {
      return cached;
    }

    const view: HierarchyNodeView = {
      id: `employee-${person.employeeId}`,
      name: person.employeeName,
      designation: person.designationName,
      department: person.departmentName,
      employeeNumber: person.employeeNumber,
      imageUrl: this.imageUrl(person.imagePath)
    };
    this.employeeNodeCache.set(person, view);
    return view;
  }

  private cotivityNode(person: CotivityHierarchyPerson): HierarchyNodeView {
    return {
      id: `cotivity-${person.cotivityEmployeeId}`,
      name: person.cotivityEmployeeName,
      designation: person.cotivityDesignationName,
      department: person.cotivityDepartmentName,
      employeeNumber: null,
      imageUrl: this.imageUrl(person.imagePath)
    };
  }

  private imageUrl(path: string | null): string | null {
    if (!path) {
      return null;
    }

    if (/^(data:|https?:\/\/)/i.test(path)) {
      return path;
    }

    return `${environment.apiUrl}${path.replace(/^[/\\]+/, '').replace(/\\/g, '/')}`;
  }

  private createReportingTree(
    flatNodes: EmployeeHierarchyPerson[],
    selectedEmployeeId: number
  ): EmployeeHierarchyTreeNode | null {
    const scopedNodes = this.directOnly()
      ? flatNodes.filter((node) => node.employeeId === selectedEmployeeId || node.parentId === selectedEmployeeId)
      : flatNodes;
    const nodes = new Map<number, EmployeeHierarchyTreeNode>();
    for (const node of scopedNodes) {
      nodes.set(node.employeeId, { ...node, reports: [] });
    }

    const root = nodes.get(selectedEmployeeId);
    if (!root) {
      return null;
    }

    for (const node of flatNodes) {
      if (node.employeeId === selectedEmployeeId || node.parentId === null) {
        continue;
      }

      const parent = nodes.get(node.parentId);
      const child = nodes.get(node.employeeId);
      if (parent && child && node.level === parent.level + 1) {
        parent.reports.push(child);
      }
    }

    const pending = [root];
    while (pending.length > 0) {
      const node = pending.pop()!;
      node.reports.sort((left, right) =>
        left.employeeName.localeCompare(right.employeeName, undefined, { sensitivity: 'base' })
      );
      pending.push(...node.reports);
    }

    return root;
  }

  private connectionSpecs(): HierarchyConnection[] {
    const hierarchy = this.hierarchy();
    if (!hierarchy) {
      return [];
    }

    const employeeNodes = new Map<number, EmployeeHierarchyPerson>();
    for (const person of [
      ...hierarchy.employeeManagers,
      hierarchy.selectedEmployee,
      ...hierarchy.reportingHierarchy
    ]) {
      employeeNodes.set(person.employeeId, person);
    }

    const connections: HierarchyConnection[] = [];
    const seen = new Set<string>();
    for (const person of employeeNodes.values()) {
      const parent = person.parentId === null ? undefined : employeeNodes.get(person.parentId);
      if (!parent || parent.level >= person.level) {
        continue;
      }

      const id = `employee-${parent.employeeId}->employee-${person.employeeId}`;
      if (!seen.has(id)) {
        connections.push({
          id,
          fromId: `employee-${parent.employeeId}`,
          toId: `employee-${person.employeeId}`
        });
        seen.add(id);
      }
    }

    const cotivityNodes = new Map<number, CotivityHierarchyPerson>();
    for (const person of hierarchy.cotivityHierarchy) {
      cotivityNodes.set(person.cotivityEmployeeId, person);
    }

    for (const person of cotivityNodes.values()) {
      const parent = person.parentId === null ? undefined : cotivityNodes.get(person.parentId);
      if (!parent || parent.level >= person.level) {
        continue;
      }

      const id = `cotivity-${parent.cotivityEmployeeId}->cotivity-${person.cotivityEmployeeId}`;
      if (!seen.has(id)) {
        connections.push({
          id,
          fromId: `cotivity-${parent.cotivityEmployeeId}`,
          toId: `cotivity-${person.cotivityEmployeeId}`
        });
        seen.add(id);
      }
    }

    const currentCotivityEmployee = hierarchy.cotivityHierarchy.find((person) => person.level === 0);
    if (currentCotivityEmployee) {
      connections.push({
        id: `cotivity-${currentCotivityEmployee.cotivityEmployeeId}->employee-${hierarchy.selectedEmployee.employeeId}`,
        fromId: `cotivity-${currentCotivityEmployee.cotivityEmployeeId}`,
        toId: `employee-${hierarchy.selectedEmployee.employeeId}`,
        route: 'direct'
      });
    }

    return connections;
  }

  private observeChartElements(): void {
    const canvas = this.elementRef.nativeElement.querySelector<HTMLElement>('.chart-canvas');
    if (!canvas || !this.resizeObserver) {
      return;
    }

    if (!this.observedElements.has(canvas)) {
      this.resizeObserver.observe(canvas);
      this.observedElements.add(canvas);
    }
    canvas.querySelectorAll<HTMLElement>('[data-node-id]').forEach((node) => {
      if (!this.observedElements.has(node)) {
        this.resizeObserver?.observe(node);
        this.observedElements.add(node);
      }
    });
  }

  private scheduleConnectorRefresh(): void {
    if (this.refreshFrame !== null) {
      cancelAnimationFrame(this.refreshFrame);
    }

    this.refreshFrame = requestAnimationFrame(() => {
      this.refreshFrame = null;
      const canvas = this.elementRef.nativeElement.querySelector<HTMLElement>('.chart-canvas');
      const svg = this.elementRef.nativeElement.querySelector<SVGSVGElement>('.connector-layer');
      if (!canvas || !svg) {
        this.connectors.set([]);
        return;
      }

      this.observeChartElements();
      this.connectors.set(this.connectorService.measure(canvas, svg, this.connectionSpecs()));
    });
  }
}
