import { Component } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { TuiButton, TuiRoot } from '@taiga-ui/core';
import { AuthService } from './core/auth/auth.service';

@Component({
    selector: 'app-root',
    imports: [RouterOutlet, RouterLink, TuiRoot, TuiButton],
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss'
})
export class AppComponent {
  constructor(
    readonly auth: AuthService,
    private readonly router: Router
  ) {}

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
