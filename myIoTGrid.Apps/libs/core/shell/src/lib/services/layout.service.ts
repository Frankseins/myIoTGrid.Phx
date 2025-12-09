import { Injectable, computed, signal, inject, DestroyRef } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';

/**
 * Layout breakpoint thresholds
 */
export const LAYOUT_BREAKPOINTS = {
  /** Mobile: < 768px */
  MOBILE: '(max-width: 767px)',
  /** Tablet: 768px - 1023px */
  TABLET: '(min-width: 768px) and (max-width: 1023px)',
  /** Desktop: >= 1024px */
  DESKTOP: '(min-width: 1024px)',
  /** Large Desktop: >= 1920px */
  LARGE_DESKTOP: '(min-width: 1920px)',
} as const;

/**
 * Layout mode enum for explicit state
 */
export type LayoutMode = 'mobile' | 'tablet' | 'desktop';

/**
 * LayoutService - Central service for responsive layout management
 *
 * Provides reactive signals for breakpoint detection using Angular CDK BreakpointObserver.
 * All components can inject this service to react to device size changes.
 *
 * @example
 * ```typescript
 * @Component({...})
 * export class MyComponent {
 *   private layout = inject(LayoutService);
 *
 *   // Use signals in templates
 *   isMobile = this.layout.isMobile;
 *   isDesktop = this.layout.isDesktop;
 *
 *   // Computed values based on layout
 *   chartHeight = computed(() => this.layout.isMobile() ? 200 : 400);
 * }
 * ```
 */
@Injectable({
  providedIn: 'root'
})
export class LayoutService {
  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly destroyRef = inject(DestroyRef);

  /**
   * Internal sidenav state
   */
  private readonly _sidenavOpen = signal<boolean>(true);

  /**
   * Mobile breakpoint signal (< 768px)
   */
  readonly isMobile = toSignal(
    this.breakpointObserver.observe(LAYOUT_BREAKPOINTS.MOBILE).pipe(
      map(result => result.matches)
    ),
    { initialValue: false }
  );

  /**
   * Tablet breakpoint signal (768px - 1023px)
   */
  readonly isTablet = toSignal(
    this.breakpointObserver.observe(LAYOUT_BREAKPOINTS.TABLET).pipe(
      map(result => result.matches)
    ),
    { initialValue: false }
  );

  /**
   * Desktop breakpoint signal (>= 1024px)
   */
  readonly isDesktop = toSignal(
    this.breakpointObserver.observe(LAYOUT_BREAKPOINTS.DESKTOP).pipe(
      map(result => result.matches)
    ),
    { initialValue: true }
  );

  /**
   * Large Desktop breakpoint signal (>= 1920px)
   */
  readonly isLargeDesktop = toSignal(
    this.breakpointObserver.observe(LAYOUT_BREAKPOINTS.LARGE_DESKTOP).pipe(
      map(result => result.matches)
    ),
    { initialValue: false }
  );

  /**
   * Handset (portrait phone) using Angular CDK Breakpoints
   */
  readonly isHandset = toSignal(
    this.breakpointObserver.observe(Breakpoints.Handset).pipe(
      map(result => result.matches)
    ),
    { initialValue: false }
  );

  /**
   * Current layout mode as computed signal
   */
  readonly layoutMode = computed<LayoutMode>(() => {
    if (this.isMobile()) return 'mobile';
    if (this.isTablet()) return 'tablet';
    return 'desktop';
  });

  /**
   * Sidenav open state (for desktop/tablet rail navigation)
   */
  readonly sidenavOpen = computed(() => {
    // Auto-close on mobile
    if (this.isMobile()) return false;
    return this._sidenavOpen();
  });

  /**
   * Whether to show bottom navigation (mobile only)
   */
  readonly showBottomNav = computed(() => this.isMobile());

  /**
   * Whether to show side navigation (desktop/tablet)
   */
  readonly showSideNav = computed(() => !this.isMobile());

  /**
   * Content padding based on device
   */
  readonly contentPadding = computed(() => {
    if (this.isMobile()) return 16;
    if (this.isTablet()) return 20;
    return 24;
  });

  /**
   * Grid columns for responsive layouts
   */
  readonly gridColumns = computed(() => {
    if (this.isMobile()) return 1;
    if (this.isTablet()) return 2;
    if (this.isLargeDesktop()) return 4;
    return 3;
  });

  constructor() {
    // Auto-close sidenav when switching to mobile
    this.breakpointObserver.observe(LAYOUT_BREAKPOINTS.MOBILE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => {
        if (result.matches) {
          this._sidenavOpen.set(false);
        }
      });
  }

  /**
   * Toggle sidenav open/closed state
   */
  toggleSidenav(): void {
    this._sidenavOpen.update(open => !open);
  }

  /**
   * Open sidenav
   */
  openSidenav(): void {
    this._sidenavOpen.set(true);
  }

  /**
   * Close sidenav
   */
  closeSidenav(): void {
    this._sidenavOpen.set(false);
  }

  /**
   * Check if current viewport matches a specific breakpoint query
   * @param query Media query string to check
   * @returns Observable<boolean> that emits when match state changes
   */
  matchesQuery(query: string) {
    return this.breakpointObserver.observe(query).pipe(
      map(result => result.matches)
    );
  }
}
