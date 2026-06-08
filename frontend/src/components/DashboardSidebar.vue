<script setup>
import { computed } from 'vue';
import { ghostButton } from './ui';

const props = defineProps({
  customer: {
    type: Object,
    required: true
  },
  activeView: {
    type: String,
    required: true
  }
});

defineEmits(['navigate', 'sign-out']);

const initials = computed(() => {
  return `${props.customer.firstName?.[0] ?? ''}${props.customer.lastName?.[0] ?? ''}`.toUpperCase();
});

const navItems = [
  { id: 'overview', label: 'Overview', icon: 'pi pi-chart-line' },
  { id: 'accounts', label: 'Accounts', icon: 'pi pi-wallet' },
  { id: 'transfer', label: 'Send Money', icon: 'pi pi-send' },
  { id: 'activity', label: 'Activity', icon: 'pi pi-list' }
];
</script>

<template>
  <aside class="lg:fixed lg:inset-y-4 lg:left-4 lg:z-30 lg:w-72">
    <div class="flex max-h-full flex-col overflow-hidden rounded-[1.5rem] border border-slate-200/80 bg-white/95 p-4 shadow-sm backdrop-blur">
      <div class="min-h-0 flex-1 overflow-y-auto pr-1">
        <section class="grid justify-items-center gap-2 border-b border-slate-100 pb-4 text-center">
          <div class="grid size-18 place-items-center rounded-full bg-gradient-to-br from-slate-950 to-emerald-800 text-xl font-black text-white shadow-lg ring-4 ring-emerald-50">
            {{ initials }}
          </div>
          <div class="grid min-w-0 gap-1">
            <p class="truncate text-sm font-black tracking-tight text-slate-950">
              {{ customer.firstName }} {{ customer.lastName }}
            </p>
            <p class="truncate text-xs font-semibold text-slate-500">{{ customer.email }}</p>
            <p class="line-clamp-2 text-[11px] font-medium leading-4 text-slate-400">
              {{ customer.address || 'No address provided' }}
            </p>
          </div>
        </section>

      <nav class="mt-4 grid gap-1.5">
        <button
          v-for="item in navItems"
          :key="item.id"
          type="button"
          :class="[
            ghostButton,
            'h-10 justify-start rounded-xl px-3 text-xs',
            activeView === item.id ? 'bg-slate-950 text-white shadow-lg shadow-slate-200 hover:bg-slate-900 hover:text-white' : ''
          ]"
          @click="$emit('navigate', item.id)"
        >
          <span
            class="grid size-7 place-items-center rounded-lg text-xs"
            :class="activeView === item.id ? 'bg-white/10' : 'bg-slate-100 text-slate-500'"
          >
            <i :class="item.icon"></i>
          </span>
          <span>{{ item.label }}</span>
        </button>
        </nav>
      </div>

      <div class="border-t border-slate-100 pt-3">
        <button
          type="button"
          :class="[ghostButton, 'h-10 w-full justify-start rounded-xl px-3 text-xs text-red-600 hover:bg-red-50 hover:text-red-700']"
          @click="$emit('sign-out')"
        >
          <span class="grid size-7 place-items-center rounded-lg bg-red-50 text-xs text-red-600">
            <i class="pi pi-sign-out"></i>
          </span>
          Sign out
        </button>
      </div>
    </div>
  </aside>
</template>
