<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { Toaster, toast } from 'vue-sonner';
import DashboardView from './components/DashboardView.vue';
import SignInView from './components/SignInView.vue';
import {
  createAccount,
  deposit,
  getAccountTransactions,
  getCustomer,
  login,
  transfer,
  withdraw
} from './api';
import { queryKeys } from './queryKeys';
import 'vue-sonner/style.css';

const idleTimeoutMs = 3 * 60 * 1000;
const activityEvents = ['mousemove', 'mousedown', 'keydown', 'scroll', 'touchstart', 'click'];

const savedCustomer = localStorage.getItem('bankCustomer');
const queryClient = useQueryClient();

const signedInCustomer = ref(savedCustomer ? JSON.parse(savedCustomer) : null);
const selectedAccountId = ref(signedInCustomer.value?.accounts?.[0]?.id ?? '');
const errorMessage = ref('');
let idleTimerId;

const customerId = computed(() => signedInCustomer.value?.id ?? '');

const customerQuery = useQuery({
  queryKey: computed(() => queryKeys.customer(customerId.value)),
  queryFn: () => getCustomer(customerId.value),
  enabled: computed(() => Boolean(customerId.value))
});

const activeCustomer = computed(() => customerQuery.data.value ?? signedInCustomer.value);

const selectedAccount = computed(() => {
  return activeCustomer.value?.accounts?.find((account) => account.id === selectedAccountId.value) ?? null;
});

const totalBalance = computed(() => {
  return activeCustomer.value?.accounts?.reduce((sum, account) => sum + Number(account.balance), 0) ?? 0;
});

const transactionsQuery = useQuery({
  queryKey: computed(() => queryKeys.accountTransactions(selectedAccountId.value)),
  queryFn: () => getAccountTransactions(selectedAccountId.value),
  enabled: computed(() => Boolean(selectedAccountId.value))
});

const loginMutation = useMutation({
  mutationFn: ({ email, password }) => login(email, password),
  onSuccess: (response) => {
    setSignedInCustomer(response.customer);
    selectedAccountId.value = response.customer.accounts[0]?.id ?? '';
    queryClient.setQueryData(queryKeys.customer(response.customer.id), response.customer);
    toast.success(`Welcome back, ${response.customer.firstName}.`);
    resetIdleTimer();
  }
});

const moneyMovementMutation = useMutation({
  mutationFn: (transaction) => {
    if (transaction.type === 'deposit') {
      return deposit(selectedAccountId.value, transaction.amount, transaction.description);
    }

    return withdraw(selectedAccountId.value, transaction.amount, transaction.description);
  },
  onSuccess: async (_, transaction) => {
    toast.success(transaction.type === 'deposit'
      ? 'Deposit completed.'
      : 'Withdrawal completed.');

    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.customer(customerId.value) }),
      queryClient.invalidateQueries({ queryKey: queryKeys.accountTransactions(selectedAccountId.value) })
    ]);
  }
});

const createAccountMutation = useMutation({
  mutationFn: (request) => createAccount(
    customerId.value,
    request.accountType,
    request.openingDeposit
  ),
  onSuccess: async (account) => {
    toast.success(`${account.accountType} account created.`);
    selectedAccountId.value = account.id;
    await queryClient.invalidateQueries({ queryKey: queryKeys.customer(customerId.value) });
  }
});

const transferMutation = useMutation({
  mutationFn: (request) => transfer(
    selectedAccountId.value,
    request.destinationAccountId,
    request.amount,
    request.description
  ),
  onSuccess: async () => {
    toast.success('Transfer completed.');
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.customer(customerId.value) }),
      queryClient.invalidateQueries({ queryKey: queryKeys.accountTransactions(selectedAccountId.value) })
    ]);
  }
});

const isBusy = computed(() => {
  return loginMutation.isPending.value
    || moneyMovementMutation.isPending.value
    || createAccountMutation.isPending.value
    || transferMutation.isPending.value
    || customerQuery.isFetching.value
    || transactionsQuery.isFetching.value;
});

