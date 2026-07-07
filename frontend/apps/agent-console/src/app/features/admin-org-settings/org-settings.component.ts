import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PreferencesApi } from '../../core/api/preferences.api';

@Component({
  selector: 'app-org-settings',
  standalone: true,
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="org-settings">
      <h1>Organization policy</h1>

      @if (loading()) {
        <p>Loading…</p>
      } @else {
        <label class="org-settings__toggle">
          <input type="checkbox" [(ngModel)]="forceLocalOnly" (ngModelChange)="save()" />
          <span>
            <strong>Force local-only triage</strong>
            <br />
            <small>
              When enabled, every ticket triages locally regardless of any agent's saved
              provider preference or per-ticket choice. Use this if the organization can't
              allow any cloud escalation at all.
            </small>
          </span>
        </label>

        @if (saved()) {
          <p role="status">Saved.</p>
        }
      }
    </section>
  `,
  styles: [
    `
      .org-settings {
        max-width: 560px;
        margin: 2rem auto;
        padding: 0 1rem;
      }
      .org-settings__toggle {
        display: flex;
        gap: 0.6rem;
        align-items: flex-start;
        cursor: pointer;
        border: 1px solid #dfe3e8;
        border-radius: 8px;
        padding: 1rem;
      }
      input[type='checkbox'] {
        margin-top: 0.2rem;
      }
      input:focus-visible {
        outline: 2px solid #163b7a;
        outline-offset: 2px;
      }
    `
  ]
})
export class OrgSettingsComponent implements OnInit {
  readonly loading = signal(true);
  readonly saved = signal(false);
  forceLocalOnly = false;

  constructor(private readonly preferencesApi: PreferencesApi) {}

  async ngOnInit(): Promise<void> {
    const current = await this.preferencesApi.getOrgSettings();
    this.forceLocalOnly = current.forceLocalOnly;
    this.loading.set(false);
  }

  async save(): Promise<void> {
    this.saved.set(false);
    await this.preferencesApi.setOrgSettings({ forceLocalOnly: this.forceLocalOnly });
    this.saved.set(true);
  }
}
