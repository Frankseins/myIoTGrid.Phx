import {
    AfterViewInit, ChangeDetectionStrategy, Component, ContentChild, ContentChildren, Directive,
    ElementRef, EventEmitter, HostListener, Input, OnChanges, OnDestroy, OnInit,
    Output, QueryList, Renderer2, SimpleChanges, TemplateRef, ViewChild, ViewContainerRef,
    ChangeDetectorRef, AfterContentInit
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { MatTableModule } from '@angular/material/table';
import { MatSort, MatSortModule, Sort, MatSortable } from '@angular/material/sort';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { SelectionModel } from '@angular/cdk/collections';
import { Router } from '@angular/router';
import { Overlay, OverlayConfig, OverlayModule, OverlayRef } from '@angular/cdk/overlay';
import { PortalModule, TemplatePortal } from '@angular/cdk/portal';

import { debounceTime, distinctUntilChanged, Subject, Subscription } from 'rxjs';

import { ConfirmDialogComponent, ConfirmDialogData } from '../confirm-dialog/confirm-dialog.component';
import { TableStateService, GenericTableExtraState } from '../../services/table-state.service';
import { ReadonlyFieldsDirective } from './readonly-fields.directive';

// Optional global search service – falls vorhanden
export abstract class GlobalSearchService {
    abstract getTerm(channel: string): string | null;
    abstract term$(channel: string): Subject<string>;
    abstract setTerm(channel: string, term: string): void;
    abstract clear(channel: string): void;
}

export type DetailMode = 'drawer' | 'route' | 'none';

export interface GenericTableColumn {
    field: string;
    header: string;
    width?: string;
    sortable?: boolean;
    type?: 'text' | 'boolean' | 'date' | 'number';
}

export interface MaterialLazyEvent {
    first: number;
    rows: number;
    sortField?: string;
    sortOrder?: 1 | -1;
    globalFilter?: string;
    filters?: Record<string, unknown>;
}

export interface FilterField {
    field: string;
    label: string;
    type: 'text' | 'select' | 'checkbox' | 'date';
    options?: { label: string; value: unknown }[];
}

/* ---------------------------
   Directive: appColumnTemplate
   Verwendet für Custom Column Templates
---------------------------- */
@Directive({
    selector: '[myiotgridColumnTemplate]',
    standalone: true
})
export class ColumnTemplateDirective {
    @Input('myiotgridColumnTemplate') columnName!: string;

    constructor(public template: TemplateRef<unknown>) {}
}

@Component({
    selector: 'myiotgrid-generic-table',
    standalone: true,
    templateUrl: './generic-table.component.html',
    styleUrls: ['./generic-table.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule, FormsModule,
        MatTableModule, MatSortModule, MatPaginatorModule,
        MatCheckboxModule, MatIconModule, MatButtonModule,
        MatToolbarModule, MatTooltipModule, MatFormFieldModule,
        MatInputModule, MatDialogModule, MatProgressBarModule,
        OverlayModule, PortalModule,
        ReadonlyFieldsDirective
    ]
})
export class GenericTableComponent implements OnInit, AfterViewInit, AfterContentInit, OnDestroy, OnChanges {
    // --- Daten / Spalten
    private _data: unknown[] = [];
    @Input() set data(v: unknown[]) {
        this._data = v ?? [];
        if (this._afterInit) this.restoreSelectionDeferred();
    }
    get data(): unknown[] { return this._data; }

    @Input() columns: GenericTableColumn[] = [];
    @Input() totalRecords = 0;
    @Input() loading = false;

    @Input() dataKey = 'id';
    @Input() stateKey = 'generic-table-material';

    // Toolbar
    @Input() showToolbar = true;
    @Input() createLabel = 'Neu';

    // Suche
    globalFilter = '';
    @Input() globalFilterFields: string[] = [];
    @Input() useGlobalSearch = false;
    @Input() searchChannel = 'default';

    // Detail
    @Input() detailMode: DetailMode = 'drawer';
    @Input() detailRouteBase = '';

    // Drawer (unter TopNav)
    @Input() drawerTopOffset = 64;
    @Input() drawerWidth = '33.33vw';
    @Input() drawerBackdrop = false;
    @Input() detailTitle = 'Details';

    // Filter
    @Input() showFilter = false;
    @Input() filterTitle = 'Filter';
    currentFilters: Record<string, unknown> = {};

    // Slot fürs Detail
    @ContentChild('detailTemplate', { static: false }) detailTemplate!: TemplateRef<unknown>;

    // Slot für Filter
    @ContentChild('filterTemplate', { static: false }) filterTemplate!: TemplateRef<unknown>;

    // Custom Column Templates (z.B. [myiotgridColumnTemplate]="'domainKey'")
    @ContentChildren(ColumnTemplateDirective) customColumnTemplates!: QueryList<ColumnTemplateDirective>;

    // Map für Custom Column Templates (field -> TemplateRef)
    columnTemplates: Map<string, TemplateRef<unknown>> = new Map();

    // Events
    @Output() create = new EventEmitter<unknown>();
    @Output() edit   = new EventEmitter<unknown>();
    @Output() remove = new EventEmitter<unknown>();
    @Output() save   = new EventEmitter<unknown>();
    @Output() lazyLoad = new EventEmitter<MaterialLazyEvent>();
    @Output() filterChange = new EventEmitter<Record<string, unknown>>();
    @Output() refresh = new EventEmitter<void>();

    // Material Table
    displayedColumns: string[] = [];
    selection = new SelectionModel<unknown>(true, []);
    selectedItem: Record<string, unknown> | null = null;

    @ViewChild(MatSort) sort!: MatSort;
    @ViewChild(MatPaginator) paginator!: MatPaginator;

    // Scrollhost
    @ViewChild('scrollWrap', { read: ElementRef }) scrollWrapRef!: ElementRef<HTMLElement>;

    // Overlay (Drawer für Detail)
    private overlayRef: OverlayRef | null = null;
    @ViewChild('drawerHost') drawerHostTpl!: TemplateRef<unknown>;

    // Overlay (Drawer für Filter)
    private filterOverlayRef: OverlayRef | null = null;
    @ViewChild('filterHost') filterHostTpl!: TemplateRef<unknown>;
    isFilterOpen = false;

    // Suche UI
    isSearchOpen = false;
    @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;
    private search$ = new Subject<string>();

    // Schlösser
    editMode = false;   // Toolbar-Schloss
    isReadonly = true;  // Drawer-Schloss

    // Footer-State-Anzeige
    hasState = false;
    private readonly defaultPageSize = 10;

    private subs = new Subscription();
    private _afterInit = false;

    private globalSearch: GlobalSearchService | null = null;

    constructor(
        private overlay: Overlay,
        private vcr: ViewContainerRef,
        private router: Router,
        private dialog: MatDialog,
        private tableState: TableStateService,
        private cdr: ChangeDetectorRef
    ) {}

    /** Eindeutiger Storage-Key (vermeidet Kollisionen zwischen Tabellen) */
    private get storageKey(): string {
        const explicit = this.stateKey && this.stateKey !== 'generic-table-material' ? this.stateKey : '';
        if (explicit) return explicit;
        const url = this.router?.url ?? '';
        const cols = (this.columns ?? []).map(c => c.field).join(',');
        return `gt:${url}:${cols}:${this.dataKey}`;
    }

    /** Selektionsspalte sofort einblenden, wenn Auswahl vorhanden */
    get hasSelection(): boolean { return this.selection.hasValue(); }

    /** Prüft, ob Filter aktiv sind */
    get hasActiveFilters(): boolean {
        return Object.keys(this.currentFilters).length > 0;
    }

    // ---------- Lifecycle ----------
    ngOnInit(): void {
        const st = this.tableState.loadExtraState(this.storageKey);
        this.hasState = this.computeHasState(st);

        if (st?.editMode !== undefined) this.editMode = !!st.editMode;
        if (st?.readonly !== undefined) this.isReadonly = !!st.readonly;

        // Suche initial
        if (this.useGlobalSearch && this.globalSearch) {
            this.globalFilter = this.globalSearch.getTerm(this.searchChannel) ?? '';
        } else {
            this.globalFilter = st?.globalFilter ?? '';
        }
        this.isSearchOpen = !!this.globalFilter;

        // Selection-Änderungen persistieren
        this.subs.add(this.selection.changed.subscribe(() => {
            this.persistSelection();
            this.cdr.markForCheck();
        }));

        // Suche debounced
        this.subs.add(
            this.search$.pipe(distinctUntilChanged(), debounceTime(200)).subscribe(term => {
                if (this.useGlobalSearch && this.globalSearch) {
                    this.globalSearch.setTerm(this.searchChannel, term ?? '');
                    this.hasState = true;
                } else {
                    this.globalFilter = term ?? '';
                    this.saveState({ globalFilter: this.globalFilter ?? '' });
                }
                this.isSearchOpen = !!this.globalFilter;
                this.emitLazy();
                this.cdr.markForCheck();
            })
        );

        if (this.useGlobalSearch && this.globalSearch) {
            this.subs.add(
                this.globalSearch.term$(this.searchChannel)
                    .pipe(distinctUntilChanged(), debounceTime(150))
                    .subscribe(term => {
                        this.globalFilter = term ?? '';
                        this.hasState = this.hasState || !!this.globalFilter;
                        this.emitLazy();
                        this.isSearchOpen = !!this.globalFilter;
                        this.cdr.markForCheck();
                    })
            );
        }
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
        this.displayedColumns = ['_select', ...this.columns.map(c => c.field), '_actions'];

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
        this.restoreSelectionDeferred();
        queueMicrotask(() => this.emitLazy());
    }

    ngOnChanges(_: SimpleChanges): void {
        // handled im data-Setter
    }

    ngOnDestroy(): void {
        this.teardownOverlay();
        this.teardownFilterOverlay();
        this.subs.unsubscribe();
    }

    // ---------- Toolbar Sichtbarkeit ----------
    get selectedCount(): number { return this.selection.selected.length; }
    get canEditToolbar(): boolean { return this.selectedCount === 1; }
    get canDeleteToolbar(): boolean { return this.selectedCount >= 1; }

    // ---------- Toolbar Aktionen ----------
    toggleEditMode(): void {
        this.editMode = !this.editMode;
        this.saveState({ editMode: this.editMode });
    }

    onRefresh(): void {
        this.refresh.emit();
        this.emitLazy();
    }

    openCreate(): void {
        const item = {} as Record<string, unknown>;
        this.create.emit(item);
        if (this.detailMode === 'drawer') {
            // Neu → immer im Edit-Modus öffnen
            this.isReadonly = false;
            this.saveState({ readonly: this.isReadonly });
            this.openDrawer(item);
        } else if (this.detailRouteBase) {
            // Route-basiert: keine readonly query parameter bei "new"
            this.router.navigate([this.detailRouteBase, 'new']);
        }
    }

    onToolbarEdit(): void {
        if (!this.canEditToolbar) return;
        const row = this.selection.selected[0];
        this.openDetail(row);
    }

    onToolbarDelete(): void {
        if (!this.canDeleteToolbar) return;
        const items = this.selection.selected.slice();
        this.confirmAndDelete(items);
    }

    // ---------- Row Aktionen ----------
    openDetail(row: unknown): void {
        const copy = { ...(row as Record<string, unknown>) };
        this.edit.emit(copy);

        if (this.detailMode === 'drawer') {
            // Bearbeitungsmodus beachten: Wenn editMode aktiv, im Edit-Modus öffnen
            this.isReadonly = !this.editMode;
            this.saveState({ readonly: this.isReadonly });
            this.openDrawer(copy);
        } else if (this.detailRouteBase) {
            const id = (row as Record<string, unknown>)?.[this.dataKey];
            if (id != null) {
                // Route-basiert: editMode beachten - mode=edit wenn editMode aktiv, sonst mode=view
                const mode = this.editMode ? 'edit' : 'view';
                this.router.navigate([this.detailRouteBase, id], { queryParams: { mode } });
            }
        }
    }
    openEdit(row: unknown): void { this.openDetail(row); }

    deleteItem(row: unknown): void { this.confirmAndDelete([row]); }

    private confirmAndDelete(items: unknown[]): void {
        const count = items.length;
        const first = items[0] as Record<string, unknown>;
        const label = ((first?.['Name'] ?? first?.['name'] ?? first?.[this.dataKey]) as string)?.toString();

        const data: ConfirmDialogData = {
            title: 'Löschen bestätigen',
            message:
                count === 1
                    ? (label ? `Möchten Sie „${label}" wirklich löschen?` : 'Möchten Sie den ausgewählten Datensatz wirklich löschen?')
                    : `Möchten Sie die ${count} ausgewählten Datensätze wirklich löschen?`,
            confirmText: 'Löschen',
            cancelText: 'Abbrechen',
            confirmColor: 'warn'
        };

        this.dialog.open(ConfirmDialogComponent, {
            data, width: '420px', autoFocus: false
        }).afterClosed().subscribe(res => {
            if (res) {
                for (const item of items) this.remove.emit(item);
                this.selection.clear();
                this.saveState({ selectionIds: [] });
                this.cdr.markForCheck();
            }
        });
    }

    // ---------- Drawer ----------
    saveChanges(): void {
        if (!this.selectedItem) return;
        this.save.emit(this.selectedItem);
        this.closeDrawer();
    }
    closeDrawer(): void { this.selectedItem = null; this.teardownOverlay(); }

    private openDrawer(item: Record<string, unknown>): void {
        this.selectedItem = item;

        // Close filter sidebar if open to prevent interference
        if (this.isFilterOpen) {
            this.closeFilter();
        }

        if (!this.overlayRef) {
            const cfg: OverlayConfig = {
                hasBackdrop: this.drawerBackdrop,
                backdropClass: 'gt-drawer-backdrop',
                panelClass: ['gt-drawer-panel'],
                width: this.drawerWidth,
                height: `calc(100vh - ${this.drawerTopOffset}px)`,
                scrollStrategy: this.overlay.scrollStrategies.block(),
                positionStrategy: this.overlay.position().global().top(`${this.drawerTopOffset}px`).right('0')
            };
            this.overlayRef = this.overlay.create(cfg);
            if (this.drawerBackdrop) this.overlayRef.backdropClick().subscribe(() => this.closeDrawer());
        } else {
            this.overlayRef.updateSize({ width: this.drawerWidth, height: `calc(100vh - ${this.drawerTopOffset}px)` });
            this.overlayRef.updatePositionStrategy(this.overlay.position().global().top(`${this.drawerTopOffset}px`).right('0'));
        }

        const portal = new TemplatePortal(this.drawerHostTpl, this.vcr, {
            $implicit: this.selectedItem,
            readonly: this.isReadonly
        });
        this.overlayRef.attach(portal);
        requestAnimationFrame(() => this.overlayRef?.addPanelClass('open'));
    }

    toggleDrawerReadonly(): void {
        this.isReadonly = !this.isReadonly;
        this.saveState({ readonly: this.isReadonly });

        if (this.overlayRef?.hasAttached()) {
            this.overlayRef.detach();
            const portal = new TemplatePortal(this.drawerHostTpl, this.vcr, {
                $implicit: this.selectedItem,
                readonly: this.isReadonly
            });
            this.overlayRef.attach(portal);
        }
    }

    private teardownOverlay(): void {
        if (!this.overlayRef) return;
        this.overlayRef.removePanelClass('open');
        setTimeout(() => this.overlayRef?.detach(), 200);
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

        // Close detail drawer if open to prevent interference
        if (this.overlayRef?.hasAttached()) {
            this.closeDrawer();
        }

        if (!this.filterOverlayRef) {
            const cfg: OverlayConfig = {
                hasBackdrop: this.drawerBackdrop,
                backdropClass: 'gt-drawer-backdrop',
                panelClass: ['gt-drawer-panel', 'gt-filter-panel'],
                width: this.drawerWidth,
                height: `calc(100vh - ${this.drawerTopOffset}px)`,
                scrollStrategy: this.overlay.scrollStrategies.block(),
                positionStrategy: this.overlay.position().global().top(`${this.drawerTopOffset}px`).right('0')
            };
            this.filterOverlayRef = this.overlay.create(cfg);
            if (this.drawerBackdrop) {
                this.filterOverlayRef.backdropClick().subscribe(() => this.closeFilter());
            }
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
        requestAnimationFrame(() => this.filterOverlayRef?.addPanelClass('open'));
    }

    closeFilter(): void {
        this.isFilterOpen = false;
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
        setTimeout(() => this.filterOverlayRef?.detach(), 200);
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
        if (this.useGlobalSearch && this.globalSearch) this.globalSearch.clear(this.searchChannel);
        this.globalFilter = '';
        this.saveState({ globalFilter: '' });
        this.search$.next('');
        this.isSearchOpen = false;
        this.cdr.markForCheck();
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

    // ---------- Selection ----------
    toggleRow(row: unknown): void {
        this.selection.toggle(row);
        this.persistSelection();
        this.cdr.markForCheck();
    }
    masterToggle(): void {
        this.isAllSelected() ? this.selection.clear() : this.data.forEach(r => this.selection.select(r));
        this.persistSelection();
        this.cdr.markForCheck();
    }
    isAllSelected(): boolean { return this.selection.selected.length > 0 && this.selection.selected.length === this.data.length; }

    private persistSelection(): void {
        const ids = this.selection.selected
            .map(r => (r as Record<string, unknown>)?.[this.dataKey])
            .filter(id => id !== null && id !== undefined) as (string | number)[];
        this.saveState({ selectionIds: ids });
    }

    private restoreSelectionFromState(): void {
        const st = this.tableState.loadExtraState(this.storageKey);
        const saved = st?.selectionIds ?? [];
        this.selection.clear();
        if (Array.isArray(saved) && saved.length && this.data?.length) {
            const set = new Set(saved.map(x => String(x)));
            for (const row of this.data) {
                const id = (row as Record<string, unknown>)?.[this.dataKey];
                if (id !== undefined && id !== null && set.has(String(id))) {
                    this.selection.select(row);
                }
            }
        }
        this.cdr.markForCheck();
    }

    /** Wiederherstellen nach Render: erst microtask, dann rAF, dann restore */
    private restoreSelectionDeferred(): void {
        Promise.resolve().then(() => {
            requestAnimationFrame(() => this.restoreSelectionFromState());
        });
    }

    // ---------- Footer: Gesamten State löschen ----------
    clearAllState(): void {
        // Storage leeren
        this.tableState.clearExtraState(this.storageKey);
        this.hasState = false;

        // Suche
        if (this.useGlobalSearch && this.globalSearch) this.globalSearch.clear(this.searchChannel);
        this.globalFilter = '';
        this.isSearchOpen = false;

        // Filter
        this.currentFilters = {};
        if (this.filterOverlayRef?.hasAttached()) this.closeFilter();

        // Auswahl
        this.selection.clear();

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

        // Schlösser
        this.editMode = false;
        this.isReadonly = true;

        // Drawer schließen, falls offen
        if (this.overlayRef?.hasAttached()) this.closeDrawer();

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
        if (st.selectionIds && st.selectionIds.length) return true;
        if (st.globalFilter) return true;
        if (st.sort && st.sort.active) return true;
        if ((st.pageIndex ?? 0) > 0) return true;
        if (st.pageSize && st.pageSize !== this.defaultPageSize) return true;
        if ((st.scrollTop ?? 0) > 0) return true;
        if (st.editMode) return true;
        if (st.readonly === false) return true;
        return false;
    }

    // ---------- Tastatur-Shortcuts ----------
    @HostListener('document:keydown', ['$event'])
    onKey(e: KeyboardEvent): void {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'f') {
            e.preventDefault();
            if (!this.isSearchOpen) this.toggleSearch();
            setTimeout(() => this.searchInput?.nativeElement?.select(), 0);
            return;
        }
        if (e.key.toLowerCase() === 'e' && this.canEditToolbar) { e.preventDefault(); this.onToolbarEdit(); return; }
        if (e.key === 'Delete' && this.canDeleteToolbar) { e.preventDefault(); this.onToolbarDelete(); return; }
    }
}
