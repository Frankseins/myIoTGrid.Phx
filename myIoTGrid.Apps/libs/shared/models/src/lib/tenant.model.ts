/**
 * Tenant models - corresponds to Backend TenantDto
 */
export interface Tenant {
  id: string;
  name: string;
  cloudApiKey?: string;
  createdAt: string;
  lastSyncAt?: string;
  isActive: boolean;
}

export interface CreateTenantDto {
  name: string;
  cloudApiKey?: string;
}
