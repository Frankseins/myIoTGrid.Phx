import { Component, OnInit, inject, signal, ViewChild, ElementRef, TemplateRef, ViewContainerRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatMenuModule } from '@angular/material/menu';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { FormsModule } from '@angular/forms';
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal, ComponentPortal } from '@angular/cdk/portal';
import { firstValueFrom } from 'rxjs';
import { ExpeditionApiService } from '@myiotgrid/shared/data-access';
import {
  Expedition,
  ExpeditionStatus
} from '@myiotgrid/shared/models';
import { ExpeditionCardComponent } from '../expedition-card/expedition-card.component';
import { ExpeditionFormComponent } from '../expedition-form/expedition-form.component';
import { LoadingSpinnerComponent, EmptyStateComponent } from '@myiotgrid/shared/ui';

@Component({
  selector: 'myiotgrid-expeditions-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatMenuModule,
    MatSelectModule,
    MatFormFieldModule,
    MatToolbarModule,
    MatTooltipModule,
    MatDividerModule,
    ExpeditionCardComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent
  ],
  templateUrl: './expeditions-list.component.html',
  styleUrl: './expeditions-list.component.scss'
})
export class ExpeditionsListComponent implements OnInit {
  private readonly expeditionApiService = inject(ExpeditionApiService);
  private readonly router = inject(Router);
  private readonly overlay = inject(Overlay);
  private readonly viewContainerRef = inject(ViewContainerRef);

  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;
  @ViewChild('filterDrawer') filterDrawerTemplate!: TemplateRef<unknown>;

  // State
  expeditions = signal<Expedition[]>([]);
  initialLoadDone = signal(false);
  error = signal<string | null>(null);

  // Filter & Search
  filterStatus = 'all';
  searchTerm = '';
  isSearchOpen = false;

  // Sort
  sortField: 'name' | 'startTime' | 'status' | 'createdAt' = 'createdAt';
  sortDirection: 'asc' | 'desc' = 'desc';

  sortOptions = [
    { value: 'name' as const, label: 'Name' },
    { value: 'startTime' as const, label: 'Startzeit' },
    { value: 'status' as const, label: 'Status' },
    { value: 'createdAt' as const, label: 'Erstellt am' }
  ];

  // Overlay refs
  private filterOverlayRef: OverlayRef | null = null;
  private formOverlayRef: OverlayRef | null = null;

  // Form state
  formMode: 'create' | 'edit' = 'create';
  editingExpedition: Expedition | null = null;

  // Enum for template
  ExpeditionStatus = ExpeditionStatus;

  // Computed: filtered and sorted expeditions
  get filteredAndSortedExpeditions(): Expedition[] {
    let result = [...this.expeditions()];

    // Status filter
    if (this.filterStatus !== 'all') {
      result = result.filter(e => e.status === this.filterStatus);
    }

    // Search filter
    if (this.searchTerm.length >= 2) {
      const term = this.searchTerm.toLowerCase();
      result = result.filter(e =>
        e.name.toLowerCase().includes(term) ||
        e.nodeName?.toLowerCase().includes(term) ||
        e.description?.toLowerCase().includes(term) ||
        e.tags?.some(t => t.toLowerCase().includes(term))
      );
    }

    // Sort
    result.sort((a, b) => {
      let comparison = 0;
      switch (this.sortField) {
        case 'name':
          comparison = a.name.localeCompare(b.name);
          break;
        case 'startTime':
          comparison = new Date(a.startTime).getTime() - new Date(b.startTime).getTime();
          break;
        case 'status':
          comparison = a.status.localeCompare(b.status);
          break;
        case 'createdAt':
          comparison = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
          break;
      }
      return this.sortDirection === 'asc' ? comparison : -comparison;
    });

    return result;
  }

  get hasActiveFilters(): boolean {
    return this.filterStatus !== 'all';
  }

  ngOnInit(): void {
    this.loadExpeditions();
  }

  async loadExpeditions(): Promise<void> {
    this.error.set(null);

    try {
      const expeditions = await firstValueFrom(
        this.expeditionApiService.getAll()
      );
      this.expeditions.set(expeditions);
    } catch (err) {
      console.error('Failed to load expeditions:', err);
      this.error.set('Fehler beim Laden der Messfahrten');
    } finally {
      this.initialLoadDone.set(true);
    }
  }

  onCreate(): void {
    this.formMode = 'create';
    this.editingExpedition = null;
    this.openFormDrawer();
  }

