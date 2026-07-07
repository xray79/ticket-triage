import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
  selector: 'app-priority-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="badge" [class]="'badge--' + normalized()" role="status">{{ label() }}</span>`,
  styles: [
    `
      .badge {
        display: inline-flex;
        align-items: center;
        gap: 0.25rem;
        padding: 0.15rem 0.55rem;
        border-radius: 999px;
        font-size: 0.75rem;
        font-weight: 600;
        border: 1px solid transparent;
      }
      .badge--urgent {
        background: #fde2e1;
        color: #7a1717;
        border-color: #f3a8a5;
      }
      .badge--high {
        background: #fde8cf;
        color: #7a4a10;
        border-color: #f0c383;
      }
      .badge--medium {
        background: #fff6cf;
        color: #6b5900;
        border-color: #e8d778;
      }
      .badge--low {
        background: #e1f3e3;
        color: #1c5c28;
        border-color: #a7d9ae;
      }
      .badge--unknown {
        background: #eceef1;
        color: #4a4f57;
        border-color: #cfd4db;
      }
    `
  ]
})
export class PriorityBadgeComponent {
  @Input({ required: true }) priority: string | null = null;

  normalized(): string {
    const p = (this.priority ?? '').toLowerCase();
    return ['urgent', 'high', 'medium', 'low'].includes(p) ? p : 'unknown';
  }

  label(): string {
    const p = this.priority;
    return p ? p.charAt(0).toUpperCase() + p.slice(1) : 'Not yet triaged';
  }
}
