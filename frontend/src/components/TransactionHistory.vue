<script setup>
import { computed, ref, watch } from 'vue';
import { currency, formatDate, isCreditTransaction, transactionTypeLabel } from '../utils/formatters';
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

const pageSize = 10;
const currentPage = ref(1);
const openActionMenuId = ref('');
const actionMenuStyle = ref({});
const selectedTransaction = ref(null);

const rows = computed(() => {
  return props.transactions.map((transaction) => ({
    ...transaction,
    isCredit: isCreditTransaction(transaction.transactionType),
    transactionTypeLabel: transactionTypeLabel(transaction.transactionType)
  }));
});

const totalPages = computed(() => Math.max(1, Math.ceil(rows.value.length / pageSize)));

const pagedRows = computed(() => {
  const start = (currentPage.value - 1) * pageSize;
  return rows.value.slice(start, start + pageSize);
});

const pageStart = computed(() => rows.value.length === 0 ? 0 : ((currentPage.value - 1) * pageSize) + 1);
const pageEnd = computed(() => Math.min(currentPage.value * pageSize, rows.value.length));
const activeActionTransaction = computed(() => rows.value.find((transaction) => transaction.id === openActionMenuId.value) ?? null);

function previousPage() {
  currentPage.value = Math.max(1, currentPage.value - 1);
}

function nextPage() {
  currentPage.value = Math.min(totalPages.value, currentPage.value + 1);
}

function toggleActionMenu(transactionId, event) {
  if (openActionMenuId.value === transactionId) {
    openActionMenuId.value = '';
    return;
  }

  const buttonRect = event.currentTarget.getBoundingClientRect();
  openActionMenuId.value = transactionId;
  actionMenuStyle.value = {
    position: 'fixed',
    top: `${buttonRect.bottom + 8}px`,
    left: `${Math.max(12, buttonRect.right - 176)}px`
  };
}

function viewTransaction(transaction) {
  selectedTransaction.value = transaction;
  openActionMenuId.value = '';
}

function closeTransactionDetails() {
  selectedTransaction.value = null;
}

