import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { TicketsApi } from '../../core/api/tickets.api';
import { TicketDetail } from '../../core/models/ticket.models';
import { PriorityBadgeComponent } from '../../shared/ui/priority-badge.component';
import { ProviderBadgeComponent } from '../../shared/ui/provider-badge.component';

@Component({
    selector: 'app-ticket-detail',
    imports: [RouterLink, DatePipe, PriorityBadgeComponent, ProviderBadgeComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
    <section class="detail">
      <a routerLink="/tickets">&larr; Back to queue</a>

      @if (ticket(); as t) {
        <header>
          <h1>{{ t.subject }}</h1>
          <p class="detail__meta">
            From {{ t.customerEmail }} · {{ t.createdAtUtc | date: 'medium' }} · Status: {{ t.status }}
          </p>
        </header>

        <article class="detail__body" aria-label="Original ticket message">
          <h2>Original message</h2>
          <p>{{ t.body }}</p>
        </article>

        <article class="detail__triage" aria-label="Triage result">
          <h2>Triage</h2>
          @if (t.triage; as triage) {
            <div class="detail__badges">
              <app-priority-badge [priority]="triage.priority" />
              <app-provider-badge [provider]="triage.provider" [wasFallback]="triage.wasFallback" />
            </div>
            <p><strong>Category:</strong> {{ triage.category }}</p>
            <p><strong>Summary:</strong> {{ triage.summary }}</p>
            <h3>Draft reply</h3>
            <p class="detail__draft">{{ triage.draftReply }}</p>
          } @else if (t.status === 'TriageFailed') {
            <p role="alert">Automated triage failed for this ticket even after the local fallback. An agent can retry manually.</p>
          } @else {
            <p>Not yet triaged — this ticket is queued for async processing.</p>
          }
        </article>

        <div class="detail__actions">
          @if (auth.hasPermission('tickets:resolve') && t.status !== 'Resolved') {
            <button type="button" (click)="resolve()" [disabled]="acting()">
              {{ acting() ? 'Resolving…' : 'Mark resolved' }}
            </button>
          }
        </div>
      } @else {
        <p>Loading ticket…</p>
      }
    </section>
  `,
    styles: [
        `
      .detail {
        max-width: 720px;
        margin: 2rem auto;
        padding: 0 1rem;
      }
      .detail__meta {
        color: #5a6270;
        font-size: 0.9rem;
      }
      .detail__body,
      .detail__triage {
        margin-top: 1.5rem;
        padding: 1rem;
        border: 1px solid #dfe3e8;
        border-radius: 8px;
      }
      .detail__badges {
        display: flex;
        gap: 0.5rem;
        margin-bottom: 0.5rem;
      }
      .detail__draft {
        white-space: pre-wrap;
        background: #f7f8fa;
        padding: 0.75rem;
        border-radius: 6px;
      }
      .detail__actions {
        margin-top: 1.5rem;
      }
      button {
        padding: 0.55rem 1rem;
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
      button:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
    `
    ]
})
export class TicketDetailComponent implements OnInit {
  readonly ticket = signal<TicketDetail | null>(null);
  readonly acting = signal(false);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly ticketsApi: TicketsApi,
    readonly auth: AuthService
  ) {}

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id')!;
    await this.load(id);
  }

  private async load(id: string): Promise<void> {
    this.ticket.set(await this.ticketsApi.get(id));
  }

  async resolve(): Promise<void> {
    const t = this.ticket();
    if (!t) return;
    this.acting.set(true);
    try {
      await this.ticketsApi.resolve(t.id);
      await this.load(t.id);
    } finally {
      this.acting.set(false);
    }
  }
}
