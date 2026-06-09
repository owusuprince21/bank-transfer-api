<script setup>
import { ref, watch } from 'vue';
import AccountList from './AccountList.vue';
import AccountTrendChart from './AccountTrendChart.vue';
import CreateAccountForm from './CreateAccountForm.vue';
import DashboardSidebar from './DashboardSidebar.vue';
import FinancialControlsPanel from './FinancialControlsPanel.vue';
import KycDocumentsPanel from './KycDocumentsPanel.vue';
import NotificationsPanel from './NotificationsPanel.vue';
import SummaryTiles from './SummaryTiles.vue';
import TransactionForm from './TransactionForm.vue';
import TransactionHistory from './TransactionHistory.vue';
import TransferForm from './TransferForm.vue';
import { cardBase } from './ui';

const props = defineProps({
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
  },
  financialControl: {
    type: Object,
    default: null
  },
  kycDocuments: {
    type: Array,
    default: () => []
  },
  notifications: {
    type: Array,
    default: () => []
  },
  unreadNotifications: {
    type: Number,
    default: 0
  }
});

defineEmits([
  'sign-out',
  'select-account',
  'submit-transaction',
  'create-account',
  'update-financial-controls',
  'delete-financial-controls',
  'upload-kyc-document',
  'send-transfer',
  'refresh-transactions',
  'refresh-notifications',
  'mark-notification-read',
  'mark-all-notifications-read',
  'delete-notification'
]);

const activeView = ref('overview');
const bellPulse = ref(false);

watch(() => props.unreadNotifications, (nextCount, previousCount) => {
  if (nextCount > previousCount) {
    bellPulse.value = true;
    window.setTimeout(() => {
      bellPulse.value = false;
    }, 1200);
  }
});
</script>

<template>
  <section class="mx-auto grid w-full max-w-[1520px] items-start gap-5 lg:block lg:pl-80">
    <DashboardSidebar
      :customer="customer"
      :active-view="activeView"
      :unread-notifications="unreadNotifications"
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
        <div class="flex flex-wrap items-center justify-end gap-3">
          <button
            type="button"
            class="relative grid size-12 place-items-center rounded-2xl border border-slate-200 bg-white text-slate-700 shadow-sm transition hover:border-emerald-200 hover:bg-emerald-50 hover:text-emerald-700"
            :class="bellPulse || unreadNotifications > 0 ? 'ring-4 ring-emerald-100' : ''"
            aria-label="Open notifications"
            @click="activeView = 'notifications'"
          >
            <i class="pi pi-bell text-lg" :class="bellPulse ? 'animate-pulse' : ''"></i>
            <span
              v-if="unreadNotifications > 0"
              class="absolute -right-1 -top-1 grid min-w-6 place-items-center rounded-full bg-emerald-500 px-1.5 py-0.5 text-[10px] font-black text-white shadow-lg ring-2 ring-white"
            >
              {{ unreadNotifications > 99 ? '99+' : unreadNotifications }}
            </span>
          </button>
          <div class="rounded-2xl bg-slate-950 px-4 py-3 text-white shadow-lg">
            <p class="text-[10px] font-bold uppercase text-white/60">Signed in as</p>
            <p class="mt-1 text-sm font-black">{{ customer.firstName }} {{ customer.lastName }}</p>
          </div>
        </div>
      </header>

      <section v-if="activeView === 'overview'" class="grid min-w-0 gap-5">
        <SummaryTiles
          :customer="customer"
          :total-balance="totalBalance"
          :transactions="transactions"
        />
        <AccountTrendChart :transactions="transactions" :selected-account="selectedAccount" />
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

      <section v-else-if="activeView === 'kyc'" class="grid min-w-0 gap-5">
        <KycDocumentsPanel
          :documents="kycDocuments"
          :is-loading="isLoading"
          @upload="$emit('upload-kyc-document', $event)"
        />
      </section>

      <section v-else-if="activeView === 'money'" class="grid min-w-0 gap-5 xl:grid-cols-[minmax(0,1fr)_340px]">
        <TransactionForm
          :selected-account="selectedAccount"
          :is-loading="isLoading"
          @submit="$emit('submit-transaction', $event)"
        />
        <AccountList
          :accounts="customer.accounts"
          :selected-account-id="selectedAccountId"
          @select="$emit('select-account', $event)"
        />
      </section>

      <section v-else-if="activeView === 'controls'" class="grid min-w-0 gap-5">
        <FinancialControlsPanel
          :control="financialControl"
          :is-loading="isLoading"
          @submit="$emit('update-financial-controls', $event)"
          @delete="$emit('delete-financial-controls')"
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
        v-else-if="activeView === 'transactions'"
        :transactions="transactions"
        :is-loading="isLoading"
        @refresh="$emit('refresh-transactions')"
      />

      <NotificationsPanel
        v-else-if="activeView === 'notifications'"
        :notifications="notifications"
        :is-loading="isLoading"
        @refresh="$emit('refresh-notifications')"
        @mark-read="$emit('mark-notification-read', $event)"
        @mark-all-read="$emit('mark-all-notifications-read')"
        @delete="$emit('delete-notification', $event)"
      />

      <section v-else :class="[cardBase, 'grid gap-5 p-5']">
        <div>
          <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Customer Profile</p>
          <h2 class="text-lg font-black tracking-tight text-slate-950">Personal details</h2>
        </div>

        <dl class="grid gap-3 md:grid-cols-2">
          <div class="rounded-2xl bg-slate-50 p-4">
            <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Name</dt>
            <dd class="mt-1 text-sm font-bold text-slate-900">{{ customer.firstName }} {{ customer.lastName }}</dd>
          </div>
          <div class="rounded-2xl bg-slate-50 p-4">
            <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Email</dt>
            <dd class="mt-1 break-words text-sm font-bold text-slate-900">{{ customer.email }}</dd>
          </div>
          <div class="rounded-2xl bg-slate-50 p-4">
            <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Phone</dt>
            <dd class="mt-1 text-sm font-bold text-slate-900">{{ customer.phoneNumber || 'Not provided' }}</dd>
          </div>
          <div class="rounded-2xl bg-slate-50 p-4">
            <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Date of birth</dt>
            <dd class="mt-1 text-sm font-bold text-slate-900">{{ customer.dateOfBirth }}</dd>
          </div>
          <div class="rounded-2xl bg-slate-50 p-4 md:col-span-2">
            <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Address</dt>
            <dd class="mt-1 text-sm font-bold text-slate-900">{{ customer.address || 'Not provided' }}</dd>
          </div>
        </dl>
      </section>
    </main>
  </section>
</template>
