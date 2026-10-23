import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { HierarchyNodeView } from './models';

@Component({
  selector: 'app-hierarchy-node',
  standalone: true,
  imports: [CommonModule],
  template: `
    <article class="node-card" [class.highlighted]="selected()">
      <div class="avatar">
        <img *ngIf="person().imageUrl; else initialsTemplate" [src]="person().imageUrl" [alt]="person().name">
        <ng-template #initialsTemplate>
          <span aria-hidden="true">{{ initials() }}</span>
        </ng-template>
      </div>
      <div class="details">
        <strong [title]="person().name">{{ person().name }}</strong>
        <span class="designation" [title]="person().designation || 'No designation'">
          {{ person().designation || 'No designation' }}
        </span>
        <div class="metadata">
          <span class="department" [title]="person().department || 'No department'">
            {{ person().department || 'No department' }}
          </span>
          <small *ngIf="person().employeeNumber !== null">ENo: {{ person().employeeNumber }}</small>
        </div>
      </div>
    </article>
  `,
  styles: `
    :host { display: block; width: 240px; }
    :host(.selected-node) { width: 300px; }
    .node-card {
      display: flex;
      min-height: 84px;
      align-items: center;
      gap: 12px;
      padding: 12px;
      border: 1px solid #dbe3ed;
      border-radius: 12px;
      background: #fff;
      box-shadow: 0 3px 10px rgb(15 23 42 / 7%);
    }
    .node-card.highlighted {
      border: 2px solid #4263eb;
      background: linear-gradient(135deg, #fff 35%, #f5f8ff);
      box-shadow: 0 5px 16px rgb(66 99 235 / 14%);
    }
    .avatar {
      display: grid;
      width: 40px;
      height: 40px;
      flex: 0 0 40px;
      place-items: center;
      overflow: hidden;
      border-radius: 50%;
      background: #e8edff;
      color: #3349a6;
      font-size: 13px;
      font-weight: 700;
    }
    .avatar img { width: 100%; height: 100%; object-fit: cover; }
    .details { display: flex; min-width: 0; flex: 1; flex-direction: column; gap: 3px; }
    .details strong, .designation, .department { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .details strong { color: #172033; font-size: 13px; font-weight: 700; }
    .designation { color: #596579; font-size: 11px; }
    .metadata { display: flex; min-width: 0; align-items: center; gap: 6px; }
    .department {
      max-width: 125px;
      padding: 2px 7px;
      border-radius: 999px;
      background: #eff3f8;
      color: #4b5a70;
      font-size: 10px;
    }
    small { flex: 0 0 auto; color: #7b8798; font-size: 10px; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HierarchyNodeComponent {
  readonly person = input.required<HierarchyNodeView>();
  readonly selected = input(false);

  initials(): string {
    return this.person()
      .name.split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0].toUpperCase())
      .join('');
  }
}
