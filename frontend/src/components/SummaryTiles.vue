<script setup>
import { computed } from 'vue';
import { currency } from '../utils/formatters';

const props = defineProps({
  customer: {
    type: Object,
    required: true
  },
  totalBalance: {
    type: Number,
    required: true
  },
  transactions: {
    type: Array,
    required: true
  }
});

const totalInflow = computed(() => {
  return props.transactions
    .filter((transaction) => transaction.transactionType === 'Deposit' || transaction.transactionType === 'TransferReceived')
    .reduce((sum, transaction) => sum + Number(transaction.amount), 0);
});

const totalOutflow = computed(() => {
  return props.transactions
    .filter((transaction) => transaction.transactionType === 'Withdrawal' || transaction.transactionType === 'TransferSent')
    .reduce((sum, transaction) => sum + Number(transaction.amount), 0);
});

const cards = computed(() => [
  {
    label: 'Total Balance',
    value: currency(props.totalBalance),
    icon: 'pi pi-wallet',
    accent: 'from-slate-950 to-emerald-900'
  },
  {
    label: 'Money In',
    value: currency(totalInflow.value),
    icon: 'pi pi-arrow-down-left',
    accent: 'from-emerald-500 to-teal-600'
  },
  {
    label: 'Money Out',
    value: currency(totalOutflow.value),
    icon: 'pi pi-arrow-up-right',
    accent: 'from-amber-500 to-orange-600'
  },
  {
    label: 'Accounts',
    value: props.customer.accounts.length,
    icon: 'pi pi-building-columns',
    accent: 'from-indigo-500 to-violet-600'
  }
]);
</script>

<template>
  <section class="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
    <article
      v-for="card in cards"
      :key="card.label"
      class="group rounded-2xl border border-slate-200/80 bg-white/90 p-4 shadow-sm backdrop-blur transition duration-200 hover:-translate-y-0.5 hover:shadow-lg"
    >
      <div class="flex items-start justify-between gap-4">
        <div class="grid gap-2">
          <span class="text-xs font-bold text-slate-500">{{ card.label }}</span>
          <strong class="break-words text-lg font-black tracking-tight text-slate-950">
            {{ card.value }}
          </strong>
        </div>
        <span
          class="grid size-10 place-items-center rounded-xl bg-gradient-to-br text-sm text-white shadow-md transition group-hover:scale-105"
          :class="card.accent"
        >
          <i :class="card.icon"></i>
        </span>
      </div>
    </article>
  </section>
</template>
