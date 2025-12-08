import {
    AfterViewInit, ChangeDetectionStrategy, Component, ContentChild, ContentChildren, Directive,
    ElementRef, EventEmitter, Input, OnChanges, OnDestroy, OnInit,
    Output, QueryList, SimpleChanges, TemplateRef, ViewChild, ViewContainerRef,
    ChangeDetectorRef, AfterContentInit
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { MatTableModule } from '@angular/material/table';
import { MatSort, MatSortModule, Sort, MatSortable } from '@angular/material/sort';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatInputModule } from '@angular/material/input';

import { Router } from '@angular/router';
import { Overlay, OverlayConfig, OverlayModule, OverlayRef } from '@angular/cdk/overlay';
import { PortalModule, TemplatePortal } from '@angular/cdk/portal';

import { debounceTime, distinctUntilChanged, Subject, Subscription } from 'rxjs';

import { TableStateService, GenericTableExtraState } from '../../services/table-state.service';

export interface GenericListColumn {
    field: string;
    header: string;
    width?: string;
    sortable?: boolean;
    type?: 'text' | 'boolean' | 'date' | 'number';
}

export interface ListLazyEvent {
    first: number;
    rows: number;
    sortField?: string;
    sortOrder?: 1 | -1;
    globalFilter?: string;
    filters?: Record<string, unknown>;
}

/* ---------------------------
   Directive: ListColumnTemplate
   Verwendet für Custom Column Templates
---------------------------- */
@Directive({
    selector: '[myiotgridListColumnTemplate]',
    standalone: true
})
export class ListColumnTemplateDirective {
    @Input('myiotgridListColumnTemplate') columnName!: string;

    constructor(public template: TemplateRef<unknown>) {}
}

@Component({
    selector: 'myiotgrid-generic-list',
    standalone: true,
    templateUrl: './generic-list.component.html',
    styleUrls: ['./generic-list.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule, FormsModule,
        MatTableModule, MatSortModule, MatPaginatorModule,
        MatIconModule, MatButtonModule,
        MatToolbarModule, MatTooltipModule, MatProgressBarModule,
        MatInputModule,
        OverlayModule, PortalModule
    ]
})
export class GenericListComponent implements OnInit, AfterViewInit, AfterContentInit, OnDestroy, OnChanges {
    // --- Daten / Spalten
    private _data: unknown[] = [];
    @Input() set data(v: unknown[]) {
        this._data = v ?? [];
    }
    get data(): unknown[] { return this._data; }

    @Input() columns: GenericListColumn[] = [];
    @Input() totalRecords = 0;
    @Input() loading = false;

    @Input() dataKey = 'id';
    @Input() stateKey = 'generic-list';

    // Titel (wird in der Toolbar angezeigt)
    @Input() title = '';

    // Suche
    @Input() showSearch = true;
    globalFilter = '';
    @Input() globalFilterFields: string[] = [];

    // Drawer (unter TopNav)
    @Input() drawerTopOffset = 64;
    @Input() drawerWidth = '33.33vw';
    @Input() drawerBackdrop = true;

    // Filter
    @Input() showFilter = false;
    @Input() filterTitle = 'Filter';
    currentFilters: Record<string, unknown> = {};

    // Delete Button
    @Input() showDeleteButton = false;

    // Slot für Filter
    @ContentChild('filterTemplate', { static: false }) filterTemplate!: TemplateRef<unknown>;

    // Slot für zusätzliche Toolbar-Buttons (z.B. Delete Button)
    @ContentChild('toolbarActionsTemplate', { static: false }) toolbarActionsTemplate!: TemplateRef<unknown>;

    // Custom Column Templates
    @ContentChildren(ListColumnTemplateDirective) customColumnTemplates!: QueryList<ListColumnTemplateDirective>;

    // Map für Custom Column Templates (field -> TemplateRef)
    columnTemplates: Map<string, TemplateRef<unknown>> = new Map();

    // Events
    @Output() lazyLoad = new EventEmitter<ListLazyEvent>();
    @Output() filterChange = new EventEmitter<Record<string, unknown>>();
    @Output() refresh = new EventEmitter<void>();
    @Output() deleteClicked = new EventEmitter<void>();

    // Material Table
    displayedColumns: string[] = [];

    @ViewChild(MatSort) sort!: MatSort;
    @ViewChild(MatPaginator) paginator!: MatPaginator;

    // Scrollhost
    @ViewChild('scrollWrap', { read: ElementRef }) scrollWrapRef!: ElementRef<HTMLElement>;

