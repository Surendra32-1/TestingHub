import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

export type EmployeeSearchField = 'all' | 'name' | 'designation' | 'department';

@Component({
  selector: 'app-directory-controls',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './directory-controls.component.html',
  styleUrl: './directory-controls.component.css'
})
export class DirectoryControlsComponent {
  @Input() searchTerm = '';
  @Output() searchTermChange = new EventEmitter<string>();
  @Input() searchField: EmployeeSearchField = 'all';
  @Output() searchFieldChange = new EventEmitter<EmployeeSearchField>();

  updateSearchTerm(searchTerm: string): void {
    this.searchTermChange.emit(searchTerm);
  }

  updateSearchField(searchField: EmployeeSearchField): void {
    this.searchFieldChange.emit(searchField);
  }
}
