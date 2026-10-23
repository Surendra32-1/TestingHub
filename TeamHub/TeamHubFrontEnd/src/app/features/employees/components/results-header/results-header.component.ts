import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-results-header',
  standalone: true,
  imports:[NgIf, RouterLink],
  templateUrl: './results-header.component.html',
  styleUrl: './results-header.component.css'
})
export class ResultsHeaderComponent {
  @Input() isLoading = false;
  @Input() count = 0;
  @Input() showAddButton = true;
}
