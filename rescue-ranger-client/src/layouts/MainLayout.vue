<template>
  <q-layout view="lHh Lpr lFf">
    <q-header elevated>
      <q-toolbar>
        <q-btn flat dense round icon="menu" aria-label="Menu" @click="toggleLeftDrawer" />

        <q-toolbar-title> Rescue Ranger </q-toolbar-title>

        <div>Quasar v{{ $q.version }}</div>
      </q-toolbar>
    </q-header>

    <q-drawer v-model="leftDrawerOpen" show-if-above bordered>
      <q-list>
        <q-item-label header> Navigation </q-item-label>

        <EssentialLink v-for="link in linksList" :key="link.title" v-bind="link" />
      </q-list>
    </q-drawer>

    <q-page-container>
      <router-view />
    </q-page-container>
  </q-layout>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import EssentialLink, { type EssentialLinkProps } from 'components/EssentialLink.vue';

const linksList: EssentialLinkProps[] = [
  {
    title: 'Home',
    caption: 'Main dashboard',
    icon: 'home',
    link: '/',
  },
  {
    title: 'Health Check',
    caption: 'API health status',
    icon: 'health_and_safety',
    link: '/health',
  },
  {
    title: 'API Documentation',
    caption: 'Swagger/OpenAPI docs',
    icon: 'description',
    link: 'http://localhost:5000/swagger',
  },
  {
    title: 'Github',
    caption: 'github.com/rheckart/rescue-ranger-net',
    icon: 'code',
    link: 'https://github.com/rheckart/rescue-ranger-net',
  },
  {
    title: 'Quasar Framework',
    caption: 'quasar.dev',
    icon: 'school',
    link: 'https://quasar.dev',
  },
];

const leftDrawerOpen = ref(false);

function toggleLeftDrawer() {
  leftDrawerOpen.value = !leftDrawerOpen.value;
}
</script>
