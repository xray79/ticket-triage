import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { ReportingApi, ReportingSummary } from '../../core/api/reporting.api';

@Component({
  selector: 'app-reporting-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="viz-root dashboard">
      <h1>Reporting</h1>

      @if (loading()) {
        <p>Loading…</p>
      } @else if (summary()) {
        @let s = summary()!;
        <div class="dashboard__tiles" role="list">
          <div class="tile" role="listitem">
            <span class="tile__value">{{ s.totalTickets }}</span>
            <span class="tile__label">Total tickets</span>
          </div>
          <div class="tile" role="listitem">
            <span class="tile__value">{{ s.newCount }}</span>
            <span class="tile__label">Not yet triaged</span>
          </div>
          <div class="tile" role="listitem">
            <span class="tile__value">{{ s.triagedCount }}</span>
            <span class="tile__label">Triaged</span>
          </div>
          <div class="tile" role="listitem">
            <span class="tile__value">{{ s.resolvedCount }}</span>
            <span class="tile__label">Resolved</span>
          </div>
          <div class="tile" role="listitem">
            <span class="tile__value">{{ s.triageFailedCount }}</span>
            <span class="tile__label">Triage failed</span>
          </div>
          <div class="tile" role="listitem">
            <span class="tile__value">{{ formatLatency(s.averageTriageLatencySeconds) }}</span>
            <span class="tile__label">Avg. triage latency</span>
          </div>
        </div>

        <h2>Tickets triaged by provider</h2>
        @if (s.byProvider.length === 0) {
          <p>No tickets have been triaged yet.</p>
        } @else {
          <table class="sr-only">
            <caption>Tickets triaged by provider, with fallback counts</caption>
            <thead>
              <tr>
                <th scope="col">Provider</th>
                <th scope="col">Count</th>
                <th scope="col">Fallback count</th>
              </tr>
            </thead>
            <tbody>
              @for (p of s.byProvider; track p.provider) {
                <tr>
                  <td>{{ p.provider }}</td>
                  <td>{{ p.count }}</td>
                  <td>{{ p.fallbackCount }}</td>
                </tr>
              }
            </tbody>
          </table>

          <div class="barchart" aria-hidden="true">
            @for (p of s.byProvider; track p.provider) {
              <div class="barchart__row">
                <span class="barchart__label">{{ p.provider }}</span>
                <div class="barchart__track">
                  <div
                    class="barchart__bar"
                    [style.width.%]="percentOf(p.count, s.byProvider)"
                    [attr.title]="p.count + ' triaged' + (p.fallbackCount > 0 ? ', ' + p.fallbackCount + ' via local fallback' : '')"
                  ></div>
                </div>
                <span class="barchart__value">
                  {{ p.count }}
                  @if (p.fallbackCount > 0) {
                    <small>({{ p.fallbackCount }} fallback)</small>
                  }
                </span>
              </div>
            }
          </div>
        }
      }
    </section>
  `,
  styles: [
    `
      .viz-root {
        --surface-1: #fcfcfb;
        --text-primary: #0b0b0b;
        --text-secondary: #52514e;
        --series-1: #2a78d6;
        --border: #dfe3e8;
      }
      @media (prefers-color-scheme: dark) {
        .viz-root {
          --surface-1: #1a1a19;
          --text-primary: #ffffff;
          --text-secondary: #c3c2b7;
          --series-1: #3987e5;
          --border: #3a3a38;
        }
      }

      .dashboard {
        max-width: 800px;
        margin: 2rem auto;
        padding: 0 1rem;
        color: var(--text-primary);
      }

      .dashboard__tiles {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
        gap: 0.75rem;
        margin: 1rem 0 2rem;
      }
      .tile {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        padding: 1rem;
        border: 1px solid var(--border);
        border-radius: 8px;
      }
      .tile__value {
        font-size: 1.75rem;
        font-weight: 700;
        font-variant-numeric: tabular-nums;
      }
      .tile__label {
        font-size: 0.8rem;
        color: var(--text-secondary);
      }

      .barchart {
        display: flex;
        flex-direction: column;
        gap: 0.6rem;
        margin-top: 1rem;
      }
      .barchart__row {
        display: grid;
        grid-template-columns: 100px 1fr 120px;
        align-items: center;
        gap: 0.75rem;
      }
      .barchart__label {
        font-size: 0.85rem;
        text-transform: capitalize;
      }
      .barchart__track {
        background: var(--border);
        border-radius: 4px;
        height: 8px;
        overflow: hidden;
      }
      .barchart__bar {
        background: var(--series-1);
        height: 100%;
        border-radius: 4px;
        min-width: 4px;
        transition: width 0.2s ease;
      }
      .barchart__value {
        font-size: 0.85rem;
        font-variant-numeric: tabular-nums;
      }
      .barchart__value small {
        color: var(--text-secondary);
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
export class ReportingDashboardComponent implements OnInit {
  readonly loading = signal(true);
  readonly summary = signal<ReportingSummary | null>(null);

  constructor(private readonly reportingApi: ReportingApi) {}

  async ngOnInit(): Promise<void> {
    this.summary.set(await this.reportingApi.getSummary());
    this.loading.set(false);
  }

  percentOf(count: number, all: { count: number }[]): number {
    const max = Math.max(...all.map((p) => p.count), 1);
    return (count / max) * 100;
  }

  formatLatency(seconds: number | null): string {
    if (seconds === null) return '—';
    if (seconds < 60) return `${Math.round(seconds)}s`;
    return `${Math.round(seconds / 60)}m`;
  }
}
