import { Component, signal, HostListener, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from '../header/header.component';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { BreadcrumbComponent } from '../breadcrumb/breadcrumb.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, SidebarComponent, BreadcrumbComponent],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss',
})
export class MainLayoutComponent implements OnInit {
  readonly sidebarCollapsed = signal(false);
  readonly mobileMenuOpen = signal(false);
  readonly isMobile = signal(false);

  ngOnInit(): void {
    this.detectScreen();
  }

  @HostListener('window:resize')
  onResize(): void {
    this.detectScreen();
  }

  private detectScreen(): void {
    const w = window.innerWidth;
    this.isMobile.set(w < 768);
    if (w < 768) {
      this.mobileMenuOpen.set(false);
    } else if (w < 1280) {
      this.sidebarCollapsed.set(true);
    }
  }

  get isCollapsed(): boolean {
    return this.isMobile() || this.sidebarCollapsed();
  }

  toggleSidebar(): void {
    if (this.isMobile()) {
      this.mobileMenuOpen.update((v) => !v);
    } else {
      this.sidebarCollapsed.update((v) => !v);
    }
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen.set(false);
  }
}
