// 此檔案為路由佔位符，實際登入頁面為 login-page.component.ts
// Auth feature 不使用 list 頁面，保留此空殼符合架構規範
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-auth-list',
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet />`,
})
export class AuthListComponent {}
