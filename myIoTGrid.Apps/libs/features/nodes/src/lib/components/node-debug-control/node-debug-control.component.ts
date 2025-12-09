import { Component, Input, Output, EventEmitter, inject, signal, OnInit, OnChanges, SimpleChanges, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { NodeDebugApiService } from '@myiotgrid/shared/data-access';
import { NodeDebugConfiguration, SetNodeDebugLevelDto, DebugLevel } from '@myiotgrid/shared/models';

/**
 * Component for controlling debug settings of a node.
 * Sprint 8: Remote Debug System
 *
 * Refactored to integrate with parent form:
 * - No longer has its own save button
 * - Respects readonly mode from parent
 * - Outputs changes for parent to save
 */
@Component({
  selector: 'myiotgrid-node-debug-control',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatFormFieldModule,
    MatSlideToggleModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatSnackBarModule
  ],
  templateUrl: './node-debug-control.component.html',
  styleUrl: './node-debug-control.component.scss'
})
export class NodeDebugControlComponent implements OnInit, OnChanges {
  @Input({ required: true }) nodeId!: string;
  @Input() readonly = false;
  @Output() configChanged = new EventEmitter<NodeDebugConfiguration>();
  @Output() debugSettingsChanged = new EventEmitter<SetNodeDebugLevelDto>();

  private readonly debugApiService = inject(NodeDebugApiService);
  private readonly snackBar = inject(MatSnackBar);

  readonly isLoading = signal(true);
  readonly config = signal<NodeDebugConfiguration | null>(null);

  // Form state
  selectedLevel: DebugLevel = DebugLevel.Normal;
  remoteLoggingEnabled = false;

  // Computed to check if there are unsaved changes
  readonly hasUnsavedChanges = computed(() => {
    const c = this.config();
    if (!c) return false;
    return c.debugLevel !== this.selectedLevel || c.enableRemoteLogging !== this.remoteLoggingEnabled;
  });

  readonly debugLevels: { value: DebugLevel; label: string; icon: string; description: string }[] = [
    {
      value: DebugLevel.Production,
      label: 'Production',
      icon: 'lock',
      description: 'Nur kritische Fehler werden geloggt'
    },
    {
      value: DebugLevel.Normal,
      label: 'Normal',
      icon: 'info',
      description: 'Standard-Logging mit Warnungen'
    },
    {
      value: DebugLevel.Debug,
      label: 'Debug',
      icon: 'bug_report',
      description: 'Ausführliches Logging für Fehlersuche'
    }
  ];

  ngOnInit(): void {
    this.loadConfiguration();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['nodeId'] && !changes['nodeId'].firstChange) {
      this.loadConfiguration();
    }
  }

  private loadConfiguration(): void {
    if (!this.nodeId) return;

    this.isLoading.set(true);
    this.debugApiService.getDebugConfiguration(this.nodeId).subscribe({
      next: (config) => {
        this.config.set(config);
        this.selectedLevel = config.debugLevel;
        this.remoteLoggingEnabled = config.enableRemoteLogging;
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading debug configuration:', error);
        this.snackBar.open('Fehler beim Laden der Debug-Konfiguration', 'Schließen', { duration: 5000 });
        this.isLoading.set(false);
      }
    });
  }

  /** Called when form values change - emit to parent */
  onValueChange(): void {
    if (this.readonly) return;

    const dto: SetNodeDebugLevelDto = {
      debugLevel: this.selectedLevel,
      enableRemoteLogging: this.remoteLoggingEnabled
    };
    this.debugSettingsChanged.emit(dto);
  }

  /**
   * Save debug settings - called by parent form
   * Returns promise so parent can await completion
   */
  async saveSettings(): Promise<void> {
    if (!this.nodeId || !this.hasUnsavedChanges()) return;

    const dto: SetNodeDebugLevelDto = {
      debugLevel: this.selectedLevel,
      enableRemoteLogging: this.remoteLoggingEnabled
    };

    return new Promise((resolve, reject) => {
      this.debugApiService.setDebugLevel(this.nodeId, dto).subscribe({
        next: (config) => {
          this.config.set(config);
          this.configChanged.emit(config);
          resolve();
        },
        error: (error) => {
          console.error('Error saving debug configuration:', error);
          reject(error);
        }
      });
    });
  }

  /** Get current settings DTO for parent to use */
  getCurrentSettings(): SetNodeDebugLevelDto {
    return {
      debugLevel: this.selectedLevel,
      enableRemoteLogging: this.remoteLoggingEnabled
    };
  }

  hasChanges(): boolean {
    const c = this.config();
    if (!c) return false;
    return c.debugLevel !== this.selectedLevel || c.enableRemoteLogging !== this.remoteLoggingEnabled;
  }

  getLevelIcon(level: DebugLevel): string {
    return this.debugLevels.find(l => l.value === level)?.icon || 'help';
  }

  getLevelLabel(level: DebugLevel): string {
    return this.debugLevels.find(l => l.value === level)?.label || level;
  }

  getLevelDescription(level: DebugLevel): string {
    return this.debugLevels.find(l => l.value === level)?.description || '';
  }

  formatDate(dateStr: string | undefined): string {
    if (!dateStr) return 'Nie';
    const date = new Date(dateStr.endsWith('Z') ? dateStr : dateStr + 'Z');
    return date.toLocaleString('de-DE', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
