import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { tenantDetectionService, type TenantInfo, type TenantValidationResponse } from 'src/services/tenantDetection'

export interface TenantState {
  currentTenant: TenantInfo | null
  isLoading: boolean
  error: string | null
  isInitialized: boolean
}

export const useTenantStore = defineStore('tenant', () => {
  // State
  const currentTenant = ref<TenantInfo | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const isInitialized = ref(false)

  // Getters
  const isValidTenant = computed(() => !!currentTenant.value && currentTenant.value.status === 'Active')
  const tenantSubdomain = computed(() => currentTenant.value?.subdomain || null)
  const tenantName = computed(() => currentTenant.value?.name || null)
  const tenantConfiguration = computed(() => currentTenant.value?.configuration || null)
  const hasError = computed(() => !!error.value)

  // Feature flags helper
  const isFeatureEnabled = computed(() => (featureName: string): boolean => {
    return currentTenant.value?.configuration?.featureFlags?.[featureName] === true
  })

  // Tenant metadata helper
  const getTenantMetadata = computed(() => (key: string): unknown => {
    return currentTenant.value?.configuration?.metadata?.[key]
  })

  // Actions
  async function initializeTenant(): Promise<boolean> {
    if (isInitialized.value) {
      return isValidTenant.value
    }

    isLoading.value = true
    error.value = null

    try {
      const result: TenantValidationResponse = await tenantDetectionService.initializeTenantContext()
      
      if (result.isValid && result.tenant) {
        currentTenant.value = result.tenant
        isInitialized.value = true
        return true
      } else {
        error.value = result.error || 'Failed to initialize tenant context'
        isInitialized.value = true
        return false
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Unknown error occurred'
      isInitialized.value = true
      return false
    } finally {
      isLoading.value = false
    }
  }

  async function validateTenant(subdomain: string): Promise<boolean> {
    isLoading.value = true
    error.value = null

    try {
      const result = await tenantDetectionService.validateTenant(subdomain)
      
      if (result.isValid && result.tenant) {
        currentTenant.value = result.tenant
        return true
      } else {
        error.value = result.error || 'Tenant validation failed'
        return false
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Validation error occurred'
      return false
    } finally {
      isLoading.value = false
    }
  }

  function setTenant(tenant: TenantInfo): void {
    currentTenant.value = tenant
    error.value = null
    if (!isInitialized.value) {
      isInitialized.value = true
    }
  }

  function clearTenant(): void {
    currentTenant.value = null
    error.value = null
    isInitialized.value = false
    tenantDetectionService.clearTenantContext()
  }

  function setError(errorMessage: string): void {
    error.value = errorMessage
  }

  function clearError(): void {
    error.value = null
  }

  function redirectToMainSite(): void {
    tenantDetectionService.redirectToMainSite()
  }

  // Admin functions (for system administrators)
  async function switchTenant(subdomain: string): Promise<boolean> {
    const success = await validateTenant(subdomain)
    if (success) {
      // Clear any cached data that might be tenant-specific
      // This would be expanded based on other stores in the app
      console.log(`Switched to tenant: ${subdomain}`)
    }
    return success
  }

  function getTenantDisplayName(): string {
    if (!currentTenant.value) return 'Unknown Tenant'
    return currentTenant.value.name || currentTenant.value.subdomain || 'Unknown Tenant'
  }

  // Persistence helpers (for maintaining tenant context across page reloads)
  function saveTenantToStorage(): void {
    if (currentTenant.value) {
      try {
        localStorage.setItem('rescueRanger_tenant', JSON.stringify(currentTenant.value))
      } catch (err) {
        console.warn('Failed to save tenant to localStorage:', err)
      }
    }
  }

  function loadTenantFromStorage(): TenantInfo | null {
    try {
      const stored = localStorage.getItem('rescueRanger_tenant')
      if (stored) {
        const tenant = JSON.parse(stored) as TenantInfo
        // Verify the stored tenant is still valid (basic check)
        if (tenant.id && tenant.subdomain && tenant.name) {
          return tenant
        }
      }
    } catch (err) {
      console.warn('Failed to load tenant from localStorage:', err)
    }
    return null
  }

  function clearTenantFromStorage(): void {
    try {
      localStorage.removeItem('rescueRanger_tenant')
    } catch (err) {
      console.warn('Failed to clear tenant from localStorage:', err)
    }
  }

  // Initialize from storage on store creation
  const storedTenant = loadTenantFromStorage()
  if (storedTenant) {
    currentTenant.value = storedTenant
    // Note: We should still validate this with the server on app initialization
  }

  return {
    // State
    currentTenant,
    isLoading,
    error,
    isInitialized,
    
    // Getters
    isValidTenant,
    tenantSubdomain,
    tenantName,
    tenantConfiguration,
    hasError,
    isFeatureEnabled,
    getTenantMetadata,
    
    // Actions
    initializeTenant,
    validateTenant,
    setTenant,
    clearTenant,
    setError,
    clearError,
    redirectToMainSite,
    switchTenant,
    getTenantDisplayName,
    
    // Persistence
    saveTenantToStorage,
    loadTenantFromStorage,
    clearTenantFromStorage
  }
})

// Export types for use in components
export type { TenantInfo }