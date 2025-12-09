import { Component, input, output, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { Alert, AlertLevel } from '@myiotgrid/shared/models';
import { RelativeTimePipe } from '@myiotgrid/shared/utils';

@Component({
  selector: 'myiotgrid-alert-banner',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, RelativeTimePipe],
  templateUrl: './alert-banner.component.html',
  styleUrl: './alert-banner.component.scss'
})
export class AlertBannerComponent {
  alert = input.required<Alert>();

  acknowledge = output<string>();

  readonly alertClass = computed(() => {
    switch (this.alert().level) {
      case AlertLevel.Critical:
        return 'alert-critical';
      case AlertLevel.Warning:
        return 'alert-warning';
      case AlertLevel.Info:
        return 'alert-info';
      default:
        return 'alert-ok';
    }
  });

  readonly alertIcon = computed(() => {
    switch (this.alert().level) {
      case AlertLevel.Critical:
        return 'error';
      case AlertLevel.Warning:
        return 'warning';
      case AlertLevel.Info:
        return 'info';
      default:
        return 'check_circle';
    }
  });

  onAcknowledge(): void {
    this.acknowledge.emit(this.alert().id);
  }
}
