import { Component, Input, computed, signal, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SparklinePoint } from '@myiotgrid/shared/models';

export interface SparklineSeries {
  data: SparklinePoint[];
  color: string;
  label?: string;
}

@Component({
  selector: 'myiotgrid-sparkline-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './sparkline-chart.component.html',
  styleUrl: './sparkline-chart.component.scss'
})
export class SparklineChartComponent implements OnChanges {
  @Input() series: SparklineSeries[] = [];
  @Input() height = 40;
  @Input() showLegend = false;

  private readonly _series = signal<SparklineSeries[]>([]);
  private readonly _height = signal(40);

  readonly viewBox = computed(() => `0 0 100 ${this._height()}`);

  readonly normalizedSeries = computed(() => {
    const series = this._series();
    if (!series.length) return [];

    // Find global min/max across all series
    let globalMin = Infinity;
    let globalMax = -Infinity;

    for (const s of series) {
      for (const point of s.data) {
        if (point.value < globalMin) globalMin = point.value;
        if (point.value > globalMax) globalMax = point.value;
      }
    }

    // Add 10% padding
    const range = globalMax - globalMin;
    const padding = range * 0.1;
    globalMin -= padding;
    globalMax += padding;

    if (range === 0) {
      globalMin -= 1;
      globalMax += 1;
    }

    const height = this._height();

    return series.map(s => ({
      ...s,
      path: this.generatePath(s.data, globalMin, globalMax, height)
    }));
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['series']) {
      this._series.set(this.series);
    }
    if (changes['height']) {
      this._height.set(this.height);
    }
  }

  private generatePath(data: SparklinePoint[], min: number, max: number, height: number): string {
    if (!data.length) return '';

    const points = data.map((point, index) => {
      const x = (index / Math.max(1, data.length - 1)) * 100;
      const y = height - ((point.value - min) / (max - min)) * height;
      return { x, y };
    });

    // Generate smooth SVG path using quadratic bezier curves
    let path = `M ${points[0].x} ${points[0].y}`;

    for (let i = 1; i < points.length; i++) {
      const prev = points[i - 1];
      const curr = points[i];

      // Simple line for now (could be upgraded to bezier for smoother curves)
      path += ` L ${curr.x} ${curr.y}`;
    }

    return path;
  }
}
