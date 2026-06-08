<script setup>
import { computed, onBeforeUnmount, onMounted } from 'vue';
import { primaryButton, secondaryButton } from './ui';

const props = defineProps({
  open: {
    type: Boolean,
    required: true
  },
  title: {
    type: String,
    default: 'Confirm action'
  },
  description: {
    type: String,
    default: ''
  },
  confirmLabel: {
    type: String,
    default: 'Confirm'
  },
  isLoading: {
    type: Boolean,
    default: false
  },
  tone: {
    type: String,
    default: 'default'
  },
  details: {
    type: Array,
    default: () => []
  }
});

const emit = defineEmits(['cancel', 'confirm']);

const confirmButtonClass = computed(() => {
  if (props.tone === 'danger') {
    return 'inline-flex min-h-9 items-center justify-center gap-2 rounded-xl bg-red-600 px-3 py-2 text-xs font-bold text-white shadow-sm transition hover:bg-red-700 disabled:pointer-events-none disabled:opacity-50';
  }

  return primaryButton;
});

function handleKeydown(event) {
  if (event.key === 'Escape' && props.open && !props.isLoading) {
    emit('cancel');
  }
}

onMounted(() => {
  window.addEventListener('keydown', handleKeydown);
});

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleKeydown);
});
</script>

<template>
  <Teleport to="body">
    <div
      v-if="open"
      class="fixed inset-0 z-50 grid place-items-center bg-slate-950/45 p-4 backdrop-blur-sm"
      role="dialog"
      aria-modal="true"
    >
      <div class="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-5 shadow-2xl">
        <div class="grid gap-2">
          <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Please confirm</p>
          <h2 class="text-lg font-black tracking-tight text-slate-950">{{ title }}</h2>
          <p v-if="description" class="text-xs font-medium leading-5 text-slate-500">
            {{ description }}
          </p>
        </div>

        <dl v-if="details.length" class="mt-4 grid gap-2 rounded-xl bg-slate-50 p-3">
          <div
            v-for="detail in details"
            :key="detail.label"
            class="grid gap-1 sm:grid-cols-[120px_minmax(0,1fr)]"
          >
            <dt class="text-[11px] font-black uppercase tracking-wide text-slate-400">{{ detail.label }}</dt>
            <dd class="break-words text-xs font-bold text-slate-800">{{ detail.value }}</dd>
          </div>
        </dl>

        <div class="mt-5 flex justify-end gap-2">
          <button type="button" :class="secondaryButton" :disabled="isLoading" @click="$emit('cancel')">
            Cancel
          </button>
          <button type="button" :class="confirmButtonClass" :disabled="isLoading" @click="$emit('confirm')">
            <i class="pi pi-check"></i>
            {{ isLoading ? 'Processing...' : confirmLabel }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>
