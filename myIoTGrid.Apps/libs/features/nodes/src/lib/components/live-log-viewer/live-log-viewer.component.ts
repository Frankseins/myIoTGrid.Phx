import { Component, Input, inject, signal, OnInit, OnDestroy, OnChanges, SimpleChanges, ViewChild, ElementRef, AfterViewChecked, TemplateRef, ViewContainerRef, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Overlay, OverlayConfig, OverlayModule, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal, PortalModule } from '@angular/cdk/portal';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';

import { NodeDebugApiService, SignalRService } from '@myiotgrid/shared/data-access';
import { NodeDebugLog, DebugLevel, LogCategory, DebugLogFilter } from '@myiotgrid/shared/models';
import { ConfirmDialogComponent } from '@myiotgrid/shared/ui';

/**
 * Live Log Viewer component for real-time debug logs.
 * Sprint 8: Remote Debug System
 * Refactored with GenericListComponent-style Filter Drawer
 */
@Component({
  selector: 'myiotgrid-live-log-viewer',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatSelectModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    MatTooltipModule,
    MatChipsModule,
    MatSlideToggleModule,
    MatSnackBarModule,
    MatDialogModule,
    MatToolbarModule,
    OverlayModule,
    PortalModule
  ],
  templateUrl: './live-log-viewer.component.html',
  styleUrl: './live-log-viewer.component.scss'
})
export class LiveLogViewerComponent implements OnInit, OnDestroy, OnChanges, AfterViewChecked {
  @Input({ required: true }) nodeId!: string;
  @Input() drawerTopOffset = 64;
  @Input() drawerWidth = '320px';

  @ViewChild('logContainer') logContainer!: ElementRef<HTMLDivElement>;
  @ViewChild('filterHost') filterHostTpl!: TemplateRef<unknown>;
  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;

