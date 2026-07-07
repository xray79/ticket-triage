import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

const BADGE_CLASSES: Record<string, string> = {
  urgent: 'bg-red-100 text-red-900 border-red-300',
  high: 'bg-orange-100 text-orange-900 border-orange-300',
  medium: 'bg-yellow-100 text-yellow-900 border-yellow-300',
  low: 'bg-green-100 text-green-900 border-green-300',
  unknown: 'bg-slate-100 text-slate-700 border-slate-300'
};

@Component({
  selector: 'app-priority-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="inline-flex items-center gap-1 rounded-full border px-2.5 py-0.5 text-xs font-semibold"
      [class]="badgeClass()"
      role="status"
    >
      {{ label() }}
    </span>
  `
})
export class PriorityBadgeComponent {
  @Input({ required: true }) priority: string | null = null;

  normalized(): string {
    const p = (this.priority ?? '').toLowerCase();
    return ['urgent', 'high', 'medium', 'low'].includes(p) ? p : 'unknown';
  }

  badgeClass(): string {
    return BADGE_CLASSES[this.normalized()];
  }

  label(): string {
    const p = this.priority;
    return p ? p.charAt(0).toUpperCase() + p.slice(1) : 'Not yet triaged';
  }
}
