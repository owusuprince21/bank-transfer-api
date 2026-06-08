<script setup>
import { ref } from 'vue';
import AccountList from './AccountList.vue';
import AccountTrendChart from './AccountTrendChart.vue';
import CreateAccountForm from './CreateAccountForm.vue';
import DashboardSidebar from './DashboardSidebar.vue';
import SummaryTiles from './SummaryTiles.vue';
import TransactionForm from './TransactionForm.vue';
import TransactionHistory from './TransactionHistory.vue';
import TransferForm from './TransferForm.vue';
import { cardBase } from './ui';

defineProps({
  customer: {
    type: Object,
    required: true
  },
  selectedAccount: {
    type: Object,
    default: null
  },
  selectedAccountId: {
    type: String,
    default: ''
  },
  totalBalance: {
    type: Number,
    required: true
  },
  transactions: {
    type: Array,
    required: true
  },
  isLoading: {
    type: Boolean,
    default: false
  }
});

defineEmits([
  'sign-out',
  'select-account',
  'submit-transaction',
  'create-account',
  'send-transfer',
  'refresh-transactions'
]);

const activeView = ref('overview');
</script>

<template>
  <section class="mx-auto grid w-full max-w-[1520px] items-start gap-5 lg:block lg:pl-80">
    <DashboardSidebar
      :customer="customer"
      :active-view="activeView"
      @navigate="activeView = $event"
      @sign-out="$emit('sign-out')"
    />

    <main class="grid min-w-0 gap-5">
      <header :class="[cardBase, 'grid gap-4 p-5 md:grid-cols-[1fr_auto] md:items-center']">
        <div class="grid gap-2">
          <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">ApiDemo Bank</p>
          <h1 class="text-2xl font-black tracking-tight text-slate-950 md:text-4xl">
            Banking workspace
          </h1>
          <p class="max-w-2xl text-xs font-medium leading-5 text-slate-500">
            A focused dashboard for balances, account creation, transfers, and activity monitoring.
          </p>
        </div>
        <div class="rounded-2xl bg-slate-950 px-4 py-3 text-white shadow-lg">
          <p class="text-[10px] font-bold uppercase text-white/60">Signed in as</p>
          <p class="mt-1 text-sm font-black">{{ customer.firstName }} {{ customer.lastName }}</p>
        </div>
      </header>

      <SummaryTiles
        :customer="customer"
        :total-balance="totalBalance"
        :transactions="transactions"
      />

      <section v-if="activeView === 'overview'" class="grid min-w-0 gap-5 xl:grid-cols-[330px_minmax(0,1fr)]">
        <AccountList
          :accounts="customer.accounts"
          :selected-account-id="selectedAccountId"
          @select="$emit('select-account', $event)"
        />
        <div class="grid min-w-0 gap-5">
          <AccountTrendChart :transactions="transactions" :selected-account="selectedAccount" />
          <TransactionForm
            :selected-account="selectedAccount"
            :is-loading="isLoading"
            @submit="$emit('submit-transaction', $event)"
          />
        </div>
      </section>

      <section v-else-if="activeView === 'accounts'" class="grid min-w-0 gap-5 xl:grid-cols-[minmax(0,1fr)_340px]">
        <AccountList
          :accounts="customer.accounts"
          :selected-account-id="selectedAccountId"
          @select="$emit('select-account', $event)"
        />
        <CreateAccountForm
          :is-loading="isLoading"
          @submit="$emit('create-account', $event)"
        />
      </section>

      <section v-else-if="activeView === 'transfer'" class="grid min-w-0 gap-5 xl:grid-cols-[minmax(0,1fr)_340px]">
        <TransferForm
          :selected-account="selectedAccount"
          :is-loading="isLoading"
          @submit="$emit('send-transfer', $event)"
        />
        <AccountList
          :accounts="customer.accounts"
          :selected-account-id="selectedAccountId"
          @select="$emit('select-account', $event)"
        />
      </section>

      <TransactionHistory
        v-else
        :transactions="transactions"
        :is-loading="isLoading"
        @refresh="$emit('refresh-transactions')"
      />
    </main>
  </section>
</template>