const transactions = computed(() => transactionsQuery.data.value ?? []);

function clearMessages() {
  errorMessage.value = '';
}

function setSignedInCustomer(customer) {
  signedInCustomer.value = customer;
  localStorage.setItem('bankCustomer', JSON.stringify(customer));
}

async function handleLogin(credentials) {
  clearMessages();

  try {
    await loginMutation.mutateAsync(credentials);
  } catch (error) {
    errorMessage.value = error.message;
    toast.error(error.message);
  }
}

function handleAccountSelect(accountId) {
  clearMessages();
  selectedAccountId.value = accountId;
}

async function handleTransaction(transaction) {
  if (!selectedAccountId.value) {
    errorMessage.value = 'Select an account first.';
    toast.error(errorMessage.value);
    return;
  }

  clearMessages();

  try {
    await moneyMovementMutation.mutateAsync(transaction);
  } catch (error) {
    errorMessage.value = error.message;
    toast.error(error.message);
  }
}

async function handleCreateAccount(request) {
  clearMessages();

  try {
    await createAccountMutation.mutateAsync(request);
  } catch (error) {
    errorMessage.value = error.message;
    toast.error(error.message);
  }
}

async function handleTransfer(request) {
  if (!selectedAccountId.value) {
    errorMessage.value = 'Select an account first.';
    toast.error(errorMessage.value);
    return;
  }

  clearMessages();

  try {
    await transferMutation.mutateAsync(request);
  } catch (error) {
    errorMessage.value = error.message;
    toast.error(error.message);
  }
}

function signOut(options = {}) {
  signedInCustomer.value = null;
  selectedAccountId.value = '';
  queryClient.clear();
  localStorage.removeItem('bankCustomer');
  clearMessages();

  if (options.reason === 'idle') {
    toast.info('You were signed out after 3 minutes of inactivity.');
  }
}

function resetIdleTimer() {
  window.clearTimeout(idleTimerId);

  if (!signedInCustomer.value) {
    return;
  }

  idleTimerId = window.setTimeout(() => {
    signOut({ reason: 'idle' });
  }, idleTimeoutMs);
}

function handleActivity() {
  resetIdleTimer();
}

watch(customerQuery.data, (freshCustomer) => {
  if (!freshCustomer) {
    return;
  }

  setSignedInCustomer(freshCustomer);

  if (!selectedAccountId.value && freshCustomer.accounts.length > 0) {
    selectedAccountId.value = freshCustomer.accounts[0].id;
  }
});

watch(signedInCustomer, (nextCustomer) => {
  if (nextCustomer) {
    resetIdleTimer();
  } else {
    window.clearTimeout(idleTimerId);
  }
});

onMounted(() => {
  activityEvents.forEach((eventName) => {
    window.addEventListener(eventName, handleActivity, { passive: true });
  });

  resetIdleTimer();
});

onBeforeUnmount(() => {
  window.clearTimeout(idleTimerId);
  activityEvents.forEach((eventName) => {
    window.removeEventListener(eventName, handleActivity);
  });
});
</script>

<template>
  <Toaster rich-colors position="top-right" />

  <main class="min-h-screen overflow-x-hidden bg-[radial-gradient(circle_at_top_left,#ecfdf5,transparent_34%),linear-gradient(135deg,#f8fafc_0%,#eef6f3_48%,#f8fafc_100%)] p-4 text-slate-950 md:p-6">
    <SignInView
      v-if="!activeCustomer"
      :is-loading="isBusy"
      @submit="handleLogin"
    />

    <DashboardView
      v-else
      :customer="activeCustomer"
      :selected-account="selectedAccount"
      :selected-account-id="selectedAccountId"
      :total-balance="totalBalance"
      :transactions="transactions"
      :is-loading="isBusy"
      @sign-out="signOut"
      @select-account="handleAccountSelect"
      @submit-transaction="handleTransaction"
      @create-account="handleCreateAccount"
      @send-transfer="handleTransfer"
      @refresh-transactions="transactionsQuery.refetch"
    />
  </main>
</template>
