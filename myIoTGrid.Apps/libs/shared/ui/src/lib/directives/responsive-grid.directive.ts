import { Directive, Input, HostBinding, inject, computed, signal, effect } from '@angular/core';
import { LayoutService } from '@myiotgrid/core/shell';

/**
 * ResponsiveGridDirective - Automatically adjusts grid columns based on viewport
 *
 * Uses LayoutService to detect breakpoints and sets CSS grid-template-columns.
 *
 * @example
 * ```html
 * <div class="widget-grid"
 *      myiotgridResponsiveGrid
 *      [desktopCols]="3"
 *      [tabletCols]="2"
 *      [mobileCols]="1"
 *      [gap]="24">
 *   <app-widget />
 *   <app-widget />
 *   <app-widget />
 * </div>
 * ```
 */
@Directive({
  selector: '[myiotgridResponsiveGrid]',
  standalone: true
})
export class ResponsiveGridDirective {
  private readonly layout = inject(LayoutService);

  /** Number of columns on desktop (>= 1024px) */
  @Input() desktopCols = 3;

  /** Number of columns on tablet (768px - 1023px) */
  @Input() tabletCols = 2;

  /** Number of columns on mobile (< 768px) */
  @Input() mobileCols = 1;

  /** Gap between grid items in pixels */
  @Input() gap = 24;

  /** Large desktop columns (>= 1920px) */
  @Input() largeDesktopCols?: number;

  /** Grid template columns computed based on current breakpoint */
  private readonly gridColumns = computed(() => {
    const cols = this.getCurrentCols();
    return `repeat(${cols}, 1fr)`;
  });

  /** Grid gap computed (smaller on mobile) */
  private readonly gridGap = computed(() => {
    if (this.layout.isMobile()) {
      return Math.min(this.gap, 16);
    }
    if (this.layout.isTablet()) {
      return Math.min(this.gap, 20);
    }
    return this.gap;
  });

  @HostBinding('style.display') display = 'grid';

  @HostBinding('style.grid-template-columns')
  get templateColumns(): string {
    return this.gridColumns();
  }

  @HostBinding('style.gap.px')
  get gapPx(): number {
    return this.gridGap();
  }

  @HostBinding('style.width') width = '100%';

  /**
   * Get current column count based on breakpoint
   */
  private getCurrentCols(): number {
    if (this.layout.isMobile()) {
      return this.mobileCols;
    }
    if (this.layout.isTablet()) {
      return this.tabletCols;
    }
    if (this.layout.isLargeDesktop() && this.largeDesktopCols) {
      return this.largeDesktopCols;
    }
    return this.desktopCols;
  }
}
