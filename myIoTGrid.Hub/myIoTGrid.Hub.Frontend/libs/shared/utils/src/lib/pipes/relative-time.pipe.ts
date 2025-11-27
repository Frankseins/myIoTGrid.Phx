import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'relativeTime',
  standalone: true,
  pure: false
})
export class RelativeTimePipe implements PipeTransform {
  transform(value: string | Date | null | undefined): string {
    if (!value) return '';

    const date = typeof value === 'string' ? new Date(value) : value;
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffSeconds = Math.floor(diffMs / 1000);
    const diffMinutes = Math.floor(diffSeconds / 60);
    const diffHours = Math.floor(diffMinutes / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffSeconds < 5) {
      return 'gerade eben';
    } else if (diffSeconds < 60) {
      return `vor ${diffSeconds} Sekunden`;
    } else if (diffMinutes === 1) {
      return 'vor 1 Minute';
    } else if (diffMinutes < 60) {
      return `vor ${diffMinutes} Minuten`;
    } else if (diffHours === 1) {
      return 'vor 1 Stunde';
    } else if (diffHours < 24) {
      return `vor ${diffHours} Stunden`;
    } else if (diffDays === 1) {
      return 'gestern';
    } else if (diffDays < 7) {
      return `vor ${diffDays} Tagen`;
    } else {
      return date.toLocaleDateString('de-DE', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric'
      });
    }
  }
}
