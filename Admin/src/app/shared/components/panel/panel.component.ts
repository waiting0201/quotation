import { Component, input } from '@angular/core';

@Component({
  selector: 'app-panel',
  standalone: true,
  template: `
    <div class="bg-white rounded-[var(--radius-panel)] shadow-panel overflow-hidden">
      @if (title()) {
        <div class="px-5 py-3.5 border-b border-slate-100">
          <h3 class="text-base font-semibold text-slate-800">{{ title() }}</h3>
        </div>
      }
      <div class="p-5">
        <ng-content />
      </div>
    </div>
  `,
})
export class PanelComponent {
  readonly title = input<string>('');
}
