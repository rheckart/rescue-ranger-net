import { api } from 'boot/axios'

export interface HealthCheckResponse {
  status: string
  timestamp?: string
  version?: string
  environment?: string
  services?: Record<string, ServiceStatus>
  system?: SystemInfo
}

export interface ServiceStatus {
  status: string
  responseTime?: string
  error?: string
}

export interface SystemInfo {
  uptime: string
  memoryUsage: string
}

export interface ApiInfoResponse {
  name: string
  version: string
  framework: string
  environment: string
  timestamp: string
}

export const healthService = {
  async checkHealth(): Promise<HealthCheckResponse> {
    const response = await api.get<HealthCheckResponse>('/health')
    return response.data
  },
  
  async checkDetailedHealth(): Promise<HealthCheckResponse> {
    const response = await api.get<HealthCheckResponse>('/health/ready')
    return response.data
  },
  
  async getApiInfo(): Promise<ApiInfoResponse> {
    const response = await api.get<ApiInfoResponse>('/api/info')
    return response.data
  }
}