watch(rows, () => {
  currentPage.value = Math.min(currentPage.value, totalPages.value);
  if (currentPage.value < 1) {
    currentPage.value = 1;
  }

  openActionMenuId.value = '';
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
      <table class="w-full min-w-[860px] border-collapse text-left text-xs">
        <thead class="bg-slate-50 text-xs font-black uppercase tracking-wide text-slate-500">
          <tr>
            <th class="px-3 py-2.5">Type</th>
            <th class="px-3 py-2.5">Description</th>
            <th class="px-3 py-2.5">Amount</th>
            <th class="px-3 py-2.5">Balance</th>
            <th class="px-3 py-2.5">Date</th>
            <th class="px-3 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-200 bg-white">
          <tr v-for="transaction in pagedRows" :key="transaction.id" class="hover:bg-slate-50/80">
            <td class="px-3 py-3">
              <span
                class="inline-flex rounded-full px-2 py-1 text-[11px] font-black"
                :class="transaction.isCredit ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'"
              >
                {{ transaction.transactionTypeLabel }}
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
            <td class="px-3 py-3 text-right">
              <div class="relative inline-flex">
                <button
                  type="button"
                  class="grid size-9 place-items-center rounded-xl text-slate-500 transition hover:bg-slate-100 hover:text-slate-950"
                  :aria-expanded="openActionMenuId === transaction.id"
                  aria-label="Transaction actions"
                  @click="toggleActionMenu(transaction.id, $event)"
                >
                  <i class="pi pi-ellipsis-v"></i>
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="rows.length > pageSize" class="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-slate-50 px-3 py-2">
      <p class="text-xs font-bold text-slate-500">
        Showing {{ pageStart }}-{{ pageEnd }} of {{ rows.length }}
      </p>
      <div class="flex items-center gap-2">
        <button type="button" :class="secondaryButton" :disabled="currentPage === 1" @click="previousPage">
          Previous
        </button>
        <span class="rounded-xl bg-white px-3 py-2 text-xs font-black text-slate-600">
          {{ currentPage }} / {{ totalPages }}
        </span>
        <button type="button" :class="secondaryButton" :disabled="currentPage === totalPages" @click="nextPage">
          Next
        </button>
      </div>
    </div>

    <Teleport to="body">
      <div
        v-if="activeActionTransaction"
        class="z-[60] w-44 overflow-hidden rounded-xl border border-slate-200 bg-white py-1 text-left shadow-xl"
        :style="actionMenuStyle"
      >
        <button
          type="button"
          class="flex w-full items-center gap-2 px-3 py-2 text-xs font-bold text-slate-700 transition hover:bg-slate-50 hover:text-slate-950"
          @click="viewTransaction(activeActionTransaction)"
        >
          <i class="pi pi-eye text-emerald-600"></i>
          View details
        </button>
      </div>
    </Teleport>

    <Teleport to="body">
      <div
        v-if="selectedTransaction"
        class="fixed inset-0 z-50 grid place-items-center bg-slate-950/55 p-4 backdrop-blur-sm"
        role="dialog"
        aria-modal="true"
      >
        <section class="grid max-h-[92vh] w-full max-w-3xl grid-rows-[auto_minmax(0,1fr)_auto] overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl">
          <header class="flex items-start justify-between gap-4 border-b border-slate-100 p-5">
            <div class="min-w-0">
              <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Transaction details</p>
              <h2 class="text-xl font-black tracking-tight text-slate-950">{{ selectedTransaction.transactionTypeLabel }}</h2>
              <p class="mt-1 break-all text-xs font-semibold text-slate-500">{{ selectedTransaction.referenceNumber }}</p>
            </div>
            <button
              type="button"
              class="grid size-9 place-items-center rounded-xl bg-slate-100 text-slate-500 transition hover:bg-slate-200 hover:text-slate-950"
              aria-label="Close transaction details"
              @click="closeTransactionDetails"
            >
              <i class="pi pi-times"></i>
            </button>
          </header>

          <div class="min-h-0 overflow-y-auto p-5">
            <div class="grid gap-4 md:grid-cols-2">
              <div class="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                <p class="text-[11px] font-black uppercase tracking-wide text-slate-500">Amount</p>
                <p
                  class="mt-2 text-2xl font-black"
                  :class="selectedTransaction.isCredit ? 'text-emerald-700' : 'text-slate-950'"
                >
                  {{ selectedTransaction.isCredit ? '+' : '-' }}{{ currency(selectedTransaction.amount) }}
                </p>
              </div>
              <div class="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                <p class="text-[11px] font-black uppercase tracking-wide text-slate-500">Balance after</p>
                <p class="mt-2 text-2xl font-black text-slate-950">{{ currency(selectedTransaction.balanceAfterTransaction) }}</p>
              </div>
            </div>

            <div class="mt-5 grid gap-4 md:grid-cols-2">
              <div class="rounded-2xl border border-slate-200 p-4">
                <h3 class="text-sm font-black text-slate-950">Account</h3>
                <dl class="mt-3 grid gap-3 text-xs">
                  <div>
                    <dt class="font-black uppercase tracking-wide text-slate-400">Account number</dt>
                    <dd class="mt-1 break-all font-bold text-slate-800">{{ selectedTransaction.accountNumber || 'Not provided' }}</dd>
                  </div>
                  <div>
                    <dt class="font-black uppercase tracking-wide text-slate-400">Account type</dt>
                    <dd class="mt-1 font-bold text-slate-800">{{ selectedTransaction.accountType || 'Not provided' }}</dd>
                  </div>
                  <div>
                    <dt class="font-black uppercase tracking-wide text-slate-400">Currency</dt>
                    <dd class="mt-1 font-bold text-slate-800">{{ selectedTransaction.currency || 'GHS' }}</dd>
                  </div>
                </dl>
              </div>

              <div class="rounded-2xl border border-slate-200 p-4">
                <h3 class="text-sm font-black text-slate-950">
                  {{ selectedTransaction.transactionType === 'TransferReceived' ? 'Sender' : 'Recipient' }}
                </h3>
                <dl class="mt-3 grid gap-3 text-xs">
                  <div>
                    <dt class="font-black uppercase tracking-wide text-slate-400">Name</dt>
                    <dd class="mt-1 break-all font-bold text-slate-800">{{ selectedTransaction.counterpartyName || 'Not applicable' }}</dd>
                  </div>
                  <div>
                    <dt class="font-black uppercase tracking-wide text-slate-400">Account number</dt>
                    <dd class="mt-1 break-all font-bold text-slate-800">{{ selectedTransaction.counterpartyAccountNumber || 'Not applicable' }}</dd>
                  </div>
                  <div>
                    <dt class="font-black uppercase tracking-wide text-slate-400">Account type</dt>
                    <dd class="mt-1 font-bold text-slate-800">{{ selectedTransaction.counterpartyAccountType || 'Not applicable' }}</dd>
                  </div>
                  <div>
                    <dt class="font-black uppercase tracking-wide text-slate-400">Email</dt>
                    <dd class="mt-1 break-all font-bold text-slate-800">{{ selectedTransaction.counterpartyEmail || 'Not applicable' }}</dd>
                  </div>
                </dl>
              </div>
            </div>

            <div class="mt-5 rounded-2xl border border-slate-200 p-4">
              <h3 class="text-sm font-black text-slate-950">Payment information</h3>
              <dl class="mt-3 grid gap-3 text-xs md:grid-cols-2">
                <div>
                  <dt class="font-black uppercase tracking-wide text-slate-400">Description</dt>
                  <dd class="mt-1 break-words font-bold text-slate-800">{{ selectedTransaction.description || 'No description' }}</dd>
                </div>
                <div>
                  <dt class="font-black uppercase tracking-wide text-slate-400">Reference</dt>
                  <dd class="mt-1 break-all font-bold text-slate-800">{{ selectedTransaction.referenceNumber }}</dd>
                </div>
                <div>
                  <dt class="font-black uppercase tracking-wide text-slate-400">Transaction date</dt>
                  <dd class="mt-1 font-bold text-slate-800">{{ formatDate(selectedTransaction.createdAtUtc) }}</dd>
                </div>
                <div>
                  <dt class="font-black uppercase tracking-wide text-slate-400">Transaction ID</dt>
                  <dd class="mt-1 break-all font-bold text-slate-800">{{ selectedTransaction.id }}</dd>
                </div>
              </dl>
            </div>
          </div>

          <footer class="flex justify-end border-t border-slate-100 p-4">
            <button type="button" :class="secondaryButton" @click="closeTransactionDetails">Close</button>
          </footer>
        </section>
      </div>
    </Teleport>
  </section>
</template>
