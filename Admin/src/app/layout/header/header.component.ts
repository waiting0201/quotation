import { Component, inject, input, output } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
})
export class HeaderComponent {
  private readonly auth = inject(AuthService);

  readonly sidebarCollapsed = input<boolean>(false);
  readonly isMobile = input<boolean>(false);
  readonly toggleSidebar = output<void>();

  readonly currentUser = this.auth.currentUser;

  onToggle(): void {
    this.toggleSidebar.emit();
  }

  onLogout(): void {
    this.auth.logout();
  }
}
