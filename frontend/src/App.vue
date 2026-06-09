<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { Toaster, toast } from 'vue-sonner';
import AdminDashboard from './components/AdminDashboard.vue';
import ConfirmationDialog from './components/ConfirmationDialog.vue';
import DashboardView from './components/DashboardView.vue';
import LoadingDialog from './components/LoadingDialog.vue';
import SignInView from './components/SignInView.vue';
import {
  API_BASE_URL,
  createAccount,
  deleteFinancialControls,
  deleteNotification,
  deposit,
  getAccountTransactions,
  getCurrentSession,
  getCustomer,
  getFinancialControls,
  getKycDocuments,
  getNotifications,
  login,
  logout,
  markAllNotificationsAsRead,
  markNotificationAsRead,
  registerCustomer,
  transfer,
  updateFinancialControls,
  uploadKycDocument,
  withdraw
} from './api';
import { queryKeys } from './queryKeys';
import { currency } from './utils/formatters';
import 'vue-sonner/style.css';

const idleTimeoutMs = 3 * 60 * 1000;
const activityEvents = ['mousemove', 'mousedown', 'keydown', 'scroll', 'touchstart', 'click'];

const queryClient = useQueryClient();

localStorage.removeItem('bankCustomer');

const signedInCustomer = ref(null);
const signedInAdmin = ref(null);
const selectedAccountId = ref(signedInCustomer.value?.accounts?.[0]?.id ?? '');
const errorMessage = ref('');
const registrationNotice = ref('');
const pendingConfirmation = ref(null);
const savedFinancialControl = ref(null);
const hasCheckedSession = ref(false);
let idleTimerId;
let sessionFallbackTimerId;
let notificationsConnection;

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

const financialControlsQuery = useQuery({
  queryKey: queryKeys.financialControls(),
  queryFn: getFinancialControls,
  enabled: computed(() => Boolean(customerId.value))
});

const kycDocumentsQuery = useQuery({
  queryKey: queryKeys.kycDocuments(),
  queryFn: getKycDocuments,
  enabled: computed(() => Boolean(customerId.value))
});

const notificationsQuery = useQuery({
  queryKey: queryKeys.notifications(),
  queryFn: getNotifications,
  enabled: computed(() => Boolean(customerId.value))
});

const loginMutation = useMutation({
  mutationFn: ({ email, password }) => login(email, password),
  onSuccess: (response) => {
    if (response.role === 'Admin') {
      signedInAdmin.value = response.admin;
      signedInCustomer.value = null;
      selectedAccountId.value = '';
      toast.success(`Welcome back, ${response.admin.fullName}.`);
      resetIdleTimer();
      return;
    }

    setSignedInCustomer(response.customer);
    selectedAccountId.value = response.customer.accounts[0]?.id ?? '';
    queryClient.setQueryData(queryKeys.customer(response.customer.id), response.customer);
    toast.success(`Welcome back, ${response.customer.firstName}.`);
    resetIdleTimer();
  }
});

const registerMutation = useMutation({
  mutationFn: registerCustomer,
  onSuccess: () => {
    registrationNotice.value = 'Registration submitted. Wait for admin approval before signing in.';
    toast.success(registrationNotice.value);
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
      queryClient.invalidateQueries({ queryKey: queryKeys.accountTransactions(selectedAccountId.value) }),
      queryClient.invalidateQueries({ queryKey: queryKeys.notifications() })
    ]);
  }
});

const createAccountMutation = useMutation({
  mutationFn: (request) => createAccount(customerId.value, request),
  onSuccess: async (account) => {
    toast.success(`${account.accountType} account created.`);
    selectedAccountId.value = account.id;
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.customer(customerId.value) }),
      queryClient.invalidateQueries({ queryKey: queryKeys.notifications() })
    ]);
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
      queryClient.invalidateQueries({ queryKey: queryKeys.accountTransactions(selectedAccountId.value) }),
      queryClient.invalidateQueries({ queryKey: queryKeys.notifications() })
    ]);
  }
});

