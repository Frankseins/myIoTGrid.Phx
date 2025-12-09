import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { ChartInterval, ChartIntervalLabels } from '@myiotgrid/shared/models';

@Component({
  selector: 'myiotgrid-interval-selector',
  standalone: true,
  imports: [CommonModule, MatButtonToggleModule],
  templateUrl: './interval-selector.component.html',
  styleUrl: './interval-selector.component.scss'
})
export class IntervalSelectorComponent {
  @Input() selectedInterval: ChartInterval = ChartInterval.OneDay;
  @Output() intervalChange = new EventEmitter<ChartInterval>();

  readonly intervals = [
    ChartInterval.OneHour,
    ChartInterval.OneDay,
    ChartInterval.OneWeek,
    ChartInterval.OneMonth,
    ChartInterval.ThreeMonths,
    ChartInterval.SixMonths,
    ChartInterval.OneYear
  ];

  readonly labels = ChartIntervalLabels;

  onIntervalChange(interval: ChartInterval): void {
    this.selectedInterval = interval;
    this.intervalChange.emit(interval);
  }
}
