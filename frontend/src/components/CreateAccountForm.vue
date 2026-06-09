<script setup>
import { reactive } from 'vue';
import { createAccountSchema, getFirstZodError } from '../schemas';
import { cardBase, inputBase, labelBase, primaryButton } from './ui';

defineProps({
  isLoading: {
    type: Boolean,
    default: false
  }
});

const emit = defineEmits(['submit']);

const form = reactive({
  accountType: 'Savings',
  currency: 'GHS',
  openingDeposit: 0,
  dailyTransferLimit: 10000,
  dailyWithdrawalLimit: 5000,
  allowInternationalTransfers: false,
  validationError: ''
});

const accountTypes = ['Savings', 'Current', 'Business', 'Investment'];

function submit() {
  form.validationError = '';
  const result = createAccountSchema.safeParse({
    accountType: form.accountType,
    currency: form.currency,
    openingDeposit: Number(form.openingDeposit),
    dailyTransferLimit: Number(form.dailyTransferLimit),
    dailyWithdrawalLimit: Number(form.dailyWithdrawalLimit),
    allowInternationalTransfers: form.allowInternationalTransfers
  });

  if (!result.success) {
    form.validationError = getFirstZodError(result.error);
    return;
  }

  emit('submit', result.data);
  form.openingDeposit = 0;
}
</script>

<template>
  <form :class="[cardBase, 'grid gap-4 p-4']" @submit.prevent="submit">
    <div>
      <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">New Account</p>
      <h2 class="text-lg font-black tracking-tight text-slate-950">Create account</h2>
    </div>

    <label :class="labelBase">
      Account type
      <select v-model="form.accountType" :class="inputBase">
        <option v-for="type in accountTypes" :key="type" :value="type">{{ type }}</option>
      </select>
    </label>

    <label :class="labelBase">
      Opening deposit
      <input v-model.number="form.openingDeposit" :class="inputBase" type="number" min="0" step="0.01" />
    </label>

    <div class="grid gap-4 md:grid-cols-2">
      <label :class="labelBase">
        Currency
        <select v-model="form.currency" :class="inputBase">
          <option value="GHS">GHS</option>
          <option value="USD">USD</option>
          <option value="EUR">EUR</option>
          <option value="GBP">GBP</option>
        </select>
      </label>

      <label :class="labelBase">
        Daily transfer limit
        <input v-model.number="form.dailyTransferLimit" :class="inputBase" type="number" min="0" step="0.01" />
      </label>

      <label :class="labelBase">
        Daily withdrawal limit
        <input v-model.number="form.dailyWithdrawalLimit" :class="inputBase" type="number" min="0" step="0.01" />
      </label>
    </div>

    <label class="flex items-center gap-3 rounded-2xl bg-slate-50 p-3 text-xs font-bold text-slate-700">
      <input v-model="form.allowInternationalTransfers" type="checkbox" class="size-4 accent-emerald-600" />
      Allow international transfers
    </label>

    <p v-if="form.validationError" class="rounded-xl bg-amber-50 px-3 py-2 text-xs font-semibold text-amber-700">
      {{ form.validationError }}
    </p>

    <button type="submit" :class="primaryButton" :disabled="isLoading">
      <i class="pi pi-plus"></i>
      Create account
    </button>
  </form>
</template>
