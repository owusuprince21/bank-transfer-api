<script setup>
import { currency } from '../utils/formatters';
import { cardBase } from './ui';

defineProps({
  accounts: {
    type: Array,
    required: true
  },
  selectedAccountId: {
    type: String,
    default: ''
  }
});

const emit = defineEmits(['select']);
</script>

<template>
  <section :class="[cardBase, 'grid gap-3 p-4']">
    <div>
      <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Portfolio</p>
      <h2 class="text-lg font-black tracking-tight text-slate-950">Accounts</h2>
    </div>

    <div class="grid gap-2.5">
      <button
        v-for="account in accounts"
        :key="account.id"
        type="button"
        class="group grid min-h-24 w-full gap-2 rounded-2xl border p-3 text-left transition duration-200"
        :class="account.id === selectedAccountId
          ? 'border-slate-950 bg-slate-950 text-white shadow-xl'
          : 'border-slate-200 bg-slate-50/80 text-slate-900 hover:border-slate-300 hover:bg-white hover:shadow-md'"
        @click="emit('select', account.id)"
      >
        <div class="flex items-center justify-between gap-3">
          <span
            class="text-[10px] font-black uppercase tracking-wide"
            :class="account.id === selectedAccountId ? 'text-white/60' : 'text-slate-500'"
          >
            {{ account.accountType }}
          </span>
          <span
            class="grid size-8 place-items-center rounded-xl text-xs"
            :class="account.id === selectedAccountId ? 'bg-white/10 text-white' : 'bg-white text-emerald-700 shadow-sm'"
          >
            <i class="pi pi-credit-card"></i>
          </span>
        </div>

        <strong class="text-xl font-black tracking-tight">{{ currency(account.balance) }}</strong>
        <small :class="account.id === selectedAccountId ? 'text-[11px] font-semibold text-white/60' : 'text-[11px] font-semibold text-slate-500'">
          {{ account.accountNumber }}
        </small>
      </button>
    </div>
  </section>
</template>