    // Search
    isSearchOpen = false;
    @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;
    private search$ = new Subject<string>();

    // Overlay (Drawer für Filter)
    private filterOverlayRef: OverlayRef | null = null;
    @ViewChild('filterHost') filterHostTpl!: TemplateRef<unknown>;
    isFilterOpen = false;

    // Footer-State-Anzeige
    hasState = false;
    private readonly defaultPageSize = 10;

    private subs = new Subscription();
    private _afterInit = false;

    constructor(
        private overlay: Overlay,
        private vcr: ViewContainerRef,
        private router: Router,
        private tableState: TableStateService,
        private cdr: ChangeDetectorRef
    ) {}

    /** Eindeutiger Storage-Key */
    private get storageKey(): string {
        const explicit = this.stateKey && this.stateKey !== 'generic-list' ? this.stateKey : '';
        if (explicit) return explicit;
        const url = this.router?.url ?? '';
        const cols = (this.columns ?? []).map(c => c.field).join(',');
        return `gl:${url}:${cols}:${this.dataKey}`;
    }

    /** Prüft, ob Filter aktiv sind */
    get hasActiveFilters(): boolean {
        return Object.keys(this.currentFilters).some(key => {
            const val = this.currentFilters[key];
            return val !== undefined && val !== null && val !== '';
        });
    }

    // ---------- Lifecycle ----------
    ngOnInit(): void {
        const st = this.tableState.loadExtraState(this.storageKey);
        this.hasState = this.computeHasState(st);

        // Filter aus State laden
        if (st?.filters) {
            this.currentFilters = st.filters as Record<string, unknown>;
        }

        // Suche aus State laden
        this.globalFilter = st?.globalFilter ?? '';
        this.isSearchOpen = !!this.globalFilter;

        // Suche debounced (erst ab 3 Zeichen oder leer)
        this.subs.add(
            this.search$.pipe(distinctUntilChanged(), debounceTime(200)).subscribe(term => {
                const normalized = term ?? '';
                // Nur bei leerem String oder ab 3 Zeichen suchen
                if (normalized.length === 0 || normalized.length >= 3) {
                    this.globalFilter = normalized;
                    this.saveState({ globalFilter: this.globalFilter });
                    this.emitLazy();
                }
                this.cdr.markForCheck();
            })
        );
    }

    ngAfterContentInit(): void {
        // Register all custom column templates
        if (this.customColumnTemplates) {
            this.customColumnTemplates.forEach(directive => {
                if (directive.columnName) {
                    this.columnTemplates.set(directive.columnName, directive.template);
                }
            });
        }
    }

    ngAfterViewInit(): void {
        // Nur Datenspalten (keine Select/Actions)
        this.displayedColumns = this.columns.map(c => c.field);

        // Sort/Paging/Scroll anwenden
        const st = this.tableState.loadExtraState(this.storageKey);
        if (st) {
            if (this.paginator) {
                if (typeof st.pageSize === 'number') this.paginator.pageSize = st.pageSize;
                if (typeof st.pageIndex === 'number') this.paginator.pageIndex = st.pageIndex;
            }
            if (this.sort && st.sort?.active) {
                const s: MatSortable = {
                    id: st.sort.active,
                    start: (st.sort.direction || 'asc') as 'asc' | 'desc',
                    disableClear: false
                };
                this.sort.sort(s);
            }
            if (this.scrollWrapRef?.nativeElement && typeof st.scrollTop === 'number') {
                setTimeout(() => {
                    try { this.scrollWrapRef.nativeElement.scrollTop = st.scrollTop!; } catch { /* ignore */ }
                }, 0);
            }
        }

        this._afterInit = true;
        queueMicrotask(() => this.emitLazy());
    }

    ngOnChanges(_: SimpleChanges): void {
        // handled im data-Setter
    }

    ngOnDestroy(): void {
        this.teardownFilterOverlay();
        this.subs.unsubscribe();
    }

    // ---------- Toolbar Aktionen ----------
    onRefresh(): void {
        this.refresh.emit();
        this.emitLazy();
    }

