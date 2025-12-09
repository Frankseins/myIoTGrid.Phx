import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable, map } from 'rxjs';
import { ConfirmDialogComponent, ConfirmDialogData } from './confirm-dialog.component';

/**
 * Service for opening confirm dialogs
 */
@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  private readonly dialog = inject(MatDialog);

  /**
   * Opens a confirm dialog
   * @returns Observable that emits true if confirmed, false if cancelled
   */
  confirm(data: ConfirmDialogData): Observable<boolean> {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data,
      autoFocus: true,
      disableClose: false
    });

    return dialogRef.afterClosed().pipe(
      map(result => result === true)
    );
  }

  /**
   * Opens a delete confirmation dialog
   */
  confirmDelete(itemName: string, itemType = 'Element'): Observable<boolean> {
    return this.confirm({
      title: `${itemType} löschen`,
      message: `Möchten Sie <strong>${itemName}</strong> wirklich löschen? Diese Aktion kann nicht rückgängig gemacht werden.`,
      confirmText: 'Löschen',
      cancelText: 'Abbrechen',
      confirmColor: 'warn',
      icon: 'delete_forever',
      iconColor: 'warn'
    });
  }

  /**
   * Opens a save confirmation dialog
   */
  confirmSave(message = 'Möchten Sie die Änderungen speichern?'): Observable<boolean> {
    return this.confirm({
      title: 'Änderungen speichern',
      message,
      confirmText: 'Speichern',
      cancelText: 'Abbrechen',
      confirmColor: 'primary',
      icon: 'save',
      iconColor: 'primary'
    });
  }

  /**
   * Opens a discard changes confirmation dialog
   */
  confirmDiscard(): Observable<boolean> {
    return this.confirm({
      title: 'Änderungen verwerfen',
      message: 'Sie haben ungespeicherte Änderungen. Möchten Sie diese wirklich verwerfen?',
      confirmText: 'Verwerfen',
      cancelText: 'Abbrechen',
      confirmColor: 'warn',
      icon: 'warning',
      iconColor: 'warn'
    });
  }

  /**
   * Opens an info dialog (only confirm button)
   */
  info(title: string, message: string): Observable<boolean> {
    return this.confirm({
      title,
      message,
      confirmText: 'OK',
      confirmColor: 'primary',
      icon: 'info',
      iconColor: 'primary',
      showCancel: false
    });
  }

  /**
   * Opens a warning dialog
   */
  warning(title: string, message: string): Observable<boolean> {
    return this.confirm({
      title,
      message,
      confirmText: 'Verstanden',
      confirmColor: 'warn',
      icon: 'warning',
      iconColor: 'warn',
      showCancel: false
    });
  }
}
