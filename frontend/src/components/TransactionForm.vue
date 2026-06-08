<script setup>
import { reactive } from 'vue';
import { getFirstZodError, transactionSchema } from '../schemas';
import { currency } from '../utils/formatters';
import { cardBase, inputBase, labelBase, primaryButton, secondaryButton } from './ui';

defineProps({
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
  type: 'deposit',
  amount: null,
  description: '',
  validationError: ''
});

function setType(type) {
  form.type = type;
}

function submit() {
  form.validationError = '';

  const result = transactionSchema.safeParse({
    type: form.type,
    amount: Number(form.amount),
    description: form.description?.trim() || undefined
  });

  if (!result.success) {
    form.validationError = getFirstZodError(result.error);
    return;
  }

  emit('submit', result.data);
  form.amount = null;
  form.description = '';
}
</script>

<template>
  <form :class="[cardBase, 'grid gap-4 p-4']" @submit.prevent="submit">
    <div class="grid gap-3 md:grid-cols-[1fr_auto] md:items-start">
      <div>
        <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Quick action</p>
        <h2 class="text-lg font-black tracking-tight text-slate-950">Move money</h2>
      </div>
      <div class="rounded-xl bg-emerald-50 px-3 py-2 text-right">
        <p class="text-[10px] font-bold uppercase text-emerald-700">Selected balance</p>
        <p class="text-sm font-black text-emerald-950">{{ currency(selectedAccount?.balance ?? 0) }}</p>
      </div>
    </div>

    <div class="grid grid-cols-2 gap-2 rounded-xl bg-slate-100 p-1">
      <button
        type="button"
        :class="[form.type === 'deposit' ? primaryButton : secondaryButton, 'rounded-xl']"
        @click="setType('deposit')"
      >
        <i class="pi pi-arrow-down-left"></i>
        Deposit
      </button>
      <button
        type="button"
        :class="[form.type === 'withdrawal' ? primaryButton : secondaryButton, 'rounded-xl']"
        @click="setType('withdrawal')"
      >
        <i class="pi pi-arrow-up-right"></i>
        Withdraw
      </button>
    </div>

    <div class="grid gap-4 md:grid-cols-2">
      <label :class="labelBase">
        Amount
        <input v-model.number="form.amount" :class="inputBase" type="number" min="0.01" step="0.01" />
      </label>

      <label :class="labelBase">
        Description
        <input v-model="form.description" :class="inputBase" type="text" maxlength="500" placeholder="Optional" />
      </label>
    </div>

      <p v-if="form.validationError" class="rounded-xl bg-amber-50 px-3 py-2 text-xs font-semibold text-amber-700">
      {{ form.validationError }}
    </p>

    <button type="submit" :class="primaryButton" :disabled="isLoading || !selectedAccount">
      <i class="pi pi-send"></i>
      Submit transaction
    </button>
  </form>
</template>
