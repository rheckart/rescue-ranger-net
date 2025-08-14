<template>
  <q-page class="q-pa-md">
    <div class="row justify-center">
      <div class="col-12 col-md-8 col-lg-6">
        <q-card>
          <q-card-section>
            <div class="text-h4 q-mb-md">
              <q-icon name="health_and_safety" class="q-mr-sm" />
              API Health Check
            </div>
            <div class="text-subtitle2 text-grey-7">
              Check the status of the Rescue Ranger API
            </div>
          </q-card-section>

          <q-card-section>
            <div class="q-gutter-md">
              <q-btn
                @click="checkBasicHealth"
                label="Basic Health Check"
                color="primary"
                icon="play_arrow"
                :loading="basicLoading"
                :disable="basicLoading"
              />
              
              <q-btn
                @click="checkDetailedHealth"
                label="Detailed Health Check"
                color="secondary"
                icon="search"
                :loading="detailedLoading"
                :disable="detailedLoading"
              />
            </div>
          </q-card-section>

          <!-- Basic Health Results -->
          <q-card-section v-if="basicHealth">
            <div class="text-h6 q-mb-sm">Basic Health Status</div>
            <q-chip
              :color="basicHealth.status === 'Healthy' ? 'positive' : 'negative'"
              text-color="white"
              icon="circle"
            >
              {{ basicHealth.status }}
            </q-chip>
          </q-card-section>

          <!-- Detailed Health Results -->
          <q-card-section v-if="detailedHealth">
            <div class="text-h6 q-mb-sm">Detailed Health Status</div>
            
            <div class="q-mb-md">
              <q-chip
                :color="detailedHealth.status === 'Healthy' ? 'positive' : 'negative'"
                text-color="white"
                icon="circle"
              >
                {{ detailedHealth.status }}
              </q-chip>
            </div>

            <div v-if="detailedHealth.timestamp" class="q-mb-sm">
              <strong>Timestamp:</strong> {{ formatDate(detailedHealth.timestamp) }}
            </div>

            <div v-if="detailedHealth.version" class="q-mb-sm">
              <strong>Version:</strong> {{ detailedHealth.version }}
            </div>

            <div v-if="detailedHealth.environment" class="q-mb-sm">
              <strong>Environment:</strong> {{ detailedHealth.environment }}
            </div>

            <!-- Services Status -->
            <div v-if="detailedHealth.services" class="q-mt-md">
              <div class="text-subtitle1 q-mb-sm">Services</div>
              <q-list bordered separator>
                <q-item
                  v-for="(service, name) in detailedHealth.services"
                  :key="name"
                >
                  <q-item-section avatar>
                    <q-icon
                      :name="service.status === 'Healthy' ? 'check_circle' : 'error'"
                      :color="service.status === 'Healthy' ? 'positive' : 'negative'"
                    />
                  </q-item-section>
                  <q-item-section>
                    <q-item-label>{{ name }}</q-item-label>
                    <q-item-label caption>
                      Status: {{ service.status }}
                      <span v-if="service.responseTime"> | Response Time: {{ service.responseTime }}</span>
                      <span v-if="service.error"> | Error: {{ service.error }}</span>
                    </q-item-label>
                  </q-item-section>
                </q-item>
              </q-list>
            </div>

            <!-- System Info -->
            <div v-if="detailedHealth.system" class="q-mt-md">
              <div class="text-subtitle1 q-mb-sm">System Information</div>
              <q-list bordered>
                <q-item v-if="detailedHealth.system.uptime">
                  <q-item-section>
                    <q-item-label>Uptime</q-item-label>
                    <q-item-label caption>{{ detailedHealth.system.uptime }}</q-item-label>
                  </q-item-section>
                </q-item>
                <q-item v-if="detailedHealth.system.memoryUsage">
                  <q-item-section>
                    <q-item-label>Memory Usage</q-item-label>
                    <q-item-label caption>{{ detailedHealth.system.memoryUsage }}</q-item-label>
                  </q-item-section>
                </q-item>
              </q-list>
            </div>
          </q-card-section>

          <!-- Error Display -->
          <q-card-section v-if="error">
            <q-banner class="text-white bg-red">
              <template v-slot:avatar>
                <q-icon name="error" color="white" />
              </template>
              <div class="text-subtitle2">Error checking API health</div>
              <div>{{ error }}</div>
            </q-banner>
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { healthService, type HealthCheckResponse } from 'src/services/health.service'
import { useQuasar } from 'quasar'

const $q = useQuasar()

const basicHealth = ref<HealthCheckResponse | null>(null)
const detailedHealth = ref<HealthCheckResponse | null>(null)
const basicLoading = ref(false)
const detailedLoading = ref(false)
const error = ref<string | null>(null)

const checkBasicHealth = async () => {
  basicLoading.value = true
  error.value = null
  
  try {
    basicHealth.value = await healthService.checkHealth()
    $q.notify({
      color: 'positive',
      message: 'Basic health check completed successfully',
      icon: 'check'
    })
  } catch (err: unknown) {
    error.value = (err as Error).message || 'Failed to check basic health'
    $q.notify({
      color: 'negative',
      message: 'Failed to check API health',
      icon: 'error'
    })
    console.error('Basic health check failed:', err)
  } finally {
    basicLoading.value = false
  }
}

const checkDetailedHealth = async () => {
  detailedLoading.value = true
  error.value = null
  
  try {
    detailedHealth.value = await healthService.checkDetailedHealth()
    $q.notify({
      color: 'positive',
      message: 'Detailed health check completed successfully',
      icon: 'check'
    })
  } catch (err: unknown) {
    error.value = (err as Error).message || 'Failed to check detailed health'
    $q.notify({
      color: 'negative',
      message: 'Failed to check detailed API health',
      icon: 'error'
    })
    console.error('Detailed health check failed:', err)
  } finally {
    detailedLoading.value = false
  }
}

const formatDate = (dateString: string) => {
  return new Date(dateString).toLocaleString()
}
</script>