    // ---------- Suche ----------
    onFilterInput(value: string): void { this.search$.next(value ?? ''); }
    toggleSearch(): void {
        this.isSearchOpen = !this.isSearchOpen;
        if (this.isSearchOpen) setTimeout(() => this.searchInput?.nativeElement?.focus(), 0);
        else this.clearSearch();
    }
    closeSearch(): void { if (this.isSearchOpen) { this.isSearchOpen = false; this.clearSearch(); } }
    onSearchBlur(): void { if (!this.globalFilter?.length) this.isSearchOpen = false; }
    clearSearch(): void {
        this.globalFilter = '';
        this.saveState({ globalFilter: '' });
        this.search$.next('');
        this.isSearchOpen = false;
        this.cdr.markForCheck();
    }

    // ---------- Filter Sidebar ----------
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
                hasBackdrop: this.drawerBackdrop,
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

        const portal = new TemplatePortal(this.filterHostTpl, this.vcr, {
            $implicit: this.currentFilters
        });
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

    applyFilters(): void {
        this.filterChange.emit(this.currentFilters);
        this.saveState({ filters: this.currentFilters });
        this.emitLazy();
        this.closeFilter();
    }

    clearFilters(): void {
        this.currentFilters = {};
        this.filterChange.emit(this.currentFilters);
        this.saveState({ filters: {} });
        this.emitLazy();
        this.closeFilter();
        this.cdr.markForCheck();
    }

    private teardownFilterOverlay(): void {
        if (!this.filterOverlayRef) return;
        this.filterOverlayRef.removePanelClass('open');
        setTimeout(() => {
            this.filterOverlayRef?.detach();
        }, 200);
    }

    // ---------- Sort/Paging ----------
    onSortChange(e: Sort) {
        this.saveState({ sort: { active: e.active, direction: (e.direction || '') as 'asc' | 'desc' | '' } });
        this.emitLazy();
    }

    onPageChange(e: PageEvent) {
        this.saveState({ pageIndex: e.pageIndex, pageSize: e.pageSize });
        this.emitLazy();
    }

    private emitLazy(): void {
        const first = this.paginator?.pageIndex ? (this.paginator.pageIndex * this.paginator.pageSize) : 0;
        const rows = this.paginator?.pageSize ?? this.defaultPageSize;
        const sortField = this.sort?.active || undefined;
        const sortOrder = this.sort?.direction ? (this.sort.direction === 'desc' ? -1 : 1) : undefined;
        this.lazyLoad.emit({
            first,
            rows,
            sortField,
            sortOrder,
            globalFilter: this.globalFilter,
            filters: this.currentFilters
        });
    }

    // ---------- Scroll ----------
    onScroll(evt: Event): void {
        const host = evt.target as HTMLElement;
        this.saveState({ scrollTop: host.scrollTop ?? 0 });
    }

    // ---------- Footer: Gesamten State löschen ----------
    clearAllState(): void {
        // Storage leeren
        this.tableState.clearExtraState(this.storageKey);
        this.hasState = false;

        // Suche
        this.globalFilter = '';
        this.isSearchOpen = false;

        // Filter
        this.currentFilters = {};
        if (this.filterOverlayRef?.hasAttached()) this.closeFilter();

        // Sortierung
        if (this.sort) {
            this.sort.active = '';
            this.sort.direction = '';
            this.sort.sortChange.emit({ active: '', direction: '' });
        }

        // Paging
        if (this.paginator) {
            this.paginator.pageSize = this.defaultPageSize;
            this.paginator.firstPage();
        }

        // Scroll
        if (this.scrollWrapRef?.nativeElement) this.scrollWrapRef.nativeElement.scrollTop = 0;

        // Parent informieren
        this.emitLazy();
        this.cdr.markForCheck();
    }

    // ---------- Hilfen ----------
    /** Prüft, ob ein Custom-Template für eine Spalte existiert */
    getColumnTemplate(field: string): TemplateRef<unknown> | null {
        return this.columnTemplates.get(field) ?? null;
    }

    /** Helper für Template: Zugriff auf Objekt-Property */
    getFieldValue(row: unknown, field: string): unknown {
        return (row as Record<string, unknown>)?.[field];
    }

    private saveState(patch: Partial<GenericTableExtraState>): void {
        this.tableState.saveExtraState(this.storageKey, patch);
        this.hasState = true;
    }

    private computeHasState(st: GenericTableExtraState | null): boolean {
        if (!st) return false;
        if (st.globalFilter) return true;
        if (st.sort && st.sort.active) return true;
        if ((st.pageIndex ?? 0) > 0) return true;
        if (st.pageSize && st.pageSize !== this.defaultPageSize) return true;
        if ((st.scrollTop ?? 0) > 0) return true;
        if (st.filters && Object.keys(st.filters).length > 0) return true;
        return false;
    }
}
