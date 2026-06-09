<script setup>
import { reactive, ref, watch } from 'vue';
import { toast } from 'vue-sonner';
import { getFirstZodError, spendingControlSchema } from '../schemas';
import { currency, formatDate } from '../utils/formatters';
import { cardBase, inputBase, labelBase, primaryButton, secondaryButton } from './ui';

const props = defineProps({
  control: {
    type: Object,
    default: null
  },
  isLoading: {
    type: Boolean,
    default: false
  }
});

const emit = defineEmits(['submit', 'delete']);

const openActionMenu = ref(false);
const form = reactive({
  monthlySpendLimit: 0,
  singleTransactionLimit: 0,
  savingsTarget: 0,
  blockTransfersWhenLimitReached: false
});

function resetForm() {
  form.monthlySpendLimit = 0;
  form.singleTransactionLimit = 0;
  form.savingsTarget = 0;
  form.blockTransfersWhenLimitReached = false;
}

function editControl() {
  if (!props.control) {
    return;
  }

  form.monthlySpendLimit = Number(props.control.monthlySpendLimit ?? 0);
  form.singleTransactionLimit = Number(props.control.singleTransactionLimit ?? 0);
  form.savingsTarget = Number(props.control.savingsTarget ?? 0);
  form.blockTransfersWhenLimitReached = Boolean(props.control.blockTransfersWhenLimitReached);
  openActionMenu.value = false;
}

function submit() {
  const result = spendingControlSchema.safeParse({
    monthlySpendLimit: Number(form.monthlySpendLimit),
    singleTransactionLimit: Number(form.singleTransactionLimit),
    savingsTarget: Number(form.savingsTarget),
    blockTransfersWhenLimitReached: form.blockTransfersWhenLimitReached
  });

  if (!result.success) {
    toast.error(getFirstZodError(result.error));
    return;
  }

  emit('submit', result.data);
  resetForm();
}

function requestDelete() {
  openActionMenu.value = false;
  emit('delete');
}

watch(() => props.control, (control) => {
  if (!control) {
    resetForm();
    return;
  }

  form.monthlySpendLimit = Number(control.monthlySpendLimit ?? 0);
  form.singleTransactionLimit = Number(control.singleTransactionLimit ?? 0);
  form.savingsTarget = Number(control.savingsTarget ?? 0);
  form.blockTransfersWhenLimitReached = Boolean(control.blockTransfersWhenLimitReached);
}, { immediate: true });
</script>

<template>
  <section class="grid gap-5">
    <form :class="[cardBase, 'grid gap-4 p-4']" @submit.prevent="submit">
      <div>
        <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Money controls</p>
        <h2 class="text-lg font-black tracking-tight text-slate-950">Financial management</h2>
        <p class="mt-1 text-xs font-semibold leading-5 text-slate-500">
          These controls become your active outgoing spend rules for withdrawals and transfers.
        </p>
      </div>

      <div class="grid items-start gap-4 md:grid-cols-3">
        <label :class="[labelBase, 'content-start']">
          Monthly spend limit
          <input v-model.number="form.monthlySpendLimit" :class="inputBase" type="number" min="0" step="0.01" />
        </label>
        <label :class="[labelBase, 'content-start']">
          Single transaction limit
          <input v-model.number="form.singleTransactionLimit" :class="inputBase" type="number" min="0" step="0.01" />
          <span class="text-[11px] font-semibold leading-4 text-slate-500">Maximum amount allowed for one withdrawal or transfer.</span>
        </label>
        <label :class="[labelBase, 'content-start']">
          Savings target
          <input v-model.number="form.savingsTarget" :class="inputBase" type="number" min="0" step="0.01" />
        </label>
      </div>

      <label class="flex items-center gap-3 rounded-2xl bg-slate-50 p-3 text-sm font-bold text-slate-700">
        <input v-model="form.blockTransfersWhenLimitReached" type="checkbox" class="size-4 accent-emerald-600" />
        Block withdrawals and transfers when monthly limit is reached
      </label>

      <div class="flex flex-wrap justify-end gap-2">
        <button type="button" :class="secondaryButton" :disabled="isLoading" @click="resetForm">
          Clear
        </button>
        <button type="submit" :class="primaryButton" :disabled="isLoading">
          <i class="pi pi-shield"></i>
          {{ isLoading ? 'Saving...' : control ? 'Save new settings' : 'Add controls' }}
        </button>
      </div>
    </form>

    <section :class="[cardBase, 'grid gap-4 p-4']">
      <div>
        <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Saved controls</p>
        <h2 class="text-lg font-black tracking-tight text-slate-950">Active money-control rule</h2>
      </div>

      <div v-if="!control" class="grid min-h-32 place-items-center rounded-2xl border border-dashed border-slate-200 bg-slate-50 text-xs font-semibold text-slate-500">
        No money controls added yet.
      </div>

      <div v-else class="overflow-x-visible rounded-2xl border border-slate-200">
        <table class="w-full min-w-[900px] text-left text-xs">
          <thead class="bg-slate-50 font-black uppercase tracking-wide text-slate-500">
            <tr>
              <th class="px-3 py-2.5">Monthly limit</th>
              <th class="px-3 py-2.5">Single limit</th>
              <th class="px-3 py-2.5">Savings target</th>
              <th class="px-3 py-2.5">Block at limit</th>
              <th class="px-3 py-2.5">Updated</th>
              <th class="px-3 py-2.5">Action</th>
            </tr>
          </thead>
          <tbody class="bg-white">
            <tr class="hover:bg-slate-50/80">
              <td class="px-3 py-3 font-bold text-slate-900">{{ currency(control.monthlySpendLimit) }}</td>
              <td class="px-3 py-3 font-bold text-slate-900">{{ currency(control.singleTransactionLimit) }}</td>
              <td class="px-3 py-3 font-bold text-slate-900">{{ currency(control.savingsTarget) }}</td>
              <td class="px-3 py-3">
                <span class="rounded-full px-2 py-1 text-[11px] font-black" :class="control.blockTransfersWhenLimitReached ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'">
                  {{ control.blockTransfersWhenLimitReached ? 'Enabled' : 'Disabled' }}
                </span>
              </td>
              <td class="px-3 py-3 font-semibold text-slate-500">{{ formatDate(control.updatedAtUtc) }}</td>
              <td class="relative px-3 py-3">
                <button
                  type="button"
                  class="grid size-9 place-items-center rounded-xl border border-slate-200 bg-white text-slate-600 shadow-sm transition hover:border-slate-300 hover:bg-slate-50 hover:text-slate-950 disabled:opacity-50"
                  :disabled="isLoading"
                  :aria-expanded="openActionMenu"
                  @click="openActionMenu = !openActionMenu"
                >
                  <i class="pi pi-ellipsis-v"></i>
                </button>

                <div
                  v-if="openActionMenu"
                  class="absolute right-3 top-12 z-20 grid min-w-40 gap-1 rounded-2xl border border-slate-200 bg-white p-2 shadow-2xl"
                >
                  <button
                    type="button"
                    class="flex min-h-9 items-center gap-2 rounded-xl px-3 text-left text-xs font-bold text-slate-700 hover:bg-slate-50 hover:text-slate-950"
                    @click="editControl"
                  >
                    <i class="pi pi-pencil"></i>
                    Update
                  </button>
                  <button
                    type="button"
                    class="flex min-h-9 items-center gap-2 rounded-xl px-3 text-left text-xs font-bold text-red-600 hover:bg-red-50"
                    @click="requestDelete"
                  >
                    <i class="pi pi-trash"></i>
                    Delete
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </section>
</template>