  private readonly debugApiService = inject(NodeDebugApiService);
  private readonly signalRService = inject(SignalRService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly overlay = inject(Overlay);
  private readonly vcr = inject(ViewContainerRef);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly isLoading = signal(true);
  readonly isClearing = signal(false);
  readonly logs = signal<NodeDebugLog[]>([]);
  readonly isPaused = signal(false);
  readonly autoScroll = signal(true);

  // Filter Drawer
  private filterOverlayRef: OverlayRef | null = null;
  isFilterOpen = false;

  // Search
  isSearchOpen = false;
  globalFilter = '';
  private search$ = new Subject<string>();

  // Filters
  selectedLevel: DebugLevel | null = null;
  selectedCategory: LogCategory | null = null;

  private shouldScrollToBottom = false;
  private readonly MAX_LOGS = 500; // Limit displayed logs

  readonly debugLevels: { value: DebugLevel; label: string; color: string }[] = [
    { value: DebugLevel.Production, label: 'Production', color: '#4caf50' },
    { value: DebugLevel.Normal, label: 'Normal', color: '#2196f3' },
    { value: DebugLevel.Debug, label: 'Debug', color: '#ff9800' }
  ];

  readonly logCategories: { value: LogCategory; label: string; icon: string }[] = [
    { value: LogCategory.System, label: 'System', icon: 'computer' },
    { value: LogCategory.Hardware, label: 'Hardware', icon: 'memory' },
    { value: LogCategory.Network, label: 'Network', icon: 'wifi' },
    { value: LogCategory.Sensor, label: 'Sensor', icon: 'sensors' },
    { value: LogCategory.GPS, label: 'GPS', icon: 'location_on' },
    { value: LogCategory.API, label: 'API', icon: 'api' },
    { value: LogCategory.Storage, label: 'Storage', icon: 'storage' },
    { value: LogCategory.Error, label: 'Error', icon: 'error' }
  ];

  ngOnInit(): void {
    this.loadRecentLogs();
    this.setupSignalR();

    // Debounced search
    this.search$.pipe(distinctUntilChanged(), debounceTime(200)).subscribe(term => {
      this.globalFilter = term ?? '';
      this.cdr.markForCheck();
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['nodeId'] && !changes['nodeId'].firstChange) {
      this.logs.set([]);
      this.loadRecentLogs();
      this.setupSignalR();
    }
  }

  ngOnDestroy(): void {
    this.cleanupSignalR();
    this.teardownFilterOverlay();
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom && this.autoScroll() && this.logContainer) {
      this.scrollToBottom();
      this.shouldScrollToBottom = false;
    }
  }

  private loadRecentLogs(): void {
    if (!this.nodeId) return;

    this.isLoading.set(true);
    this.debugApiService.getRecentLogs(this.nodeId, 100).subscribe({
      next: (logs) => {
        // Reverse to show oldest first, newest at bottom
        this.logs.set(logs.reverse());
        this.shouldScrollToBottom = true;
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading logs:', error);
        this.snackBar.open('Fehler beim Laden der Logs', 'Schließen', { duration: 5000 });
        this.isLoading.set(false);
      }
    });
  }

  private async setupSignalR(): Promise<void> {
    if (!this.nodeId) return;

    try {
      if (this.signalRService.connectionState() !== 'connected') {
        await this.signalRService.startConnection();
      }

      // Join debug group for this node
      await this.signalRService.joinDebugGroup(this.nodeId);

      // Subscribe to debug log events
      this.signalRService.onDebugLogReceived((log: NodeDebugLog) => {
        if (log.nodeId === this.nodeId && !this.isPaused()) {
          this.addLog(log);
        }
      });
    } catch (error) {
      console.error('Error setting up SignalR:', error);
    }
  }

  private cleanupSignalR(): void {
    if (this.nodeId) {
      this.signalRService.leaveDebugGroup(this.nodeId).catch(err =>
        console.warn('Failed to leave debug group:', err)
      );
    }
    this.signalRService.off('DebugLogReceived');
  }

  private addLog(log: NodeDebugLog): void {
    const currentLogs = this.logs();
    const newLogs = [...currentLogs, log];

    // Limit the number of logs
    if (newLogs.length > this.MAX_LOGS) {
      newLogs.splice(0, newLogs.length - this.MAX_LOGS);
    }

    this.logs.set(newLogs);
    this.shouldScrollToBottom = true;
  }

  private scrollToBottom(): void {
    if (this.logContainer?.nativeElement) {
      const el = this.logContainer.nativeElement;
      el.scrollTop = el.scrollHeight;
    }
  }

  togglePause(): void {
    this.isPaused.update(v => !v);
  }

  clearLogs(): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Logs löschen',
        message: 'Möchten Sie alle Debug-Logs für diesen Node löschen? Diese Aktion kann nicht rückgängig gemacht werden.',
        confirmText: 'Löschen',
        cancelText: 'Abbrechen'
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.performClearLogs();
      }
    });
  }

  private performClearLogs(): void {
    this.isClearing.set(true);
    this.debugApiService.clearLogs(this.nodeId).subscribe({
      next: (result) => {
        this.logs.set([]);
        this.snackBar.open(`${result.deleted} Logs gelöscht`, 'Schließen', { duration: 3000 });
        this.isClearing.set(false);
      },
      error: (error) => {
        console.error('Error clearing logs:', error);
        this.snackBar.open('Fehler beim Löschen der Logs', 'Schließen', { duration: 5000 });
        this.isClearing.set(false);
      }
    });
  }

  refresh(): void {
    this.loadRecentLogs();
  }

  // Filtered logs based on current filters
  get filteredLogs(): NodeDebugLog[] {
    let result = this.logs();

    if (this.selectedLevel) {
      result = result.filter(l => l.level === this.selectedLevel);
    }

    if (this.selectedCategory) {
      result = result.filter(l => l.category === this.selectedCategory);
    }

    if (this.globalFilter) {
      const search = this.globalFilter.toLowerCase();
      result = result.filter(l =>
        l.message.toLowerCase().includes(search) ||
        (l.stackTrace?.toLowerCase().includes(search) ?? false)
      );
    }

    return result;
  }

  // ---------- Search Methods ----------
  onFilterInput(value: string): void {
    this.search$.next(value ?? '');
  }

  toggleSearch(): void {
    this.isSearchOpen = !this.isSearchOpen;
    if (this.isSearchOpen) {
      setTimeout(() => this.searchInput?.nativeElement?.focus(), 0);
    } else {
      this.clearSearch();
    }
  }

  closeSearch(): void {
    if (this.isSearchOpen) {
      this.isSearchOpen = false;
      this.clearSearch();
    }
  }

  onSearchBlur(): void {
    if (!this.globalFilter?.length) {
      this.isSearchOpen = false;
    }
  }

  clearSearch(): void {
    this.globalFilter = '';
    this.search$.next('');
    this.isSearchOpen = false;
    this.cdr.markForCheck();
  }

  // ---------- Filter Drawer Methods ----------
  toggleFilter(): void {
    if (this.isFilterOpen) {
      this.closeFilter();
    } else {
      this.openFilter();
    }
  }

  openFilter(): void {
    this.isFilterOpen = true;
    this.cdr.markForCheck();

    if (!this.filterOverlayRef) {
      const cfg: OverlayConfig = {
        hasBackdrop: true,
        backdropClass: 'gl-drawer-backdrop',
        panelClass: ['gl-drawer-panel'],
        width: this.drawerWidth,
        height: `calc(100vh - ${this.drawerTopOffset}px)`,
        scrollStrategy: this.overlay.scrollStrategies.block(),
        positionStrategy: this.overlay.position().global().top(`${this.drawerTopOffset}px`).right('0')
      };
      this.filterOverlayRef = this.overlay.create(cfg);
      this.filterOverlayRef.backdropClick().subscribe(() => this.closeFilter());
    } else {
      this.filterOverlayRef.updateSize({
        width: this.drawerWidth,
        height: `calc(100vh - ${this.drawerTopOffset}px)`
      });
      this.filterOverlayRef.updatePositionStrategy(
        this.overlay.position().global().top(`${this.drawerTopOffset}px`).right('0')
      );
    }

    const portal = new TemplatePortal(this.filterHostTpl, this.vcr);
    this.filterOverlayRef.attach(portal);
    requestAnimationFrame(() => {
      this.filterOverlayRef?.addPanelClass('open');
      this.cdr.markForCheck();
    });
  }

  closeFilter(): void {
    this.isFilterOpen = false;
    this.cdr.markForCheck();
    this.teardownFilterOverlay();
  }

  private teardownFilterOverlay(): void {
    if (!this.filterOverlayRef) return;
    this.filterOverlayRef.removePanelClass('open');
    setTimeout(() => {
      this.filterOverlayRef?.detach();
    }, 200);
  }

  applyFilters(): void {
    this.closeFilter();
  }

  clearFilters(): void {
    this.selectedLevel = null;
    this.selectedCategory = null;
    this.cdr.markForCheck();
    this.closeFilter();
  }

  hasActiveFilters(): boolean {
    return this.selectedLevel !== null || this.selectedCategory !== null;
  }

  getLevelColor(level: DebugLevel): string {
    return this.debugLevels.find(l => l.value === level)?.color || '#666';
  }

  getCategoryIcon(category: LogCategory): string {
    return this.logCategories.find(c => c.value === category)?.icon || 'help';
  }

  getCategoryLabel(category: LogCategory): string {
    return this.logCategories.find(c => c.value === category)?.label || category;
  }

  formatTime(dateStr: string): string {
    const date = new Date(dateStr.endsWith('Z') ? dateStr : dateStr + 'Z');
    const timeStr = date.toLocaleTimeString('de-DE', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
    // Manually add milliseconds
    const ms = date.getMilliseconds().toString().padStart(3, '0');
    return `${timeStr}.${ms}`;
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr.endsWith('Z') ? dateStr : dateStr + 'Z');
    return date.toLocaleDateString('de-DE', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }
}
