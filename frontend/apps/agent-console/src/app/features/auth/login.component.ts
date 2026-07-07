import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
    selector: 'app-login',
    imports: [FormsModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
    <main class="login">
      <form class="login__card" (ngSubmit)="submit()" aria-labelledby="login-heading">
        <h1 id="login-heading">Ticket Triage — Sign in</h1>

        <label for="email">Email</label>
        <input id="email" name="email" type="email" [(ngModel)]="email" required autocomplete="username" />

        <label for="password">Password</label>
        <input
          id="password"
          name="password"
          type="password"
          [(ngModel)]="password"
          required
          autocomplete="current-password"
        />

        @if (error()) {
          <p class="login__error" role="alert">{{ error() }}</p>
        }

        <button type="submit" [disabled]="submitting()">
          {{ submitting() ? 'Signing in…' : 'Sign in' }}
        </button>
      </form>
    </main>
  `,
    styles: [
        `
      .login {
        min-height: 100vh;
        display: flex;
        align-items: center;
        justify-content: center;
        background: #f4f5f7;
      }
      .login__card {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
        width: min(360px, 90vw);
        padding: 2rem;
        background: white;
        border-radius: 12px;
        box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
      }
      label {
        font-size: 0.85rem;
        font-weight: 600;
      }
      input {
        padding: 0.6rem 0.75rem;
        border: 1px solid #ccd2db;
        border-radius: 8px;
        font-size: 1rem;
      }
      input:focus-visible {
        outline: 2px solid #2563eb;
        outline-offset: 1px;
      }
      button {
        margin-top: 0.5rem;
        padding: 0.65rem;
        border: none;
        border-radius: 8px;
        background: #2563eb;
        color: white;
        font-weight: 600;
        cursor: pointer;
      }
      button:focus-visible {
        outline: 2px solid #163b7a;
        outline-offset: 2px;
      }
      button:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
      .login__error {
        color: #7a1717;
        font-size: 0.85rem;
        margin: 0;
      }
    `
    ]
})
export class LoginComponent {
  email = '';
  password = '';
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router
  ) {}

  async submit(): Promise<void> {
    this.error.set(null);
    this.submitting.set(true);
    try {
      await this.auth.login({ email: this.email, password: this.password });
      await this.router.navigate(['/tickets']);
    } catch {
      this.error.set('Invalid email or password.');
    } finally {
      this.submitting.set(false);
    }
  }
}
