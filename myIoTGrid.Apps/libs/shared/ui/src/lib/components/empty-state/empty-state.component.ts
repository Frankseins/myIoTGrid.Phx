import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'myiotgrid-empty-state',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule],
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss'
})
export class EmptyStateComponent {
  icon = input<string>('inbox');
  title = input<string>('Keine Daten');
  message = input<string>('Es sind noch keine Daten vorhanden.');
  actionLabel = input<string | null>(null);

  actionClicked = output<void>();

  onAction(): void {
    this.actionClicked.emit();
  }
}
