<template>
  <q-page class="row items-center justify-evenly">
    <div class="text-center">
      <div>
        <div class="text-h2 text-weight-thin">
          Welcome to
        </div>
        <div class="text-h1 text-weight-bold text-primary q-mt-sm q-mb-xs">
          Rescue Ranger
        </div>
        <div class="text-subtitle1 text-grey-7">
          Horse Rescue Management System
        </div>
      </div>

      <q-img
        class="rounded-borders q-ma-lg"
        src="~/assets/quasar-logo-vertical.svg"
        style="width: 280px; height: 256px"
      >
        <template v-slot:error>
          <div class="absolute-full flex flex-center bg-negative text-white">
            Cannot load image
          </div>
        </template>
      </q-img>

      <div class="q-pa-md">
        <q-btn
          label="Check API Health"
          color="primary"
          icon="health_and_safety"
          to="/health"
          class="q-ma-sm"
        />
        
        <q-btn
          label="View API Documentation"
          color="secondary"
          icon="description"
          href="http://localhost:5000/swagger"
          target="_blank"
          class="q-ma-sm"
        />
      </div>

      <div class="q-pa-md">
        <div class="text-caption text-grey-6">
          Built with Vue 3 + Quasar + TypeScript + .NET 9
        </div>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
// This is the main landing page for Rescue Ranger
</script>