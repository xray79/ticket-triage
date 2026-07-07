import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
  selector: 'app-provider-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="inline-flex items-center rounded-full border border-blue-300 bg-blue-100 px-2.5 py-0.5 text-xs font-semibold text-blue-900"
      role="status"
    >
      {{ providerLabel() }}
      @if (wasFallback) {
        <span class="ml-1 font-medium italic"> (local fallback used)</span>
      }
    </span>
  `
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
