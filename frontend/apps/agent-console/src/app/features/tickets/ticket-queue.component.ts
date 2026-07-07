import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { PreferencesApi } from '../../core/api/preferences.api';
import { TicketsApi } from '../../core/api/tickets.api';
import { TicketSummary } from '../../core/models/ticket.models';
import { PriorityBadgeComponent } from '../../shared/ui/priority-badge.component';

@Component({
  selector: 'app-ticket-queue',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe, PriorityBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="queue">
      <header class="queue__header">
        <h1>Ticket queue</h1>
        @if (auth.hasPermission('tickets:triage')) {
          <button type="button" (click)="showForm.set(!showForm())">
            {{ showForm() ? 'Cancel' : 'New ticket' }}
          </button>
        }
      </header>

      @if (showForm()) {
        <form class="queue__form" (ngSubmit)="createTicket()" aria-label="Create a new ticket">
          <label for="subject">Subject</label>
          <input id="subject" name="subject" [(ngModel)]="newSubject" required />

          <label for="customerEmail">Customer email</label>
          <input id="customerEmail" name="customerEmail" type="email" [(ngModel)]="newCustomerEmail" required />

          <label for="body">Description</label>
          <textarea id="body" name="body" rows="4" [(ngModel)]="newBody" required></textarea>

          <label for="provider">Triage provider</label>
          <select id="provider" name="provider" [(ngModel)]="newProvider">
            <option value="local">Local (private, default)</option>
            <option value="openai">OpenAI (opt-in)</option>
            <option value="anthropic">Anthropic (opt-in)</option>
            <option value="gemini">Gemini (opt-in)</option>
          </select>
          <small><a routerLink="/settings/provider">Change your default provider</a></small>

          <button type="submit" [disabled]="creating()">{{ creating() ? 'Creating…' : 'Create ticket' }}</button>
        </form>
      }

      @if (loading()) {
        <p>Loading tickets…</p>
      } @else if (tickets().length === 0) {
        <p>No tickets yet.</p>
      } @else {
        <table>
          <caption class="sr-only">Support ticket queue</caption>
          <thead>
            <tr>
              <th scope="col">Subject</th>
              <th scope="col">Customer</th>
              <th scope="col">Status</th>
              <th scope="col">Priority</th>
              <th scope="col">Created</th>
            </tr>
          </thead>
          <tbody>
            @for (ticket of tickets(); track ticket.id) {
              <tr>
                <td><a [routerLink]="['/tickets', ticket.id]">{{ ticket.subject }}</a></td>
                <td>{{ ticket.customerEmail }}</td>
                <td>{{ ticket.status }}</td>
                <td><app-priority-badge [priority]="ticket.priority" /></td>
                <td>{{ ticket.createdAtUtc | date: 'short' }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `,
  styles: [
    `
      .queue {
        max-width: 960px;
        margin: 2rem auto;
        padding: 0 1rem;
      }
      .queue__header {
        display: flex;
        align-items: center;
        justify-content: space-between;
      }
      .queue__form {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        margin: 1rem 0;
        padding: 1rem;
        border: 1px solid #dfe3e8;
        border-radius: 8px;
      }
      table {
        width: 100%;
        border-collapse: collapse;
      }
      th,
      td {
        text-align: left;
        padding: 0.5rem;
        border-bottom: 1px solid #eaecef;
      }
      button {
        padding: 0.5rem 1rem;
        border: none;
        border-radius: 8px;
        background: #2563eb;
        color: white;
        font-weight: 600;
        cursor: pointer;
      }
      button:focus-visible,
      a:focus-visible {
        outline: 2px solid #163b7a;
        outline-offset: 2px;
      }
      .sr-only {
        position: absolute;
        width: 1px;
        height: 1px;
        overflow: hidden;
        clip: rect(0 0 0 0);
      }
    `
  ]
})
export class TicketQueueComponent implements OnInit {
  readonly tickets = signal<TicketSummary[]>([]);
  readonly loading = signal(true);
  readonly showForm = signal(false);
  readonly creating = signal(false);

  newSubject = '';
  newBody = '';
  newCustomerEmail = '';
  newProvider = 'local';

  constructor(
    private readonly ticketsApi: TicketsApi,
    private readonly preferencesApi: PreferencesApi,
    readonly auth: AuthService
  ) {}

  async ngOnInit(): Promise<void> {
    await this.reload();
    const preference = await this.preferencesApi.getMyProviderPreference();
    this.newProvider = preference.providerPreference;
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    try {
      this.tickets.set(await this.ticketsApi.list());
    } finally {
      this.loading.set(false);
    }
  }

  async createTicket(): Promise<void> {
    this.creating.set(true);
    try {
      await this.ticketsApi.create({
        subject: this.newSubject,
        body: this.newBody,
        customerEmail: this.newCustomerEmail,
        requestedProvider: this.newProvider
      });
      this.newSubject = '';
      this.newBody = '';
      this.newCustomerEmail = '';
      this.showForm.set(false);
      await this.reload();
    } finally {
      this.creating.set(false);
    }
  }
}
