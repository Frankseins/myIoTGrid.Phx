import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'relativeTime',
  standalone: true,
  pure: false
})
export class RelativeTimePipe implements PipeTransform {
  transform(value: string | Date | null | undefined): string {
    if (!value) return '';

    // Parse date - handle both ISO strings (with Z suffix) and local dates
    let date: Date;
    if (typeof value === 'string') {
      // If the string doesn't end with Z or timezone offset, treat as UTC
      // Backend sends ISO 8601 format which should have Z suffix
      date = new Date(value);
    } else {
      date = value;
    }

    // Validate the date
    if (isNaN(date.getTime())) {
      return '';
    }

    const now = new Date();
    const diffMs = now.getTime() - date.getTime();

    // Handle future dates (shouldn't happen, but be defensive)
    if (diffMs < 0) {
      return 'gerade eben';
    }

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
