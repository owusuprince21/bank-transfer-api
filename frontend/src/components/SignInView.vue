<script setup>
import { reactive, ref } from 'vue';
import { getFirstZodError, loginSchema } from '../schemas';
import { inputBase, labelBase, primaryButton } from './ui';

defineProps({
  isLoading: {
    type: Boolean,
    default: false
  }
});

const emit = defineEmits(['submit']);

const showPassword = ref(false);
const form = reactive({
  email: '',
  password: '',
  validationError: ''
});

function submit() {
  form.validationError = '';

  const result = loginSchema.safeParse({
    email: form.email,
    password: form.password
  });

  if (!result.success) {
    form.validationError = getFirstZodError(result.error);
    return;
  }

  emit('submit', result.data);
}
</script>

<template>
  <section class="grid min-h-[calc(100vh-2rem)] items-center gap-8 md:min-h-[calc(100vh-4rem)] lg:grid-cols-[minmax(0,1fr)_430px]">
    <div class="signin-hero flex min-h-[360px] flex-col justify-end rounded-[2rem] p-7 text-white shadow-2xl md:min-h-[620px] md:p-12">
      <p class="text-xs font-extrabold uppercase tracking-wide text-emerald-100">ApiDemo Bank</p>
      <h1 class="max-w-3xl text-5xl font-black leading-none tracking-tight md:text-7xl">
        Modern banking dashboard
      </h1>
      <p class="max-w-xl pt-5 text-lg font-semibold leading-8 text-white/85">
        Sign in to manage balances, create accounts, send money, and monitor activity in real time.
      </p>
    </div>

    <form class="grid gap-6 rounded-[2rem] border border-slate-200 bg-white/90 p-6 shadow-xl backdrop-blur" @submit.prevent="submit">
      <div class="grid gap-2">
        <p class="text-xs font-extrabold uppercase tracking-wide text-emerald-700">Secure access</p>
        <h2 class="text-3xl font-black tracking-tight text-slate-950">Sign in</h2>
        <p class="text-sm font-medium text-slate-500">Use your customer email and password.</p>
      </div>

      <label :class="labelBase">
        Email
        <input v-model="form.email" :class="inputBase" type="email" autocomplete="email" required />
      </label>

      <label :class="labelBase">
        Password
        <div class="relative">
          <input
            v-model="form.password"
            :class="[inputBase, 'pr-12']"
            :type="showPassword ? 'text' : 'password'"
            autocomplete="current-password"
            required
          />
          <button
            type="button"
            class="absolute inset-y-0 right-3 text-slate-500 hover:text-slate-950"
            @click="showPassword = !showPassword"
          >
            <i :class="showPassword ? 'pi pi-eye-slash' : 'pi pi-eye'"></i>
          </button>
        </div>
      </label>

      <p v-if="form.validationError" class="rounded-xl bg-amber-50 px-3 py-2 text-sm font-semibold text-amber-700">
        {{ form.validationError }}
      </p>

      <button type="submit" :class="primaryButton" :disabled="isLoading">
        <i class="pi pi-lock"></i>
        {{ isLoading ? 'Signing in...' : 'Sign in' }}
      </button>
    </form>
  </section>
</template>