const financialControlsMutation = useMutation({
  mutationFn: updateFinancialControls,
  onSuccess: async (control) => {
    toast.success('Financial controls updated.');
    savedFinancialControl.value = control;
    queryClient.setQueryData(queryKeys.financialControls(), control);
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.financialControls() }),
      queryClient.invalidateQueries({ queryKey: queryKeys.notifications() })
    ]);
  }
});

const deleteFinancialControlsMutation = useMutation({
  mutationFn: deleteFinancialControls,
  onSuccess: async () => {
    toast.success('Financial controls deleted.');
    savedFinancialControl.value = null;
    queryClient.setQueryData(queryKeys.financialControls(), null);
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.financialControls() }),
      queryClient.invalidateQueries({ queryKey: queryKeys.notifications() })
    ]);
  }
});

const markNotificationReadMutation = useMutation({
  mutationFn: markNotificationAsRead,
  onSuccess: (updatedNotification) => {
    queryClient.setQueryData(queryKeys.notifications(), (existing = []) => {
      return existing.map((notification) => notification.id === updatedNotification.id
        ? updatedNotification
        : notification);
    });
  }
});

const markAllNotificationsReadMutation = useMutation({
  mutationFn: markAllNotificationsAsRead,
  onSuccess: () => {
    queryClient.setQueryData(queryKeys.notifications(), (existing = []) => {
      return existing.map((notification) => ({ ...notification, isRead: true }));
    });
  }
});

const deleteNotificationMutation = useMutation({
  mutationFn: deleteNotification,
  onSuccess: (_, notificationId) => {
    queryClient.setQueryData(queryKeys.notifications(), (existing = []) => {
      return existing.filter((notification) => notification.id !== notificationId);
    });
  }
});

const kycUploadMutation = useMutation({
  mutationFn: async (request) => {
    const documents = request.documents ?? [request];
    const responses = [];

    for (const document of documents) {
      responses.push(await uploadKycDocument(document.documentType, document.file));
    }

    return responses;
  },
  onSuccess: async (documents) => {
    toast.success(documents.length === 1 ? 'KYC document uploaded.' : `${documents.length} KYC documents uploaded.`);
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.kycDocuments() }),
      queryClient.invalidateQueries({ queryKey: queryKeys.notifications() })
    ]);
  }
});

const isBusy = computed(() => {
  return loginMutation.isPending.value
    || moneyMovementMutation.isPending.value
    || createAccountMutation.isPending.value
    || transferMutation.isPending.value
    || registerMutation.isPending.value
    || financialControlsMutation.isPending.value
    || deleteFinancialControlsMutation.isPending.value
    || kycUploadMutation.isPending.value
    || markNotificationReadMutation.isPending.value
    || markAllNotificationsReadMutation.isPending.value
    || deleteNotificationMutation.isPending.value
    || customerQuery.isFetching.value
    || transactionsQuery.isFetching.value
    || financialControlsQuery.isFetching.value
    || kycDocumentsQuery.isFetching.value
    || notificationsQuery.isFetching.value;
});

const moneyMovementLoadingMessage = computed(() => {
  const transaction = pendingConfirmation.value?.type === 'money-movement'
    ? pendingConfirmation.value.payload
    : null;

  if (transaction?.type === 'withdrawal') {
    return 'Withdrawing money from your selected account.';
  }

  return 'Depositing money into your selected account.';
});

const transactions = computed(() => transactionsQuery.data.value ?? []);
const notifications = computed(() => notificationsQuery.data.value ?? []);
const unreadNotifications = computed(() => notifications.value.filter((notification) => !notification.isRead).length);

function clearMessages() {
  errorMessage.value = '';
}

function setSignedInCustomer(customer) {
  signedInCustomer.value = customer;
}

