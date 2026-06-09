<script setup>
defineProps({
  open: {
    type: Boolean,
    required: true
  },
  title: {
    type: String,
    default: 'Processing'
  },
  description: {
    type: String,
    default: 'Please wait while the request completes.'
  }
});
</script>

<template>
  <Teleport to="body">
    <div
      v-if="open"
      class="fixed inset-0 z-[60] grid place-items-center bg-slate-950/50 p-4 backdrop-blur-sm"
      role="alertdialog"
      aria-modal="true"
    >
      <div class="grid w-full max-w-sm justify-items-center gap-4 rounded-2xl border border-slate-200 bg-white p-6 text-center shadow-2xl">
        <div class="grid size-14 place-items-center rounded-full bg-emerald-50 text-emerald-700">
          <i class="pi pi-spin pi-spinner text-2xl"></i>
        </div>
        <div class="grid gap-2">
          <h2 class="text-lg font-black tracking-tight text-slate-950">{{ title }}</h2>
          <p class="text-xs font-semibold leading-5 text-slate-500">{{ description }}</p>
        </div>
      </div>
    </div>
  </Teleport>
</template>
