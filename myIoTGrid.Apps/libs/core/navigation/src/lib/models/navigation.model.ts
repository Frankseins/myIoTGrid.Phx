export interface NavigationItem {
  icon: string;
  label: string;
  route: string;
  badge?: number;
  disabled?: boolean;
}

export interface NavigationSection {
  title?: string;
  items: NavigationItem[];
}
