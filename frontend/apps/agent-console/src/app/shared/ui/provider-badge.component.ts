import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
  selector: 'app-provider-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="badge" role="status">
      {{ providerLabel() }}
      @if (wasFallback) {
        <span class="badge__fallback"> (local fallback used)</span>
      }
    </span>
  `,
  styles: [
    `
      .badge {
        display: inline-flex;
        align-items: center;
        padding: 0.15rem 0.55rem;
        border-radius: 999px;
        font-size: 0.75rem;
        font-weight: 600;
        background: #e6eefc;
        color: #163b7a;
        border: 1px solid #b3caf0;
      }
      .badge__fallback {
        font-weight: 500;
        font-style: italic;
      }
    `
  ]
})
export class ProviderBadgeComponent {
  @Input({ required: true }) provider: string | null = null;
  @Input() wasFallback = false;

  providerLabel(): string {
    switch ((this.provider ?? '').toLowerCase()) {
      case 'local':
        return 'Local (private)';
      case 'openai':
        return 'OpenAI';
      case 'anthropic':
        return 'Anthropic';
      case 'gemini':
        return 'Gemini';
      default:
        return 'Not yet triaged';
    }
  }
}
