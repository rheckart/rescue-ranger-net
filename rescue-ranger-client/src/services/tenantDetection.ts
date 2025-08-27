import { api } from 'boot/axios'

export interface TenantInfo {
  id: number
  name: string
  subdomain: string
  contactEmail: string
  status: string
  configuration: TenantConfiguration
  createdAt: string
  activatedAt?: string
}

export interface TenantConfiguration {
  featureFlags: Record<string, boolean>
  metadata: Record<string, unknown>
}

export interface TenantValidationResponse {
  isValid: boolean
  tenant?: TenantInfo
  error?: string
}

export class TenantDetectionService {
  private static instance: TenantDetectionService
  private currentTenant: TenantInfo | null = null
  private tenantCache = new Map<string, TenantInfo>()

  private constructor() {}

  public static getInstance(): TenantDetectionService {
    if (!TenantDetectionService.instance) {
      TenantDetectionService.instance = new TenantDetectionService()
    }
    return TenantDetectionService.instance
  }

  /**
   * Extract subdomain from current browser URL
   */
  public extractSubdomainFromUrl(url?: string): string | null {
    try {
      const targetUrl = url || window.location.hostname
      
      // Handle localhost development environment
      if (targetUrl.includes('localhost') || targetUrl.includes('127.0.0.1')) {
        // Check if there's a tenant parameter for local development
        const urlParams = new URLSearchParams(window.location.search)
        return urlParams.get('tenant') || null
      }

      // Extract subdomain from hostname (e.g., mysticacres.rescueranger.com)
      const parts = targetUrl.split('.')
      
      // Need at least 3 parts for a subdomain (subdomain.domain.tld)
      if (parts.length >= 3) {
        const subdomain = parts[0]
        
        // Ensure subdomain exists and is not empty
        if (subdomain && subdomain.trim()) {
          // Exclude common subdomains that aren't tenants
          const excludedSubdomains = ['www', 'admin', 'api', 'staging', 'dev']
          if (!excludedSubdomains.includes(subdomain.toLowerCase())) {
            return subdomain
          }
        }
      }

      return null
    } catch (error) {
      console.error('Error extracting subdomain:', error)
      return null
    }
  }

  /**
   * Validate tenant with API and cache result
   */
  public async validateTenant(subdomain: string): Promise<TenantValidationResponse> {
    try {
      // Check cache first
      const cachedTenant = this.tenantCache.get(subdomain)
      if (cachedTenant) {
        return { isValid: true, tenant: cachedTenant }
      }

      // Make API request to validate tenant
      const response = await api.get<TenantInfo>(`/api/tenants/validate/${encodeURIComponent(subdomain)}`)
      
      if (response.data && response.data.status === 'Active') {
        // Cache the tenant for future requests
        this.tenantCache.set(subdomain, response.data)
        this.currentTenant = response.data
        
        // Set tenant context in axios headers for all future requests
        this.setTenantHeaders(response.data.id.toString())
        
        return { isValid: true, tenant: response.data }
      } else {
        return { isValid: false, error: 'Tenant not found or inactive' }
      }
    } catch (error) {
      console.error('Error validating tenant:', error)
      return { 
        isValid: false, 
        error: error instanceof Error ? error.message : 'Failed to validate tenant'
      }
    }
  }

  /**
   * Initialize tenant detection and validation
   */
  public async initializeTenantContext(): Promise<TenantValidationResponse> {
    const subdomain = this.extractSubdomainFromUrl()
    
    if (!subdomain) {
      return { isValid: false, error: 'No valid subdomain found' }
    }

    return await this.validateTenant(subdomain)
  }

  /**
   * Get current tenant information
   */
  public getCurrentTenant(): TenantInfo | null {
    return this.currentTenant
  }

  /**
   * Clear tenant context (for logout or tenant switching)
   */
  public clearTenantContext(): void {
    this.currentTenant = null
    this.removeTenantHeaders()
  }

  /**
   * Set tenant headers for API requests
   */
  private setTenantHeaders(tenantId: string): void {
    api.defaults.headers.common['X-Tenant-Id'] = tenantId
  }

  /**
   * Remove tenant headers from API requests
   */
  private removeTenantHeaders(): void {
    delete api.defaults.headers.common['X-Tenant-Id']
  }

  /**
   * Handle invalid tenant redirection
   */
  public redirectToMainSite(): void {
    try {
      // Determine main site URL based on environment
      const isProduction = import.meta.env.PROD
      const mainSiteUrl = isProduction 
        ? 'https://rescueranger.com'
        : 'http://localhost:3000'

      // Add current path as return URL parameter
      const currentPath = window.location.pathname + window.location.search
      const returnUrl = encodeURIComponent(currentPath)
      
      window.location.href = `${mainSiteUrl}?invalid_tenant=true&return_url=${returnUrl}`
    } catch (error) {
      console.error('Error redirecting to main site:', error)
      // Fallback to simple redirect
      window.location.href = '/'
    }
  }

  /**
   * Clear tenant cache (useful for development or after tenant updates)
   */
  public clearCache(): void {
    this.tenantCache.clear()
  }

  /**
   * Get cached tenant by subdomain
   */
  public getCachedTenant(subdomain: string): TenantInfo | undefined {
    return this.tenantCache.get(subdomain)
  }
}

// Export singleton instance
export const tenantDetectionService = TenantDetectionService.getInstance()