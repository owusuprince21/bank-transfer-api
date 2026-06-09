<script setup>
import { reactive, ref, watch } from 'vue';
import { getFirstZodError, loginSchema, registerSchema } from '../schemas';
import { inputBase, labelBase, primaryButton, secondaryButton } from './ui';

const props = defineProps({
  isLoading: {
    type: Boolean,
    default: false
  },
  registrationNotice: {
    type: String,
    default: ''
  }
});

const emit = defineEmits(['submit', 'register']);

const showPassword = ref(false);
const mode = ref('login');
const successMessage = ref('');
const form = reactive({
  email: '',
  password: '',
  firstName: '',
  lastName: '',
  phoneNumber: '',
  dateOfBirth: '',
  address: '',
  nationalIdNumber: '',
  occupation: '',
  employerName: '',
  monthlyIncome: null,
  requestedAccountType: 'Savings',
  validationError: ''
});

function submit() {
  form.validationError = '';
  successMessage.value = '';

  if (mode.value === 'register') {
    const result = registerSchema.safeParse({
      firstName: form.firstName,
      lastName: form.lastName,
      email: form.email,
      password: form.password,
      phoneNumber: form.phoneNumber || undefined,
      dateOfBirth: form.dateOfBirth,
      address: form.address || undefined,
      nationalIdNumber: form.nationalIdNumber,
      occupation: form.occupation || undefined,
      employerName: form.employerName || undefined,
      monthlyIncome: form.monthlyIncome === null || form.monthlyIncome === '' ? undefined : Number(form.monthlyIncome),
      requestedAccountType: form.requestedAccountType || undefined
    });

    if (!result.success) {
      form.validationError = getFirstZodError(result.error);
      return;
    }

    emit('register', result.data);
    return;
  }

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

function setMode(nextMode) {
  mode.value = nextMode;
  form.validationError = '';
  successMessage.value = '';
}

function resetForm() {
  form.email = '';
  form.password = '';
  form.firstName = '';
  form.lastName = '';
  form.phoneNumber = '';
  form.dateOfBirth = '';
  form.address = '';
  form.nationalIdNumber = '';
  form.occupation = '';
  form.employerName = '';
  form.monthlyIncome = null;
  form.requestedAccountType = 'Savings';
  form.validationError = '';
}

watch(() => props.registrationNotice, (notice) => {
  if (!notice) {
    return;
  }

  resetForm();
  mode.value = 'login';
  successMessage.value = notice;
});
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
      <div class="grid gap-3">
        <p class="text-xs font-extrabold uppercase tracking-wide text-emerald-700">Secure access</p>
        <h2 class="text-3xl font-black tracking-tight text-slate-950">
          {{ mode === 'login' ? 'Sign in' : 'Create account' }}
        </h2>
        <div class="grid grid-cols-2 gap-2 rounded-xl bg-slate-100 p-1">
          <button type="button" :class="[mode === 'login' ? primaryButton : secondaryButton, 'rounded-xl']" @click="setMode('login')">
            Login
          </button>
          <button type="button" :class="[mode === 'register' ? primaryButton : secondaryButton, 'rounded-xl']" @click="setMode('register')">
            Register
          </button>
        </div>
      </div>

      <div v-if="mode === 'register'" class="grid gap-4 md:grid-cols-2">
        <label :class="labelBase">
          First name
          <input v-model="form.firstName" :class="inputBase" type="text" autocomplete="given-name" />
        </label>
        <label :class="labelBase">
          Last name
          <input v-model="form.lastName" :class="inputBase" type="text" autocomplete="family-name" />
        </label>
      </div>

      <label :class="labelBase">
        Email
        <input v-model="form.email" :class="inputBase" type="email" autocomplete="email" required />
      </label>

      <div v-if="mode === 'register'" class="grid gap-4 md:grid-cols-2">
        <label :class="labelBase">
          Phone
          <input v-model="form.phoneNumber" :class="inputBase" type="tel" autocomplete="tel" />
        </label>
        <label :class="labelBase">
          Date of birth
          <input v-model="form.dateOfBirth" :class="inputBase" type="date" />
        </label>
        <label :class="labelBase">
          National ID
          <input v-model="form.nationalIdNumber" :class="inputBase" type="text" />
        </label>
        <label :class="labelBase">
          Account type
          <select v-model="form.requestedAccountType" :class="inputBase">
            <option>Savings</option>
            <option>Current</option>
            <option>Business</option>
          </select>
        </label>
        <label :class="labelBase">
          Occupation
          <input v-model="form.occupation" :class="inputBase" type="text" />
        </label>
        <label :class="labelBase">
          Employer
          <input v-model="form.employerName" :class="inputBase" type="text" />
        </label>
        <label :class="[labelBase, 'md:col-span-2']">
          Monthly income
          <input v-model.number="form.monthlyIncome" :class="inputBase" type="number" min="0" step="0.01" />
        </label>
        <label :class="[labelBase, 'md:col-span-2']">
          Address
          <input v-model="form.address" :class="inputBase" type="text" autocomplete="street-address" />
        </label>
      </div>

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

      <p v-if="successMessage" class="rounded-xl bg-emerald-50 px-3 py-2 text-sm font-semibold text-emerald-700">
        {{ successMessage }}
      </p>

      <button type="submit" :class="primaryButton" :disabled="isLoading">
        <i :class="mode === 'login' ? 'pi pi-lock' : 'pi pi-user-plus'"></i>
        {{ isLoading ? 'Please wait...' : mode === 'login' ? 'Sign in' : 'Submit for approval' }}
      </button>
    </form>
  </section>
</template>
