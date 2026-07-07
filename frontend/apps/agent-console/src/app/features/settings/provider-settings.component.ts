import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PreferencesApi } from '../../core/api/preferences.api';

interface ProviderOption {
  key: string;
  label: string;
  description: string;
}

@Component({
    selector: 'app-provider-settings',
    imports: [FormsModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
    <section class="settings">
      <h1>Triage provider</h1>
      <p class="settings__intro">
        Every ticket is redacted before triage, regardless of provider. Local keeps everything
        on this server. Opting into a cloud provider sends the <strong>redacted</strong> ticket
        text only — never the original — for higher-quality triage.
      </p>

      @if (loading()) {
        <p>Loading…</p>
      } @else {
        <fieldset>
          <legend>Default provider for new tickets</legend>
          @for (option of providerOptions; track option.key) {
            <label class="settings__option">
              <input
                type="radio"
                name="provider"
                [value]="option.key"
                [(ngModel)]="selectedProvider"
                (ngModelChange)="save()"
              />
              <span>
                <strong>{{ option.label }}</strong>
                <br />
                <small>{{ option.description }}</small>
              </span>
            </label>
          }
        </fieldset>

        @if (saved()) {
          <p role="status">Saved.</p>
        }
      }
    </section>
  `,
    styles: [
        `
      .settings {
        max-width: 560px;
        margin: 2rem auto;
        padding: 0 1rem;
      }
      .settings__intro {
        color: #4a4f57;
      }
      fieldset {
        border: 1px solid #dfe3e8;
        border-radius: 8px;
        padding: 1rem;
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
      }
      .settings__option {
        display: flex;
        gap: 0.6rem;
        align-items: flex-start;
        cursor: pointer;
      }
      input[type='radio'] {
        margin-top: 0.2rem;
      }
      input:focus-visible {
        outline: 2px solid #163b7a;
        outline-offset: 2px;
      }
    `
    ]
})
export class ProviderSettingsComponent implements OnInit {
  readonly loading = signal(true);
  readonly saved = signal(false);
  selectedProvider = 'local';

  readonly providerOptions: ProviderOption[] = [
    { key: 'local', label: 'Local (private, default)', description: 'Never leaves this server.' },
    { key: 'openai', label: 'OpenAI (opt-in)', description: 'Redacted ticket text sent to OpenAI for triage.' },
    { key: 'anthropic', label: 'Anthropic (opt-in)', description: 'Redacted ticket text sent to Anthropic for triage.' },
    { key: 'gemini', label: 'Gemini (opt-in)', description: 'Redacted ticket text sent to Google Gemini for triage.' }
  ];

  constructor(private readonly preferencesApi: PreferencesApi) {}

  async ngOnInit(): Promise<void> {
    const current = await this.preferencesApi.getMyProviderPreference();
    this.selectedProvider = current.providerPreference;
    this.loading.set(false);
  }

  async save(): Promise<void> {
    this.saved.set(false);
    await this.preferencesApi.setMyProviderPreference(this.selectedProvider);
    this.saved.set(true);
  }
}
