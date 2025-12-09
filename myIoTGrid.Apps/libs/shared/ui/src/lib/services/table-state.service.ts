import { Injectable } from '@angular/core';

export interface GenericTableExtraState {
  pageSize?: number;
  pageIndex?: number;
  sort?: { active: string; direction: 'asc' | 'desc' | '' };
  scrollTop?: number;
  selectionIds?: (string | number)[];
  globalFilter?: string;
  editMode?: boolean;
  readonly?: boolean;
  filters?: Record<string, unknown>;
}

@Injectable({
  providedIn: 'root'
})
export class TableStateService {
  private readonly storagePrefix = 'table-state:';

  saveExtraState(key: string, patch: Partial<GenericTableExtraState>): void {
    const existing = this.loadExtraState(key) || {};
    const updated = { ...existing, ...patch };
    try {
      localStorage.setItem(this.storagePrefix + key, JSON.stringify(updated));
    } catch (e) {
      console.warn('Failed to save table state:', e);
    }
  }

  loadExtraState(key: string): GenericTableExtraState | null {
    try {
      const raw = localStorage.getItem(this.storagePrefix + key);
      return raw ? JSON.parse(raw) : null;
    } catch (e) {
      console.warn('Failed to load table state:', e);
      return null;
    }
  }

  clearExtraState(key: string): void {
    try {
      localStorage.removeItem(this.storagePrefix + key);
    } catch (e) {
      console.warn('Failed to clear table state:', e);
    }
  }
}
