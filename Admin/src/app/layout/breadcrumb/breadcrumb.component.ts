import { Component, inject } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';

export interface BreadcrumbItem {
  label: string;
  route?: string;
}

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './breadcrumb.component.html',
  styleUrl: './breadcrumb.component.scss',
})
export class BreadcrumbComponent {
  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);

  readonly breadcrumbs = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      startWith(null),
      map(() => this._buildBreadcrumbs(this.activatedRoute.root))
    ),
    { initialValue: [] as BreadcrumbItem[] }
  );

  private _buildBreadcrumbs(
    route: ActivatedRoute,
    url = '',
    crumbs: BreadcrumbItem[] = []
  ): BreadcrumbItem[] {
    const children = route.children;
    if (children.length === 0) return crumbs;

    for (const child of children) {
      if (!child.snapshot) continue;
      const segments = child.snapshot.url.map((s) => s.path);
      const path = segments.length ? `${url}/${segments.join('/')}` : url;
      const label: string | undefined = child.snapshot.data['breadcrumb'];

      if (label) {
        crumbs.push({ label, route: path });
      }

      this._buildBreadcrumbs(child, path, crumbs);
    }

    return crumbs;
  }
}
