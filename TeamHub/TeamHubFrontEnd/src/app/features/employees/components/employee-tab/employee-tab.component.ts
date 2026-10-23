import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-employee-tab',
  standalone: true,
  templateUrl: './employee-tab.component.html',
  styleUrl: './employee-tab.component.css'
})
export class EmployeeTabComponent {
  @Input() label = '';
  @Input() active = false;
  @Output() selected = new EventEmitter<void>();

  select(): void {
    this.selected.emit();
  }
}
