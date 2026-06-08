<script setup>
import { computed } from 'vue';
import { currency, formatDate } from '../utils/formatters';
import { cardBase, secondaryButton } from './ui';

const props = defineProps({
  transactions: {
    type: Array,
    required: true
  },
  isLoading: {
    type: Boolean,
    default: false
  }
});

defineEmits(['refresh']);

const rows = computed(() => {
  return props.transactions.map((transaction) => ({
    ...transaction,
    isCredit: transaction.transactionType === 'Deposit' || transaction.transactionType === 'TransferReceived'
  }));
});
</script>

<template>
  <section :class="[cardBase, 'grid gap-4 p-4']">
    <div class="grid items-center gap-4 md:grid-cols-[1fr_auto]">
      <div>
        <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Activity</p>
        <h2 class="text-lg font-black tracking-tight text-slate-950">Transactions</h2>
      </div>
      <button type="button" :class="secondaryButton" :disabled="isLoading" @click="$emit('refresh')">
        <i class="pi pi-refresh"></i>
        Refresh
      </button>
    </div>

    <div v-if="rows.length === 0" class="grid min-h-36 place-items-center rounded-2xl border border-dashed border-slate-200 bg-slate-50 text-xs font-semibold text-slate-500">
      No transactions yet.
    </div>

    <div v-else class="overflow-x-auto rounded-2xl border border-slate-200">
      <table class="w-full min-w-[760px] border-collapse text-left text-xs">
        <thead class="bg-slate-50 text-xs font-black uppercase tracking-wide text-slate-500">
          <tr>
            <th class="px-3 py-2.5">Type</th>
            <th class="px-3 py-2.5">Description</th>
            <th class="px-3 py-2.5">Amount</th>
            <th class="px-3 py-2.5">Balance</th>
            <th class="px-3 py-2.5">Date</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-200 bg-white">
          <tr v-for="transaction in rows" :key="transaction.id" class="hover:bg-slate-50/80">
            <td class="px-3 py-3">
              <span
                class="inline-flex rounded-full px-2 py-1 text-[11px] font-black"
                :class="transaction.isCredit ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'"
              >
                {{ transaction.transactionType }}
              </span>
            </td>
            <td class="max-w-[260px] truncate px-3 py-3 font-semibold text-slate-700">
              {{ transaction.description || transaction.referenceNumber }}
            </td>
            <td
              class="px-3 py-3 font-black"
              :class="transaction.isCredit ? 'text-emerald-700' : 'text-slate-950'"
            >
              {{ transaction.isCredit ? '+' : '-' }}{{ currency(transaction.amount) }}
            </td>
            <td class="px-3 py-3 font-semibold text-slate-700">
              {{ currency(transaction.balanceAfterTransaction) }}
            </td>
            <td class="px-3 py-3 font-semibold text-slate-500">
              {{ formatDate(transaction.createdAtUtc) }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
