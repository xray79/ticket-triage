import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { UsersApi } from '../../core/api/users.api';

@Component({
    selector: 'app-user-management',
    imports: [FormsModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
    <section class="admin">
      <h1>Add agent</h1>
      <form (ngSubmit)="submit()" aria-label="Register a new agent">
        <label for="displayName">Display name</label>
        <input id="displayName" name="displayName" [(ngModel)]="displayName" required />

        <label for="email">Email</label>
        <input id="email" name="email" type="email" [(ngModel)]="email" required />

        <label for="password">Temporary password</label>
        <input id="password" name="password" type="password" [(ngModel)]="password" required minlength="10" />

        <label for="role">Role</label>
        <select id="role" name="role" [(ngModel)]="role">
          <option value="Agent">Agent</option>
          <option value="Admin">Admin</option>
        </select>

        @if (message()) {
          <p role="status">{{ message() }}</p>
        }

        <button type="submit" [disabled]="submitting()">{{ submitting() ? 'Creating…' : 'Create user' }}</button>
      </form>
    </section>
  `,
    styles: [
        `
      .admin {
        max-width: 420px;
        margin: 2rem auto;
        padding: 0 1rem;
      }
      form {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }
      input,
      select {
        padding: 0.55rem 0.7rem;
        border: 1px solid #ccd2db;
        border-radius: 8px;
      }
      button {
        margin-top: 0.5rem;
        padding: 0.6rem;
        border: none;
        border-radius: 8px;
        background: #2563eb;
        color: white;
        font-weight: 600;
        cursor: pointer;
      }
      button:focus-visible,
      input:focus-visible,
      select:focus-visible {
        outline: 2px solid #163b7a;
        outline-offset: 2px;
      }
    `
    ]
})
export class UserManagementComponent {
  displayName = '';
  email = '';
  password = '';
  role = 'Agent';
  readonly submitting = signal(false);
  readonly message = signal<string | null>(null);

  constructor(private readonly usersApi: UsersApi) {}

  async submit(): Promise<void> {
    this.submitting.set(true);
    this.message.set(null);
    try {
      await this.usersApi.register({
        email: this.email,
        password: this.password,
        displayName: this.displayName,
        role: this.role
      });
      this.message.set(`Created ${this.displayName}.`);
      this.displayName = '';
      this.email = '';
      this.password = '';
    } catch {
      this.message.set('Could not create user — check the details and try again.');
    } finally {
      this.submitting.set(false);
    }
  }
}
