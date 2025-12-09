import { Directive, Input, ElementRef, Renderer2, OnInit, OnChanges, SimpleChanges } from '@angular/core';

/**
 * Directive to make form fields readonly.
 * Applies readonly attribute and styling to form controls.
 *
 * Usage:
 * ```html
 * <div [myiotgridReadonlyFields]="isReadonly">
 *   <mat-form-field>
 *     <input matInput formControlName="name">
 *   </mat-form-field>
 * </div>
 * ```
 */
@Directive({
  selector: '[myiotgridReadonlyFields]',
  standalone: true
})
export class ReadonlyFieldsDirective implements OnInit, OnChanges {
  @Input('myiotgridReadonlyFields') readonly = false;

  constructor(
    private el: ElementRef<HTMLElement>,
    private renderer: Renderer2
  ) {}

  ngOnInit(): void {
    this.applyReadonly();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['readonly']) {
      this.applyReadonly();
    }
  }

  private applyReadonly(): void {
    const inputs = this.el.nativeElement.querySelectorAll(
      'input, textarea, select, mat-select, mat-checkbox, mat-slide-toggle, mat-radio-group'
    );

    inputs.forEach(input => {
      if (this.readonly) {
        this.renderer.setAttribute(input, 'readonly', 'true');
        this.renderer.setAttribute(input, 'disabled', 'true');
        this.renderer.addClass(input, 'readonly-field');
      } else {
        this.renderer.removeAttribute(input, 'readonly');
        this.renderer.removeAttribute(input, 'disabled');
        this.renderer.removeClass(input, 'readonly-field');
      }
    });

    // Also handle mat-form-field containers
    const formFields = this.el.nativeElement.querySelectorAll('mat-form-field');
    formFields.forEach(field => {
      if (this.readonly) {
        this.renderer.addClass(field, 'readonly-form-field');
      } else {
        this.renderer.removeClass(field, 'readonly-form-field');
      }
    });

    // Handle buttons (hide in readonly mode except view buttons)
    const buttons = this.el.nativeElement.querySelectorAll('button:not(.view-button)');
    buttons.forEach(button => {
      if (this.readonly) {
        this.renderer.setStyle(button, 'display', 'none');
      } else {
        this.renderer.removeStyle(button, 'display');
      }
    });
  }
}
