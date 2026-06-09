<script setup>
import { reactive, watch } from 'vue';
import { toast } from 'vue-sonner';
import { getAccountByNumber } from '../api';
import { getFirstZodError, transferSchema } from '../schemas';
import { currency } from '../utils/formatters';
import { cardBase, inputBase, labelBase, primaryButton, secondaryButton } from './ui';

const props = defineProps({
  selectedAccount: {
    type: Object,
    default: null
  },
  isLoading: {
    type: Boolean,
    default: false
  }
});

const emit = defineEmits(['submit']);

const form = reactive({
  accountNumber: '',
  destinationAccount: null,
  amount: null,
  description: '',
  validationError: '',
  lookupError: '',
  isVerifying: false
});

function verifyButtonClass() {
  if (!form.accountNumber.trim()) {
    return secondaryButton;
  }

  if (form.destinationAccount) {
    return 'inline-flex min-h-9 items-center justify-center gap-2 rounded-xl bg-emerald-600 px-3 py-2 text-xs font-bold text-white shadow-sm transition hover:bg-emerald-700 disabled:pointer-events-none disabled:opacity-50';
  }

  return 'inline-flex min-h-9 items-center justify-center gap-2 rounded-xl bg-slate-950 px-3 py-2 text-xs font-bold text-white shadow-sm transition hover:bg-slate-800 disabled:pointer-events-none disabled:opacity-50';
}

watch(() => form.accountNumber, () => {
  form.destinationAccount = null;
  form.lookupError = '';
});

async function verifyAccount() {
  form.validationError = '';
  form.lookupError = '';
  form.destinationAccount = null;

  const accountNumber = form.accountNumber.trim();
  if (!accountNumber) {
    form.validationError = 'Enter a recipient account number.';
    toast.error(form.validationError);
    return;
  }

  if (props.selectedAccount?.accountNumber === accountNumber) {
    form.validationError = 'You cannot transfer to the same account.';
    toast.error(form.validationError);
    return;
  }

  form.isVerifying = true;

  try {
    form.destinationAccount = await getAccountByNumber(accountNumber);
    toast.success(`Verified ${form.destinationAccount.customerName}.`);
  } catch (error) {
    form.lookupError = error.message;
    toast.error(error.message);
  } finally {
    form.isVerifying = false;
  }
}

function submit() {
  form.validationError = '';
  const result = transferSchema.safeParse({
    destinationAccountId: form.destinationAccount?.id ?? '',
    accountNumber: form.accountNumber.trim(),
    amount: Number(form.amount),
    description: form.description?.trim() || undefined
  });

  if (!result.success) {
    form.validationError = getFirstZodError(result.error);
    toast.error(form.validationError);
    return;
  }

  emit('submit', {
    destinationAccountId: result.data.destinationAccountId,
    amount: result.data.amount,
    description: result.data.description,
    recipientName: form.destinationAccount.customerName,
    recipientAccountNumber: form.destinationAccount.accountNumber
  });

  form.amount = null;
  form.description = '';
}
</script>

<template>
  <form :class="[cardBase, 'grid gap-4 p-4']" @submit.prevent="submit">
    <div class="grid gap-2 md:grid-cols-[1fr_auto] md:items-start">
      <div>
        <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Transfer</p>
        <h2 class="text-lg font-black tracking-tight text-slate-950">Send by account number</h2>
      </div>
      <div class="rounded-xl bg-slate-50 px-3 py-2 text-right">
        <p class="text-[10px] font-bold uppercase text-slate-500">Available</p>
        <p class="text-sm font-black text-slate-950">{{ currency(selectedAccount?.balance ?? 0) }}</p>
      </div>
    </div>

    <label :class="labelBase">
      Recipient account number
      <div class="grid gap-2 sm:grid-cols-[1fr_auto]">
        <input
          v-model="form.accountNumber"
          :class="inputBase"
          type="text"
          inputmode="numeric"
          placeholder="Enter account number"
        />
        <button
          type="button"
          :class="verifyButtonClass()"
          :disabled="form.isVerifying || !form.accountNumber.trim()"
          @click="verifyAccount"
        >
          <i :class="form.destinationAccount ? 'pi pi-check' : 'pi pi-search'"></i>
          {{ form.isVerifying ? 'Checking...' : form.destinationAccount ? 'Verified' : 'Verify' }}
        </button>
      </div>
    </label>

    <div
      v-if="form.destinationAccount"
      class="rounded-2xl border border-emerald-200 bg-emerald-50 p-3"
    >
      <p class="text-[10px] font-black uppercase tracking-wide text-emerald-700">Verified recipient</p>
      <p class="mt-1 text-sm font-black text-emerald-950">{{ form.destinationAccount.customerName }}</p>
      <p class="text-xs font-semibold text-emerald-700">{{ form.destinationAccount.customerEmail }}</p>
      <p class="mt-2 text-xs font-semibold text-emerald-800">
        {{ form.destinationAccount.accountType }} - {{ form.destinationAccount.accountNumber }}
      </p>
    </div>

    <div class="grid gap-4 md:grid-cols-2">
      <label :class="labelBase">
        Amount
        <input v-model.number="form.amount" :class="inputBase" type="number" min="0.01" step="0.01" />
      </label>

      <label :class="labelBase">
        Note
        <input v-model="form.description" :class="inputBase" type="text" maxlength="500" placeholder="Optional" />
      </label>
    </div>

    <button type="submit" :class="primaryButton" :disabled="isLoading || !selectedAccount || !form.destinationAccount">
      <i class="pi pi-send"></i>
      Send money
    </button>
  </form>
</template>
