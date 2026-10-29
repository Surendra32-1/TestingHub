import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Department } from '../../models/employee-master.model';

export type EmployeeSearchField = 'all' | 'name' | 'designation' | 'department';

@Component({
  selector: 'app-directory-controls',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './directory-controls.component.html',
  styleUrl: './directory-controls.component.css'
})
export class DirectoryControlsComponent {
  @Input() searchTerm = '';
  @Output() searchTermChange = new EventEmitter<string>();
  @Input() searchField: EmployeeSearchField = 'all';
  @Output() searchFieldChange = new EventEmitter<EmployeeSearchField>();
  @Input() departmentOptions: Department[] | null = null;
  @Input() selectedDepartmentId: number | null = null;
  @Output() departmentChange = new EventEmitter<number | null>();

  updateSearchTerm(searchTerm: string): void {
    this.searchTermChange.emit(searchTerm);
  }

  updateSearchField(searchField: EmployeeSearchField): void {
    this.searchFieldChange.emit(searchField);
  }

  updateDepartment(value: string): void {
    this.departmentChange.emit(value === '' ? null : Number(value));
  }
}
