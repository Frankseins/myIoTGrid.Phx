import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

/**
 * Data for the confirm dialog
 */
export interface ConfirmDialogData {
  /** Dialog title */
  title: string;

  /** Dialog message (can contain HTML) */
  message: string;

  /** Confirm button text */
  confirmText?: string;

  /** Cancel button text */
  cancelText?: string;

  /** Confirm button color */
  confirmColor?: 'primary' | 'accent' | 'warn';

  /** Icon to display */
  icon?: string;

  /** Icon color class */
  iconColor?: 'primary' | 'accent' | 'warn';

  /** Whether to show the cancel button */
  showCancel?: boolean;
}

/**
 * Default dialog data
 */
const DEFAULT_DATA: Partial<ConfirmDialogData> = {
  confirmText: 'Bestätigen',
  cancelText: 'Abbrechen',
  confirmColor: 'primary',
  icon: 'help_outline',
  iconColor: 'primary',
  showCancel: true
};

@Component({
  selector: 'myiotgrid-confirm-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss'
})
export class ConfirmDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<ConfirmDialogComponent>);
  readonly data: ConfirmDialogData = { ...DEFAULT_DATA, ...inject(MAT_DIALOG_DATA) };

  onConfirm(): void {
    this.dialogRef.close(true);
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }
}
