import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, HostListener, ChangeDetectorRef } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ReadingListItem, ReadingsList } from '@myiotgrid/shared/models';
import { SensorValuePipe } from '@myiotgrid/shared/utils';
import { ReadingCardComponent } from '../reading-card/reading-card.component';

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
    SensorValuePipe,
    ReadingCardComponent
  ],
  templateUrl: './readings-table.component.html',
  styleUrl: './readings-table.component.scss'
})
export class ReadingsTableComponent implements OnInit, OnDestroy {
  // Responsive breakpoint
  private readonly MOBILE_BREAKPOINT = 768;
  isMobileView = false;

  @Input() data: ReadingsList | null = null;
  @Input() loading = false;
  @Input() unit = '';
  @Input() measurementType = '';

  // Additional inputs for mobile card view
  @Input() measurementLabel = 'Messwert';
  @Input() measurementIcon = 'sensors';
  @Input() color = '#1976d2';
  @Input() sensorName = '';
  @Input() nodeName = '';

  constructor(private cd: ChangeDetectorRef) {}

  @Output() pageChange = new EventEmitter<{ page: number; pageSize: number }>();
  @Output() exportCsv = new EventEmitter<void>();

  readonly displayedColumns = ['timestamp', 'value', 'trend'];

  ngOnInit(): void {
    this.checkViewport();
  }

  ngOnDestroy(): void {
    // Cleanup if needed
  }

  @HostListener('window:resize')
  onResize(): void {
    this.checkViewport();
  }

  private checkViewport(): void {
    const wasMobile = this.isMobileView;
    this.isMobileView = window.innerWidth < this.MOBILE_BREAKPOINT;
    if (wasMobile !== this.isMobileView) {
      this.cd.markForCheck();
    }
  }

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

  /**
   * Get unit for a reading (use reading's unit if available, fallback to component input)
   */
  getReadingUnit(reading: ReadingListItem): string {
    return reading.unit || this.unit;
  }
}
