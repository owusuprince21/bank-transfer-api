<script setup>
import { computed } from 'vue';
import { formatDate } from '../utils/formatters';
import { cardBase, secondaryButton } from './ui';

const props = defineProps({
  notifications: {
    type: Array,
    default: () => []
  },
  isLoading: {
    type: Boolean,
    default: false
  }
});

const emit = defineEmits(['mark-read', 'mark-all-read', 'refresh', 'delete']);

const unreadCount = computed(() => props.notifications.filter((notification) => !notification.isRead).length);

function iconFor(type) {
  if (type === 'TransferReceived' || type === 'Deposit') {
    return 'pi pi-arrow-down-left';
  }

  if (type === 'TransferSent' || type === 'Withdrawal') {
    return 'pi pi-arrow-up-right';
  }

  if (type?.startsWith('Kyc')) {
    return 'pi pi-id-card';
  }

  if (type?.startsWith('FinancialControls')) {
    return 'pi pi-shield';
  }

  return 'pi pi-bell';
}
</script>

<template>
  <section :class="[cardBase, 'grid max-h-[min(760px,calc(100vh-8rem))] grid-rows-[auto_minmax(0,1fr)] gap-4 overflow-hidden p-4']">
    <div class="grid items-center gap-4 md:grid-cols-[1fr_auto]">
      <div>
        <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Notifications</p>
        <h2 class="text-lg font-black tracking-tight text-slate-950">Activity messages</h2>
        <p class="mt-1 text-xs font-semibold text-slate-500">{{ unreadCount }} unread message{{ unreadCount === 1 ? '' : 's' }}</p>
      </div>
      <div class="flex flex-wrap justify-end gap-2">
        <button type="button" :class="secondaryButton" :disabled="isLoading" @click="$emit('refresh')">
          <i class="pi pi-refresh"></i>
          Refresh
        </button>
        <button type="button" :class="secondaryButton" :disabled="isLoading || unreadCount === 0" @click="$emit('mark-all-read')">
          <i class="pi pi-check-circle"></i>
          Mark all read
        </button>
      </div>
    </div>

    <div v-if="notifications.length === 0" class="grid min-h-36 place-items-center rounded-2xl border border-dashed border-slate-200 bg-slate-50 text-xs font-semibold text-slate-500">
      No notifications yet.
    </div>

    <div v-else class="min-h-0 overflow-y-auto pr-1">
      <div class="grid gap-2">
      <article
        v-for="notification in notifications"
        :key="notification.id"
        class="grid gap-3 rounded-2xl border p-4"
        :class="notification.isRead ? 'border-slate-200 bg-white' : 'border-emerald-200 bg-emerald-50/60'"
      >
        <div class="flex items-start gap-3">
          <span class="grid size-10 shrink-0 place-items-center rounded-2xl bg-white text-emerald-700 shadow-sm">
            <i :class="iconFor(notification.type)"></i>
          </span>
          <div class="min-w-0 flex-1">
            <div class="flex flex-wrap items-start justify-between gap-2">
              <h3 class="text-sm font-black text-slate-950">{{ notification.title }}</h3>
              <span class="text-[11px] font-bold text-slate-400">{{ formatDate(notification.createdAtUtc) }}</span>
            </div>
            <p class="mt-1 text-xs font-semibold leading-5 text-slate-600">{{ notification.message }}</p>
          </div>
        </div>

        <div class="flex flex-wrap justify-end gap-2">
          <button v-if="!notification.isRead" type="button" :class="secondaryButton" :disabled="isLoading" @click="$emit('mark-read', notification.id)">
            <i class="pi pi-check"></i>
            Mark read
          </button>
          <button
            type="button"
            class="inline-flex min-h-9 items-center justify-center gap-2 rounded-xl border border-red-100 bg-white px-3 py-2 text-xs font-bold text-red-600 transition hover:bg-red-50 disabled:pointer-events-none disabled:opacity-50"
            :disabled="isLoading"
            @click="$emit('delete', notification.id)"
          >
            <i class="pi pi-trash"></i>
            Delete
          </button>
        </div>
      </article>
      </div>
    </div>
  </section>
</template>
