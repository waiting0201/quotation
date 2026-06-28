import {
  Directive,
  inject,
  input,
  effect,
  TemplateRef,
  ViewContainerRef,
} from '@angular/core';
import { PermissionService, PermissionAction } from '../../core/services/permission.service';

/**
 * 使用方式：
 * <button *appPermission="'Customer'; action: 'insert'">新增</button>
 */
@Directive({
  selector: '[appPermission]',
  standalone: true,
})
export class PermissionDirective {
  private readonly perm = inject(PermissionService);
  private readonly tpl = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);

  readonly appPermission = input.required<string>();
  readonly appPermissionAction = input<PermissionAction>('query');

  constructor() {
    effect(() => {
      const key = this.appPermission();
      const action = this.appPermissionAction();
      this.vcr.clear();
      if (this.perm.hasPermission(key, action)) {
        this.vcr.createEmbeddedView(this.tpl);
      }
    });
  }
}