async function startNotificationsConnection() {
  if (!signedInCustomer.value) {
    return;
  }

  if (notificationsConnection?.state === HubConnectionState.Connected
    || notificationsConnection?.state === HubConnectionState.Connecting) {
    return;
  }

  notificationsConnection = new HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}/hubs/notifications`, {
      withCredentials: true
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();

  notificationsConnection.on('notificationReceived', (notification) => {
    queryClient.setQueryData(queryKeys.notifications(), (existing = []) => {
      if (existing.some((item) => item.id === notification.id)) {
        return existing;
      }

      return [notification, ...existing].slice(0, 50);
    });
    toast.info(notification.title, {
      description: notification.message,
      duration: 10000
    });
  });

  try {
    await notificationsConnection.start();
  } catch {
    // The notifications list still works over HTTP; websocket reconnects after the next session refresh/login.
  }
}

async function stopNotificationsConnection() {
  if (!notificationsConnection) {
    return;
  }

  const connection = notificationsConnection;
  notificationsConnection = null;
  await connection.stop().catch(() => null);
}

function restoreSession(response) {
  if (!response || signedInCustomer.value || signedInAdmin.value) {
    return;
  }

  if (response.role === 'Admin') {
    signedInAdmin.value = response.admin;
    selectedAccountId.value = '';
    resetIdleTimer();
    return;
  }

  if (response.customer) {
    setSignedInCustomer(response.customer);
    selectedAccountId.value = response.customer.accounts[0]?.id ?? '';
    queryClient.setQueryData(queryKeys.customer(response.customer.id), response.customer);
    resetIdleTimer();
  }
}

async function restoreCurrentSession() {
  const controller = new AbortController();
  const timeoutId = window.setTimeout(() => {
    controller.abort();
  }, 3000);

  try {
    const response = await getCurrentSession({ signal: controller.signal });
    restoreSession(response);
  } catch {
    // No valid cookie, expired cookie, offline API, or timeout: show login.
  } finally {
    window.clearTimeout(timeoutId);
    window.clearTimeout(sessionFallbackTimerId);
    hasCheckedSession.value = true;
  }
}

async function handleLogin(credentials) {
  clearMessages();
  registrationNotice.value = '';

  try {
    await loginMutation.mutateAsync(credentials);
  } catch (error) {
    errorMessage.value = error.message;
    toast.error(error.message);
  }
}

async function handleRegister(request) {
  clearMessages();

  try {
    await registerMutation.mutateAsync(request);
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
  pendingConfirmation.value = {
    type: 'money-movement',
    title: transaction.type === 'deposit' ? 'Confirm deposit?' : 'Confirm withdrawal?',
    description: transaction.type === 'deposit'
      ? 'Confirm the amount before adding money to this account.'
      : 'Confirm the amount before withdrawing money from this account.',
    confirmLabel: transaction.type === 'deposit' ? 'Deposit money' : 'Withdraw money',
    tone: transaction.type === 'withdrawal' ? 'danger' : 'default',
    payload: transaction,
    details: [
      { label: 'Account', value: selectedAccount.value?.accountNumber ?? 'Selected account' },
      { label: 'Type', value: transaction.type === 'deposit' ? 'Deposit' : 'Withdrawal' },
      { label: 'Amount', value: currency(transaction.amount) },
      { label: 'Note', value: transaction.description || 'No note' }
    ]
  };
}

async function completeConfirmedTransaction(transaction) {
  try {
    await moneyMovementMutation.mutateAsync(transaction);
  } catch (error) {
    errorMessage.value = error.message;
    toast.error(error.message);
  }
}

async function handleCreateAccount(request) {
  clearMessages();
  pendingConfirmation.value = {
    type: 'create-account',
    title: 'Create new account?',
    description: 'A new bank account will be created for your customer profile.',
    confirmLabel: 'Create account',
    payload: request,
    details: [
      { label: 'Type', value: request.accountType },
      { label: 'Opening deposit', value: currency(request.openingDeposit) }
    ]
  };
}

async function createConfirmedAccount(request) {
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
  pendingConfirmation.value = {
    type: 'transfer',
    title: 'Send money?',
    description: 'Confirm the recipient and amount before completing this transfer.',
    confirmLabel: 'Send money',
    payload: request,
    details: [
      { label: 'From', value: selectedAccount.value?.accountNumber ?? 'Selected account' },
      { label: 'Recipient', value: request.recipientName ?? 'Verified recipient' },
      { label: 'To', value: request.recipientAccountNumber ?? 'Recipient account' },
      { label: 'Amount', value: currency(request.amount) },
      { label: 'Note', value: request.description || 'No note' }
    ]
  };
}

async function handleFinancialControls(request) {
  pendingConfirmation.value = {
    type: 'financial-controls',
    title: 'Save money controls?',
    description: 'These limits will be applied to withdrawals and transfers from your customer profile.',
    confirmLabel: 'Save controls',
    payload: request,
    details: [
      { label: 'Monthly limit', value: currency(request.monthlySpendLimit) },
      { label: 'Single limit', value: currency(request.singleTransactionLimit) },
      { label: 'Savings target', value: currency(request.savingsTarget) },
      { label: 'Block at limit', value: request.blockTransfersWhenLimitReached ? 'Yes' : 'No' }
    ]
  };
}

async function saveConfirmedFinancialControls(request) {
  try {
    await financialControlsMutation.mutateAsync(request);
  } catch (error) {
    errorMessage.value = error.message;
    toast.error(error.message);
  }
}

async function handleDeleteFinancialControls() {
  pendingConfirmation.value = {
    type: 'delete-financial-controls',
    title: 'Delete money controls?',
    description: 'This removes your saved spending limits and savings target.',
    confirmLabel: 'Delete controls',
    tone: 'danger',
    payload: null,
    details: [
      { label: 'Action', value: 'Remove saved money controls' }
    ]
  };
}

async function deleteConfirmedFinancialControls() {
  try {
    await deleteFinancialControlsMutation.mutateAsync();
  } catch (error) {
    errorMessage.value = error.message;
    toast.error(error.message);
  }
}

async function handleKycUpload(request) {
  try {
    await kycUploadMutation.mutateAsync(request);
  } catch (error) {
    errorMessage.value = error.message;
    toast.error(error.message);
  }
}

async function handleMarkNotificationRead(notificationId) {
  try {
    await markNotificationReadMutation.mutateAsync(notificationId);
  } catch (error) {
    toast.error(error.message);
  }
}

async function handleMarkAllNotificationsRead() {
  try {
    await markAllNotificationsReadMutation.mutateAsync();
  } catch (error) {
    toast.error(error.message);
  }
}

async function handleDeleteNotification(notificationId) {
  try {
    await deleteNotificationMutation.mutateAsync(notificationId);
    toast.success('Notification deleted.');
  } catch (error) {
    toast.error(error.message);
  }
}

async function sendConfirmedTransfer(request) {
  try {
    await transferMutation.mutateAsync(request);
  } catch (error) {
    errorMessage.value = error.message;
    toast.error(error.message);
  }
}

function cancelConfirmation() {
  pendingConfirmation.value = null;
}

async function confirmPendingAction() {
  const confirmation = pendingConfirmation.value;
  if (!confirmation) {
    return;
  }

  if (confirmation.type === 'create-account') {
    await createConfirmedAccount(confirmation.payload);
  }

  if (confirmation.type === 'money-movement') {
    await completeConfirmedTransaction(confirmation.payload);
  }

  if (confirmation.type === 'transfer') {
    await sendConfirmedTransfer(confirmation.payload);
  }

  if (confirmation.type === 'financial-controls') {
    await saveConfirmedFinancialControls(confirmation.payload);
  }

  if (confirmation.type === 'delete-financial-controls') {
    await deleteConfirmedFinancialControls();
  }

  pendingConfirmation.value = null;
}

async function signOut(options = {}) {
  if (signedInCustomer.value || signedInAdmin.value) {
    await logout().catch(() => null);
  }

  await stopNotificationsConnection();
  signedInCustomer.value = null;
  signedInAdmin.value = null;
  savedFinancialControl.value = null;
  selectedAccountId.value = '';
  queryClient.clear();
  clearMessages();

  if (options.reason === 'idle') {
    toast.info('You were signed out after 3 minutes of inactivity.');
  }
}

function resetIdleTimer() {
  window.clearTimeout(idleTimerId);

  if (!signedInCustomer.value && !signedInAdmin.value) {
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

watch(financialControlsQuery.data, (control) => {
  savedFinancialControl.value = control ?? null;
}, { immediate: true });

watch([signedInCustomer, signedInAdmin], ([nextCustomer, nextAdmin]) => {
  if (nextCustomer || nextAdmin) {
    resetIdleTimer();
  } else {
    window.clearTimeout(idleTimerId);
  }
});

watch(signedInCustomer, (nextCustomer) => {
  if (nextCustomer) {
    startNotificationsConnection();
  } else {
    stopNotificationsConnection();
  }
});

onMounted(() => {
  activityEvents.forEach((eventName) => {
    window.addEventListener(eventName, handleActivity, { passive: true });
  });

  sessionFallbackTimerId = window.setTimeout(() => {
    hasCheckedSession.value = true;
  }, 3500);
  restoreCurrentSession();
  resetIdleTimer();
});

onBeforeUnmount(() => {
  window.clearTimeout(idleTimerId);
  window.clearTimeout(sessionFallbackTimerId);
  activityEvents.forEach((eventName) => {
    window.removeEventListener(eventName, handleActivity);
  });
  stopNotificationsConnection();
});
</script>

<template>
  <Toaster rich-colors position="top-right" />

  <ConfirmationDialog
    :open="Boolean(pendingConfirmation)"
    :title="pendingConfirmation?.title"
    :description="pendingConfirmation?.description"
    :confirm-label="pendingConfirmation?.confirmLabel"
    :tone="pendingConfirmation?.tone"
    :details="pendingConfirmation?.details ?? []"
    :is-loading="isBusy"
    @cancel="cancelConfirmation"
    @confirm="confirmPendingAction"
  />

  <LoadingDialog
    :open="moneyMovementMutation.isPending.value"
    title="Processing transaction"
    :description="moneyMovementLoadingMessage"
  />

  <LoadingDialog
    :open="!hasCheckedSession && !activeCustomer && !signedInAdmin"
    title="Restoring session"
    description="Checking your secure banking session."
  />

  <main class="min-h-screen overflow-x-hidden bg-[radial-gradient(circle_at_top_left,#ecfdf5,transparent_34%),linear-gradient(135deg,#f8fafc_0%,#eef6f3_48%,#f8fafc_100%)] p-4 text-slate-950 md:p-6">
    <SignInView
      v-if="hasCheckedSession && !activeCustomer && !signedInAdmin"
      :is-loading="isBusy"
      :registration-notice="registrationNotice"
      @submit="handleLogin"
      @register="handleRegister"
    />

    <AdminDashboard
      v-else-if="signedInAdmin"
      :admin="signedInAdmin"
      @sign-out="signOut"
    />

    <DashboardView
      v-else-if="activeCustomer"
      :customer="activeCustomer"
      :selected-account="selectedAccount"
      :selected-account-id="selectedAccountId"
      :total-balance="totalBalance"
      :transactions="transactions"
      :is-loading="isBusy"
      :financial-control="savedFinancialControl"
      :kyc-documents="kycDocumentsQuery.data.value ?? []"
      :notifications="notifications"
      :unread-notifications="unreadNotifications"
      @sign-out="signOut"
      @select-account="handleAccountSelect"
      @submit-transaction="handleTransaction"
      @create-account="handleCreateAccount"
      @update-financial-controls="handleFinancialControls"
      @delete-financial-controls="handleDeleteFinancialControls"
      @upload-kyc-document="handleKycUpload"
      @send-transfer="handleTransfer"
      @refresh-transactions="transactionsQuery.refetch"
      @refresh-notifications="notificationsQuery.refetch"
      @mark-notification-read="handleMarkNotificationRead"
      @mark-all-notifications-read="handleMarkAllNotificationsRead"
      @delete-notification="handleDeleteNotification"
    />
  </main>
</template>
