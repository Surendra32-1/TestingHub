import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pagination.component.html',
  styleUrl: './pagination.component.css'
})
export class PaginationComponent {
  @Input() pageNumber = 1;
  @Input() pageSize = 5;
  @Input() totalCount = 0;
  @Input() isLoading = false;
  @Output() pageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  get startItem(): number {
    return this.totalCount === 0 ? 0 : (this.pageNumber - 1) * this.pageSize + 1;
  }

  get endItem(): number {
    return Math.min(this.pageNumber * this.pageSize, this.totalCount);
  }

  previous(): void {
    if (this.pageNumber > 1 && !this.isLoading) {
      this.pageChange.emit(this.pageNumber - 1);
    }
  }

  next(): void {
    if (this.pageNumber < this.totalPages && !this.isLoading) {
      this.pageChange.emit(this.pageNumber + 1);
    }
  }

  changePageSize(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.pageSizeChange.emit(Number(target.value));
  }
}