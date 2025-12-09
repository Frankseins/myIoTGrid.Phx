import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ReadingListItem, ReadingsList } from '@myiotgrid/shared/models';

@Component({
  selector: 'myiotgrid-readings-table',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatPaginatorModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    DatePipe,
    DecimalPipe
  ],
  templateUrl: './readings-table.component.html',
  styleUrl: './readings-table.component.scss'
})
export class ReadingsTableComponent {
  @Input() data: ReadingsList | null = null;
  @Input() loading = false;
  @Input() unit = '';

  @Output() pageChange = new EventEmitter<{ page: number; pageSize: number }>();
  @Output() exportCsv = new EventEmitter<void>();

  readonly displayedColumns = ['timestamp', 'value', 'trend'];

  onPageChange(event: PageEvent): void {
    this.pageChange.emit({
      page: event.pageIndex + 1, // Backend uses 1-based pages
      pageSize: event.pageSize
    });
  }

  getTrendIcon(direction: string | null | undefined): string {
    switch (direction) {
      case 'up': return 'arrow_upward';
      case 'down': return 'arrow_downward';
      default: return 'remove';
    }
  }

  getTrendColor(direction: string | null | undefined): string {
    switch (direction) {
      case 'up': return '#4caf50';
      case 'down': return '#f44336';
      default: return '#9e9e9e';
    }
  }

  onExportClick(): void {
    this.exportCsv.emit();
  }
}