  openEditDialog(expedition: Expedition): void {
    this.formMode = 'edit';
    this.editingExpedition = expedition;
    this.openFormDrawer();
  }

  private openFormDrawer(): void {
    if (this.formOverlayRef) return;

    const positionStrategy = this.overlay
      .position()
      .global()
      .right('0')
      .top('0');

    this.formOverlayRef = this.overlay.create({
      positionStrategy,
      hasBackdrop: true,
      backdropClass: 'gt-drawer-backdrop',
      panelClass: ['gt-drawer-panel'],
      width: '420px',
      height: '100vh',
      scrollStrategy: this.overlay.scrollStrategies.block()
    });

    const portal = new ComponentPortal(ExpeditionFormComponent, this.viewContainerRef);
    const componentRef = this.formOverlayRef.attach(portal);

    // Set inputs
    componentRef.setInput('mode', this.formMode);
    componentRef.setInput('expedition', this.editingExpedition);

    // Subscribe to outputs
    componentRef.instance.saved.subscribe(async () => {
      this.closeFormDrawer();
      await this.loadExpeditions();
    });

    componentRef.instance.cancelled.subscribe(() => {
      this.closeFormDrawer();
    });

    this.formOverlayRef.backdropClick().subscribe(() => this.closeFormDrawer());

    // Trigger slide-in animation after attach
    requestAnimationFrame(() => {
      this.formOverlayRef?.addPanelClass('open');
    });
  }

  closeFormDrawer(): void {
    if (this.formOverlayRef) {
      // Remove open class to trigger slide-out animation
      this.formOverlayRef.removePanelClass('open');
      // Wait for animation to complete before disposing
      setTimeout(() => {
        this.formOverlayRef?.dispose();
        this.formOverlayRef = null;
        this.editingExpedition = null;
      }, 250);
    }
  }

  async deleteExpedition(expedition: Expedition): Promise<void> {
    if (!confirm(`Messfahrt "${expedition.name}" wirklich löschen?`)) {
      return;
    }

    try {
      await firstValueFrom(this.expeditionApiService.remove(expedition.id));
      await this.loadExpeditions();
    } catch (err) {
      console.error('Failed to delete expedition:', err);
      this.error.set('Fehler beim Löschen der Messfahrt');
    }
  }

  onCardClick(expedition: Expedition): void {
    this.router.navigate(['/expeditions', expedition.id]);
  }

  // Search methods
  toggleSearch(): void {
    this.isSearchOpen = !this.isSearchOpen;
    if (this.isSearchOpen) {
      setTimeout(() => this.searchInput?.nativeElement?.focus(), 100);
    }
  }

  closeSearch(): void {
    this.isSearchOpen = false;
    this.searchTerm = '';
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.searchInput?.nativeElement?.focus();
  }

  onSearchBlur(): void {
    if (this.searchTerm.length === 0) {
      this.isSearchOpen = false;
    }
  }

  // Sort methods
  toggleSortDirection(): void {
    this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
  }

  // Filter methods
  toggleFilter(): void {
    if (this.filterOverlayRef) {
      this.closeFilter();
    } else {
      this.openFilter();
    }
  }

  openFilter(): void {
    if (this.filterOverlayRef) return;

    const positionStrategy = this.overlay
      .position()
      .global()
      .right('0')
      .top('0');

    this.filterOverlayRef = this.overlay.create({
      positionStrategy,
      hasBackdrop: true,
      backdropClass: 'gt-drawer-backdrop',
      panelClass: ['gt-drawer-panel'],
      width: '340px',
      height: '100vh',
      scrollStrategy: this.overlay.scrollStrategies.block()
    });

    const portal = new TemplatePortal(this.filterDrawerTemplate, this.viewContainerRef);
    this.filterOverlayRef.attach(portal);
    this.filterOverlayRef.backdropClick().subscribe(() => this.closeFilter());

    // Trigger slide-in animation after attach
    requestAnimationFrame(() => {
      this.filterOverlayRef?.addPanelClass('open');
    });
  }

  closeFilter(): void {
    if (this.filterOverlayRef) {
      // Remove open class to trigger slide-out animation
      this.filterOverlayRef.removePanelClass('open');
      // Wait for animation to complete before disposing
      setTimeout(() => {
        this.filterOverlayRef?.dispose();
        this.filterOverlayRef = null;
      }, 250);
    }
  }

  clearFilters(): void {
    this.filterStatus = 'all';
  }

  onRefresh(): void {
    this.loadExpeditions();
  }

  trackByExpedition(index: number, expedition: Expedition): string {
    return expedition.id;
  }
}
