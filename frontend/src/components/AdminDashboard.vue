<script setup>
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { Chart } from 'chart.js/auto';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { toast } from 'vue-sonner';
import { approveCustomer, getAdminCustomers, getAdminKycDocumentFile, getAdminKycDocumentFileUrl, rejectCustomer } from '../api';
import { currency, formatDate } from '../utils/formatters';
import ConfirmationDialog from './ConfirmationDialog.vue';
import { cardBase, ghostButton, primaryButton, secondaryButton } from './ui';

const props = defineProps({
  admin: {
    type: Object,
    required: true
  }
});

defineEmits(['sign-out']);

const queryClient = useQueryClient();
const activeView = ref('overview');
const openActionMenuId = ref('');
const selectedCustomer = ref(null);
const previewDocument = ref(null);
const previewDocumentUrl = ref('');
const isPreviewLoading = ref(false);
const previewError = ref('');
const pendingApprovalCustomer = ref(null);
const pendingRejectionCustomer = ref(null);
const currentPage = ref(1);
const trendCanvas = ref(null);
let trendChart;
let activePreviewObjectUrl = '';
const pageSize = 10;

const customersQuery = useQuery({
  queryKey: ['admin-customers', 'all'],
  queryFn: () => getAdminCustomers()
});

const approveMutation = useMutation({
  mutationFn: approveCustomer,
  onSuccess: async () => {
    toast.success('Customer approved.');
    await queryClient.invalidateQueries({ queryKey: ['admin-customers'] });
  }
});

const rejectMutation = useMutation({
  mutationFn: (customerId) => rejectCustomer(customerId, 'Registration rejected after review.'),
  onSuccess: async () => {
    toast.success('Customer rejected.');
    await queryClient.invalidateQueries({ queryKey: ['admin-customers'] });
  }
});

const customers = computed(() => customersQuery.data.value ?? []);
const pendingCustomers = computed(() => customers.value.filter((customer) => customer.status === 'PendingApproval'));
const activeCustomers = computed(() => customers.value.filter((customer) => customer.status === 'Active'));
const rejectedCustomers = computed(() => customers.value.filter((customer) => customer.status === 'Rejected'));
const suspendedCustomers = computed(() => customers.value.filter((customer) => customer.status === 'Suspended'));
const isBusy = computed(() => customersQuery.isFetching.value || approveMutation.isPending.value || rejectMutation.isPending.value);
const previewContentType = computed(() => previewDocument.value?.contentType?.toLowerCase() ?? '');
const canPreviewInline = computed(() => previewContentType.value === 'application/pdf' || previewContentType.value.startsWith('image/'));

const initials = computed(() => {
  return props.admin.fullName
    ?.split(' ')
    .map((part) => part[0])
    .join('')
    .slice(0, 2)
    .toUpperCase() || 'SA';
});

const navItems = [
  { id: 'overview', label: 'Overview', icon: 'pi pi-chart-bar' },
  { id: 'approvals', label: ' Pending Approvals', icon: 'pi pi-check-circle' },
  { id: 'active', label: 'Customers', icon: 'pi pi-users' },
  { id: 'rejected', label: 'Rejected', icon: 'pi pi-ban' },
  { id: 'settings', label: 'Settings', icon: 'pi pi-cog' }
];

const metricCards = computed(() => [
  {
    label: 'Pending approvals',
    value: pendingCustomers.value.length,
    icon: 'pi pi-clock',
    accent: 'from-amber-500 to-orange-600'
  },
  {
    label: 'Active customers',
    value: activeCustomers.value.length,
    icon: 'pi pi-users',
    accent: 'from-emerald-500 to-teal-600'
  },
  {
    label: 'Rejected',
    value: rejectedCustomers.value.length,
    icon: 'pi pi-ban',
    accent: 'from-red-500 to-rose-600'
  },
  {
    label: 'Suspended',
    value: suspendedCustomers.value.length,
    icon: 'pi pi-lock',
    accent: 'from-slate-700 to-slate-950'
  }
]);

const visibleCustomers = computed(() => {
  if (activeView.value === 'approvals') {
    return pendingCustomers.value;
  }

  if (activeView.value === 'active') {
    return activeCustomers.value;
  }

  if (activeView.value === 'rejected') {
    return rejectedCustomers.value;
  }

  return customers.value;
});

const totalPages = computed(() => Math.max(1, Math.ceil(visibleCustomers.value.length / pageSize)));

const pagedCustomers = computed(() => {
  const start = (currentPage.value - 1) * pageSize;
  return visibleCustomers.value.slice(start, start + pageSize);
});

const pageStart = computed(() => visibleCustomers.value.length === 0 ? 0 : ((currentPage.value - 1) * pageSize) + 1);
const pageEnd = computed(() => Math.min(currentPage.value * pageSize, visibleCustomers.value.length));

const tableTitle = computed(() => {
  if (activeView.value === 'approvals') {
    return 'Pending customer approvals';
  }

  if (activeView.value === 'active') {
    return 'Active customers';
  }

  if (activeView.value === 'rejected') {
    return 'Rejected registrations';
  }

  return 'Recent customer activity';
});

const trendData = computed(() => {
  const days = Array.from({ length: 7 }, (_, index) => {
    const date = new Date();
    date.setHours(0, 0, 0, 0);
    date.setDate(date.getDate() - (6 - index));
    return date;
  });

  const dayKey = (value) => {
    const date = new Date(value);
    date.setHours(0, 0, 0, 0);
    return date.toISOString().slice(0, 10);
  };

  return {
    labels: days.map((date) => new Intl.DateTimeFormat('en-GH', {
      month: 'short',
      day: 'numeric'
    }).format(date)),
    registrations: days.map((date) => {
      const key = dayKey(date);
      return customers.value.filter((customer) => dayKey(customer.createdAtUtc) === key).length;
    }),
    approvals: days.map((date) => {
      const key = dayKey(date);
      return customers.value.filter((customer) => customer.approvedAtUtc && dayKey(customer.approvedAtUtc) === key).length;
    })
  };
});

function renderTrendChart() {
  if (!trendCanvas.value || activeView.value !== 'overview') {
    trendChart?.destroy();
    trendChart = null;
    return;
  }

  trendChart?.destroy();
  trendChart = new Chart(trendCanvas.value, {
    type: 'line',
    data: {
      labels: trendData.value.labels,
      datasets: [
        {
          label: 'Registrations',
          data: trendData.value.registrations,
          borderColor: '#059669',
          backgroundColor: 'rgba(5, 150, 105, 0.12)',
          borderWidth: 3,
          fill: true,
          pointBackgroundColor: '#047857',
          pointBorderColor: '#ffffff',
          pointBorderWidth: 2,
          pointRadius: 4,
          tension: 0.42
        },
        {
          label: 'Approvals',
          data: trendData.value.approvals,
          borderColor: '#f59e0b',
          backgroundColor: 'rgba(245, 158, 11, 0.08)',
          borderWidth: 3,
          fill: true,
          pointBackgroundColor: '#d97706',
          pointBorderColor: '#ffffff',
          pointBorderWidth: 2,
          pointRadius: 4,
          tension: 0.42
        }
      ]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      interaction: {
        intersect: false,
        mode: 'index'
      },
      plugins: {
        legend: {
          labels: {
            boxWidth: 10,
            color: '#475569',
            font: {
              size: 11,
              weight: 'bold'
            }
          }
        }
      },
      scales: {
        x: {
          grid: {
            display: false
          },
          ticks: {
            color: '#64748b'
          }
        },
        y: {
          beginAtZero: true,
          ticks: {
            color: '#64748b',
            precision: 0
          },
          grid: {
            color: 'rgba(148, 163, 184, 0.18)'
          }
        }
      }
    }
  });
}

watch([trendData, activeView], renderTrendChart, { deep: true, flush: 'post', immediate: true });

watch([visibleCustomers, activeView], () => {
  currentPage.value = Math.min(currentPage.value, totalPages.value);
  if (currentPage.value < 1) {
    currentPage.value = 1;
  }
});

onBeforeUnmount(() => {
  trendChart?.destroy();
  revokePreviewObjectUrl();
});

function toggleActionMenu(customerId) {
  openActionMenuId.value = openActionMenuId.value === customerId ? '' : customerId;
}

function viewCustomer(customer) {
  selectedCustomer.value = customer;
  openActionMenuId.value = '';
}

function approveSelectedCustomer(customerId) {
  openActionMenuId.value = '';
  const customer = customers.value.find((existingCustomer) => existingCustomer.id === customerId);
  if (!customer) {
    toast.error('Customer was not found.');
    return;
  }

  pendingApprovalCustomer.value = customer;
}

function rejectSelectedCustomer(customerId) {
  openActionMenuId.value = '';
  const customer = customers.value.find((existingCustomer) => existingCustomer.id === customerId);
  if (!customer) {
    toast.error('Customer was not found.');
    return;
  }

  pendingRejectionCustomer.value = customer;
}

function revokePreviewObjectUrl() {
  if (activePreviewObjectUrl) {
    URL.revokeObjectURL(activePreviewObjectUrl);
    activePreviewObjectUrl = '';
  }

  previewDocumentUrl.value = '';
}

async function openKycDocument(document) {
  revokePreviewObjectUrl();
  previewDocument.value = document;
  previewError.value = '';
  isPreviewLoading.value = true;

  try {
    const blob = await getAdminKycDocumentFile(document.id);

    if (previewDocument.value?.id !== document.id) {
      return;
    }

    const previewBlob = blob.type
      ? blob
      : new Blob([blob], { type: document.contentType || 'application/octet-stream' });
    activePreviewObjectUrl = URL.createObjectURL(previewBlob);
    previewDocumentUrl.value = activePreviewObjectUrl;
  } catch (error) {
    previewError.value = error.message;
    toast.error(error.message);
  } finally {
    if (previewDocument.value?.id === document.id) {
      isPreviewLoading.value = false;
    }
  }
}

function closeKycDocumentPreview() {
  previewDocument.value = null;
  previewError.value = '';
  isPreviewLoading.value = false;
  revokePreviewObjectUrl();
}

function downloadKycDocument(documentId) {
  window.open(getAdminKycDocumentFileUrl(documentId, { download: true }), '_blank', 'noopener,noreferrer');
}

async function confirmApproval() {
  if (!pendingApprovalCustomer.value) {
    return;
  }

  const customerId = pendingApprovalCustomer.value.id;
  selectedCustomer.value = null;

  try {
    await approveMutation.mutateAsync(customerId);
  } finally {
    pendingApprovalCustomer.value = null;
  }
}

function cancelApproval() {
  pendingApprovalCustomer.value = null;
}

async function confirmRejection() {
  if (!pendingRejectionCustomer.value) {
    return;
  }

  const customerId = pendingRejectionCustomer.value.id;
  selectedCustomer.value = null;

  try {
    await rejectMutation.mutateAsync(customerId);
  } finally {
    pendingRejectionCustomer.value = null;
  }
}

function cancelRejection() {
  pendingRejectionCustomer.value = null;
}

function previousPage() {
  currentPage.value = Math.max(1, currentPage.value - 1);
}

function nextPage() {
  currentPage.value = Math.min(totalPages.value, currentPage.value + 1);
}
</script>

<template>
  <section class="mx-auto grid w-full max-w-[1520px] items-start gap-5 lg:block lg:pl-80">
    <aside class="lg:fixed lg:inset-y-4 lg:left-4 lg:z-30 lg:w-72">
      <div class="flex max-h-full flex-col overflow-hidden rounded-[1.5rem] border border-slate-200/80 bg-white/95 p-4 shadow-sm backdrop-blur">
        <div class="min-h-0 flex-1 overflow-y-auto pr-1">
          <section class="grid justify-items-center gap-2 border-b border-slate-100 pb-4 text-center">
            <div class="grid size-20 place-items-center rounded-full bg-gradient-to-br from-slate-950 to-emerald-800 text-2xl font-black text-white shadow-lg ring-4 ring-emerald-50">
              {{ initials }}
            </div>
            <div class="grid min-w-0 gap-1">
              <p class="truncate text-sm font-black tracking-tight text-slate-950">{{ admin.fullName }}</p>
              <p class="truncate text-xs font-semibold text-slate-500">{{ admin.email }}</p>
              <p class="text-[11px] font-bold uppercase tracking-wide text-emerald-700">System administrator</p>
            </div>
          </section>

          <nav class="mt-4 grid gap-1.5">
            <button
              v-for="item in navItems"
              :key="item.id"
              type="button"
              :class="[
                ghostButton,
                'h-10 justify-start rounded-xl px-3 text-xs',
                activeView === item.id ? 'bg-slate-950 text-white shadow-lg shadow-slate-200 hover:bg-slate-900 hover:text-white' : ''
              ]"
              @click="activeView = item.id"
            >
              <span
                class="grid size-7 place-items-center rounded-lg text-xs"
                :class="activeView === item.id ? 'bg-white/10' : 'bg-slate-100 text-slate-500'"
              >
                <i :class="item.icon"></i>
              </span>
              <span>{{ item.label }}</span>
            </button>
          </nav>
        </div>

        <div class="border-t border-slate-100 pt-3">
          <button
            type="button"
            :class="[ghostButton, 'h-10 w-full justify-start rounded-xl px-3 text-xs text-red-600 hover:bg-red-50 hover:text-red-700']"
            @click="$emit('sign-out')"
          >
            <span class="grid size-7 place-items-center rounded-lg bg-red-50 text-xs text-red-600">
              <i class="pi pi-sign-out"></i>
            </span>
            Sign out
          </button>
        </div>
      </div>
    </aside>

    <main class="grid min-w-0 gap-5">
      <header :class="[cardBase, 'grid gap-4 p-5 md:grid-cols-[1fr_auto] md:items-center']">
        <div class="grid gap-2">
          <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Admin console</p>
          <h1 class="text-2xl font-black tracking-tight text-slate-950 md:text-4xl">Bank operations workspace</h1>
          <p class="max-w-2xl text-xs font-medium leading-5 text-slate-500">
            Review onboarding, monitor customer status, and control approval workflows from one place.
          </p>
        </div>
        <button type="button" :class="secondaryButton" :disabled="isBusy" @click="customersQuery.refetch">
          <i class="pi pi-refresh"></i>
          Refresh
        </button>
      </header>

      <section v-if="activeView === 'overview'" class="grid gap-5">
        <section class="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          <article
            v-for="card in metricCards"
            :key="card.label"
            class="group rounded-2xl border border-slate-200/80 bg-white/90 p-4 shadow-sm backdrop-blur transition duration-200 hover:-translate-y-0.5 hover:shadow-lg"
          >
            <div class="flex items-start justify-between gap-4">
              <div class="grid gap-2">
                <span class="text-xs font-bold text-slate-500">{{ card.label }}</span>
                <strong class="text-2xl font-black tracking-tight text-slate-950">{{ card.value }}</strong>
              </div>
              <span class="grid size-10 place-items-center rounded-xl bg-gradient-to-br text-sm text-white shadow-md transition group-hover:scale-105" :class="card.accent">
                <i :class="card.icon"></i>
              </span>
            </div>
          </article>
        </section>

        <section class="grid gap-5 xl:grid-cols-[minmax(0,1.35fr)_minmax(340px,0.65fr)]">
          <section :class="[cardBase, 'grid gap-4 p-4']">
            <div class="grid items-start gap-3 md:grid-cols-[1fr_auto]">
              <div>
                <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Trend</p>
                <h2 class="text-lg font-black tracking-tight text-slate-950">Registration and approval activity</h2>
              </div>
              <span class="rounded-xl bg-slate-100 px-3 py-2 text-[10px] font-black uppercase text-slate-500">Last 7 days</span>
            </div>
            <div class="h-72">
              <canvas ref="trendCanvas"></canvas>
            </div>
          </section>

          <section :class="[cardBase, 'grid content-start gap-4 p-4']">
            <div>
              <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Queue health</p>
              <h2 class="text-lg font-black tracking-tight text-slate-950">Approval workload</h2>
            </div>
            <div class="grid gap-3">
              <div class="rounded-2xl bg-amber-50 p-4">
                <p class="text-[10px] font-black uppercase tracking-wide text-amber-700">Next action</p>
                <p class="mt-2 text-sm font-bold text-amber-950">{{ pendingCustomers.length }} pending registrations need review.</p>
              </div>
              <div class="rounded-2xl bg-emerald-50 p-4">
                <p class="text-[10px] font-black uppercase tracking-wide text-emerald-700">Approval rate</p>
                <p class="mt-2 text-sm font-bold text-emerald-950">
                  {{ customers.length ? Math.round((activeCustomers.length / customers.length) * 100) : 0 }}% active customers.
                </p>
              </div>
              <div class="rounded-2xl bg-slate-50 p-4">
                <p class="text-[10px] font-black uppercase tracking-wide text-slate-500">KYC coverage</p>
                <p class="mt-2 text-sm font-bold text-slate-950">
                  {{ customers.filter((customer) => customer.kycDocumentCount > 0).length }} customers uploaded documents.
                </p>
              </div>
            </div>
          </section>
        </section>
      </section>

      <section v-else-if="activeView === 'settings'" :class="[cardBase, 'grid gap-5 p-5']">
        <div>
          <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Admin profile</p>
          <h2 class="text-lg font-black tracking-tight text-slate-950">Console settings</h2>
        </div>
        <dl class="grid gap-3 md:grid-cols-2">
          <div class="rounded-2xl bg-slate-50 p-4">
            <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Name</dt>
            <dd class="mt-1 text-sm font-bold text-slate-900">{{ admin.fullName }}</dd>
          </div>
          <div class="rounded-2xl bg-slate-50 p-4">
            <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Email</dt>
            <dd class="mt-1 break-words text-sm font-bold text-slate-900">{{ admin.email }}</dd>
          </div>
          <div class="rounded-2xl bg-slate-50 p-4 md:col-span-2">
            <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Role</dt>
            <dd class="mt-1 text-sm font-bold text-slate-900">System administrator</dd>
          </div>
        </dl>
      </section>

      <section v-else :class="[cardBase, 'grid gap-4 p-4']">
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div>
            <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Customer management</p>
            <h2 class="text-lg font-black tracking-tight text-slate-950">{{ tableTitle }}</h2>
          </div>
          <span class="rounded-xl bg-slate-100 px-3 py-2 text-xs font-bold text-slate-600">{{ visibleCustomers.length }} records</span>
        </div>

        <div class="overflow-x-visible rounded-2xl border border-slate-200">
          <table class="w-full min-w-[980px] text-left text-xs">
            <thead class="bg-slate-50 font-black uppercase tracking-wide text-slate-500">
              <tr>
                <th class="px-3 py-2.5">Customer</th>
                <th class="px-3 py-2.5">Status</th>
                <th class="px-3 py-2.5">KYC</th>
                <th class="px-3 py-2.5">Income</th>
                <th class="px-3 py-2.5">Accounts</th>
                <th class="px-3 py-2.5">Created</th>
                <th class="px-3 py-2.5">Action</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-200 bg-white">
              <tr v-for="customer in pagedCustomers" :key="customer.id" class="hover:bg-slate-50/80">
                <td class="px-3 py-3">
                  <strong class="block text-sm text-slate-950">{{ customer.firstName }} {{ customer.lastName }}</strong>
                  <span class="text-slate-500">{{ customer.email }}</span>
                </td>
                <td class="px-3 py-3">
                  <span class="rounded-full px-2 py-1 text-[11px] font-black" :class="customer.status === 'Active' ? 'bg-emerald-50 text-emerald-700' : customer.status === 'Rejected' ? 'bg-red-50 text-red-700' : 'bg-amber-50 text-amber-700'">
                    {{ customer.status }}
                  </span>
                </td>
                <td class="px-3 py-3 font-semibold text-slate-700">
                  {{ customer.nationalIdNumber || 'Missing' }} · {{ customer.kycDocumentCount }} docs
                </td>
                <td class="px-3 py-3 font-semibold text-slate-700">{{ customer.monthlyIncome ?? 0 }}</td>
                <td class="px-3 py-3 font-semibold text-slate-700">{{ customer.accountCount }}</td>
                <td class="px-3 py-3 font-semibold text-slate-500">{{ formatDate(customer.createdAtUtc) }}</td>
                <td class="relative px-3 py-3">
                  <button
                    type="button"
                    class="grid size-9 place-items-center rounded-xl border border-slate-200 bg-white text-slate-600 shadow-sm transition hover:border-slate-300 hover:bg-slate-50 hover:text-slate-950 disabled:opacity-50"
                    :disabled="isBusy"
                    :aria-expanded="openActionMenuId === customer.id"
                    @click="toggleActionMenu(customer.id)"
                  >
                    <i class="pi pi-ellipsis-v"></i>
                  </button>

                  <div
                    v-if="openActionMenuId === customer.id"
                    class="absolute right-3 top-12 z-20 grid min-w-44 gap-1 rounded-2xl border border-slate-200 bg-white p-2 shadow-2xl"
                  >
                    <button
                      type="button"
                      class="flex min-h-9 items-center gap-2 rounded-xl px-3 text-left text-xs font-bold text-slate-700 hover:bg-slate-50 hover:text-slate-950"
                      @click="viewCustomer(customer)"
                    >
                      <i class="pi pi-eye"></i>
                      View customer
                    </button>
                    <button
                      v-if="customer.status === 'PendingApproval'"
                      type="button"
                      class="flex min-h-9 items-center gap-2 rounded-xl px-3 text-left text-xs font-bold text-emerald-700 hover:bg-emerald-50"
                      @click="approveSelectedCustomer(customer.id)"
                    >
                      <i class="pi pi-check"></i>
                      Approve
                    </button>
                    <button
                      v-if="customer.status === 'PendingApproval'"
                      type="button"
                      class="flex min-h-9 items-center gap-2 rounded-xl px-3 text-left text-xs font-bold text-red-600 hover:bg-red-50"
                      @click="rejectSelectedCustomer(customer.id)"
                    >
                      <i class="pi pi-times"></i>
                      Reject
                    </button>
                    <span v-if="customer.status !== 'PendingApproval'" class="px-3 py-2 text-xs font-bold text-slate-400">
                      No pending review
                    </span>
                  </div>
                </td>
              </tr>
              <tr v-if="visibleCustomers.length === 0">
                <td colspan="7" class="px-3 py-10 text-center font-semibold text-slate-500">No customers in this view.</td>
              </tr>
            </tbody>
          </table>
        </div>

        <div v-if="visibleCustomers.length > pageSize" class="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-slate-50 px-3 py-2">
          <p class="text-xs font-bold text-slate-500">
            Showing {{ pageStart }}-{{ pageEnd }} of {{ visibleCustomers.length }}
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
      </section>
    </main>

    <Teleport to="body">
      <div
        v-if="selectedCustomer"
        class="fixed inset-0 z-50 grid place-items-center bg-slate-950/45 p-4 backdrop-blur-sm"
        role="dialog"
        aria-modal="true"
      >
        <section class="grid max-h-[90vh] w-full max-w-4xl grid-rows-[auto_minmax(0,1fr)_auto] overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl">
          <header class="flex items-start justify-between gap-4 border-b border-slate-100 p-5">
            <div>
              <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Customer review</p>
              <h2 class="text-xl font-black tracking-tight text-slate-950">
                {{ selectedCustomer.firstName }} {{ selectedCustomer.lastName }}
              </h2>
              <p class="mt-1 text-xs font-semibold text-slate-500">{{ selectedCustomer.email }}</p>
            </div>
            <button
              type="button"
              class="grid size-9 place-items-center rounded-xl bg-slate-100 text-slate-500 transition hover:bg-slate-200 hover:text-slate-950"
              @click="selectedCustomer = null"
            >
              <i class="pi pi-times"></i>
            </button>
          </header>

          <div class="grid min-h-0 gap-4 overflow-y-auto p-5">
            <div class="grid gap-3 md:grid-cols-3">
              <div class="rounded-2xl bg-slate-50 p-4">
                <p class="text-[10px] font-black uppercase tracking-wide text-slate-400">Status</p>
                <p class="mt-1 text-sm font-bold text-slate-950">{{ selectedCustomer.status }}</p>
              </div>
              <div class="rounded-2xl bg-slate-50 p-4">
                <p class="text-[10px] font-black uppercase tracking-wide text-slate-400">Accounts</p>
                <p class="mt-1 text-sm font-bold text-slate-950">{{ selectedCustomer.accountCount }}</p>
              </div>
              <div class="rounded-2xl bg-slate-50 p-4">
                <p class="text-[10px] font-black uppercase tracking-wide text-slate-400">Documents</p>
                <p class="mt-1 text-sm font-bold text-slate-950">{{ selectedCustomer.kycDocumentCount }}</p>
              </div>
            </div>

            <section class="grid gap-3">
              <div>
                <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Registration details</p>
                <h3 class="text-sm font-black text-slate-950">Identity and contact</h3>
              </div>
              <dl class="grid gap-3 md:grid-cols-2">
                <div class="rounded-2xl bg-slate-50 p-4">
                  <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">First name</dt>
                  <dd class="mt-1 text-sm font-bold text-slate-900">{{ selectedCustomer.firstName }}</dd>
                </div>
                <div class="rounded-2xl bg-slate-50 p-4">
                  <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Last name</dt>
                  <dd class="mt-1 text-sm font-bold text-slate-900">{{ selectedCustomer.lastName }}</dd>
                </div>
                <div class="rounded-2xl bg-slate-50 p-4">
                  <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Email</dt>
                  <dd class="mt-1 break-words text-sm font-bold text-slate-900">{{ selectedCustomer.email }}</dd>
                </div>
                <div class="rounded-2xl bg-slate-50 p-4">
                  <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Phone</dt>
                  <dd class="mt-1 text-sm font-bold text-slate-900">{{ selectedCustomer.phoneNumber || 'Not provided' }}</dd>
                </div>
                <div class="rounded-2xl bg-slate-50 p-4">
                  <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Date of birth</dt>
                  <dd class="mt-1 text-sm font-bold text-slate-900">{{ selectedCustomer.dateOfBirth || 'Not provided' }}</dd>
                </div>
                <div class="rounded-2xl bg-slate-50 p-4">
                  <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">National ID</dt>
                  <dd class="mt-1 text-sm font-bold text-slate-900">{{ selectedCustomer.nationalIdNumber || 'Missing' }}</dd>
                </div>
                <div class="rounded-2xl bg-slate-50 p-4 md:col-span-2">
                  <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Address</dt>
                  <dd class="mt-1 text-sm font-bold text-slate-900">{{ selectedCustomer.address || 'Not provided' }}</dd>
                </div>
              </dl>
            </section>

            <section class="grid gap-3">
              <div>
                <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Employment</p>
                <h3 class="text-sm font-black text-slate-950">Work and income profile</h3>
              </div>
              <dl class="grid gap-3 md:grid-cols-3">
                <div class="rounded-2xl bg-slate-50 p-4">
                  <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Occupation</dt>
                  <dd class="mt-1 text-sm font-bold text-slate-900">{{ selectedCustomer.occupation || 'Not provided' }}</dd>
                </div>
                <div class="rounded-2xl bg-slate-50 p-4">
                  <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Employer</dt>
                  <dd class="mt-1 text-sm font-bold text-slate-900">{{ selectedCustomer.employerName || 'Not provided' }}</dd>
                </div>
                <div class="rounded-2xl bg-slate-50 p-4">
                  <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Monthly income</dt>
                  <dd class="mt-1 text-sm font-bold text-slate-900">{{ selectedCustomer.monthlyIncome ?? 0 }}</dd>
                </div>
              </dl>
            </section>

            <section class="grid gap-3">
              <div>
                <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Accounts</p>
                <h3 class="text-sm font-black text-slate-950">Customer account numbers</h3>
              </div>
              <div v-if="!selectedCustomer.accounts?.length" class="grid min-h-24 place-items-center rounded-2xl border border-dashed border-slate-200 bg-slate-50 text-xs font-semibold text-slate-500">
                No accounts created yet.
              </div>
              <div v-else class="overflow-x-auto rounded-2xl border border-slate-200">
                <table class="w-full min-w-[760px] text-left text-xs">
                  <thead class="bg-slate-50 font-black uppercase tracking-wide text-slate-500">
                    <tr>
                      <th class="px-3 py-2.5">Account number</th>
                      <th class="px-3 py-2.5">Type</th>
                      <th class="px-3 py-2.5">Currency</th>
                      <th class="px-3 py-2.5">Balance</th>
                      <th class="px-3 py-2.5">Status</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-200 bg-white">
                    <tr v-for="account in selectedCustomer.accounts" :key="account.id">
                      <td class="px-3 py-3 font-black text-slate-950">{{ account.accountNumber }}</td>
                      <td class="px-3 py-3 font-semibold text-slate-700">{{ account.accountType }}</td>
                      <td class="px-3 py-3 font-semibold text-slate-700">{{ account.currency }}</td>
                      <td class="px-3 py-3 font-semibold text-slate-700">{{ currency(account.balance) }}</td>
                      <td class="px-3 py-3">
                        <span class="rounded-full px-2 py-1 text-[11px] font-black" :class="account.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-700'">
                          {{ account.isActive ? 'Active' : 'Inactive' }}
                        </span>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </section>

            <section class="grid gap-3">
              <div>
                <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">KYC documents</p>
                <h3 class="text-sm font-black text-slate-950">Attached verification files</h3>
              </div>
              <div v-if="!selectedCustomer.kycDocuments?.length" class="grid min-h-24 place-items-center rounded-2xl border border-dashed border-slate-200 bg-slate-50 text-xs font-semibold text-slate-500">
                No documents attached.
              </div>
              <div v-else class="grid gap-2">
                <article
                  v-for="document in selectedCustomer.kycDocuments"
                  :key="document.id"
                  class="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-slate-200 bg-slate-50 p-3"
                >
                  <div class="min-w-0">
                    <strong class="block text-sm text-slate-950">{{ document.documentType }}</strong>
                    <p class="break-all text-xs font-semibold text-slate-500">{{ document.originalFileName }}</p>
                    <p class="mt-1 text-[11px] font-bold text-slate-400">{{ document.contentType }} · {{ formatDate(document.uploadedAtUtc) }}</p>
                  </div>
                  <button type="button" :class="secondaryButton" @click="openKycDocument(document)">
                    <i class="pi pi-eye"></i>
                    Preview
                  </button>
                </article>
              </div>
            </section>

            <section class="grid gap-3">
              <div>
                <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Review state</p>
                <h3 class="text-sm font-black text-slate-950">Approval and system metadata</h3>
              </div>
              <dl class="grid gap-3 md:grid-cols-2">
                <div class="rounded-2xl bg-slate-50 p-4">
                  <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Created</dt>
                  <dd class="mt-1 text-sm font-bold text-slate-900">{{ formatDate(selectedCustomer.createdAtUtc) }}</dd>
                </div>
                <div class="rounded-2xl bg-slate-50 p-4">
                  <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Approved at</dt>
                  <dd class="mt-1 text-sm font-bold text-slate-900">
                    {{ selectedCustomer.approvedAtUtc ? formatDate(selectedCustomer.approvedAtUtc) : 'Not approved' }}
                  </dd>
                </div>
                <div class="rounded-2xl bg-slate-50 p-4 md:col-span-2">
                  <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Rejection reason</dt>
                  <dd class="mt-1 text-sm font-bold text-slate-900">{{ selectedCustomer.rejectionReason || 'None' }}</dd>
                </div>
                <div class="rounded-2xl bg-slate-50 p-4 md:col-span-2">
                  <dt class="text-[10px] font-black uppercase tracking-wide text-slate-400">Customer ID</dt>
                  <dd class="mt-1 break-all text-sm font-bold text-slate-900">{{ selectedCustomer.id }}</dd>
                </div>
              </dl>
            </section>
          </div>

          <footer class="flex justify-end gap-2 border-t border-slate-100 p-4">
            <button type="button" :class="secondaryButton" @click="selectedCustomer = null">Close</button>
            <button
              v-if="selectedCustomer.status === 'PendingApproval'"
              type="button"
              class="inline-flex min-h-9 items-center justify-center gap-2 rounded-xl bg-red-600 px-3 py-2 text-xs font-bold text-white shadow-sm transition hover:bg-red-700 disabled:pointer-events-none disabled:opacity-50"
              :disabled="isBusy"
              @click="rejectSelectedCustomer(selectedCustomer.id)"
            >
              <i class="pi pi-times"></i>
              Reject
            </button>
            <button
              v-if="selectedCustomer.status === 'PendingApproval'"
              type="button"
              :class="primaryButton"
              :disabled="isBusy"
              @click="approveSelectedCustomer(selectedCustomer.id)"
            >
              <i class="pi pi-check"></i>
              Approve
            </button>
          </footer>
        </section>
      </div>
    </Teleport>

    <Teleport to="body">
      <div
        v-if="previewDocument"
        class="fixed inset-0 z-[55] grid place-items-center bg-slate-950/55 p-4 backdrop-blur-sm"
        role="dialog"
        aria-modal="true"
      >
        <section class="grid max-h-[92vh] w-full max-w-5xl grid-rows-[auto_minmax(0,1fr)_auto] overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl">
          <header class="flex items-start justify-between gap-4 border-b border-slate-100 p-5">
            <div class="min-w-0">
              <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Attachment preview</p>
              <h2 class="truncate text-xl font-black tracking-tight text-slate-950">{{ previewDocument.documentType }}</h2>
              <p class="mt-1 break-all text-xs font-semibold text-slate-500">{{ previewDocument.originalFileName }}</p>
            </div>
            <button
              type="button"
              class="grid size-9 place-items-center rounded-xl bg-slate-100 text-slate-500 transition hover:bg-slate-200 hover:text-slate-950"
              @click="closeKycDocumentPreview"
            >
              <i class="pi pi-times"></i>
            </button>
          </header>

          <div class="min-h-0 bg-slate-100 p-3">
            <div
              v-if="isPreviewLoading"
              class="grid h-full min-h-[70vh] place-items-center rounded-xl border border-slate-200 bg-white p-6 text-center"
            >
              <div>
                <i class="pi pi-spin pi-spinner text-3xl text-emerald-600"></i>
                <p class="mt-3 text-sm font-bold text-slate-700">Loading attachment preview</p>
              </div>
            </div>
            <div
              v-else-if="previewError"
              class="grid h-full min-h-[70vh] place-items-center rounded-xl border border-slate-200 bg-white p-6 text-center"
            >
              <div class="max-w-md">
                <i class="pi pi-exclamation-triangle text-3xl text-amber-500"></i>
                <p class="mt-3 text-sm font-bold text-slate-800">{{ previewError }}</p>
                <p class="mt-2 text-xs font-semibold text-slate-500">Use download if the browser cannot render this attachment inline.</p>
              </div>
            </div>
            <iframe
              v-else-if="previewDocumentUrl && previewContentType === 'application/pdf'"
              class="h-full min-h-[70vh] w-full rounded-xl border border-slate-200 bg-white"
              :src="previewDocumentUrl"
              title="KYC document preview"
            ></iframe>
            <div v-else-if="previewDocumentUrl && previewContentType.startsWith('image/')" class="grid h-full min-h-[70vh] place-items-center overflow-auto rounded-xl border border-slate-200 bg-white p-4">
              <img
                class="max-h-full max-w-full object-contain"
                :src="previewDocumentUrl"
                alt="KYC document preview"
              />
            </div>
            <div
              v-else-if="previewDocumentUrl && !canPreviewInline"
              class="grid h-full min-h-[70vh] place-items-center rounded-xl border border-slate-200 bg-white p-6 text-center"
            >
              <div class="max-w-md">
                <i class="pi pi-file text-3xl text-slate-500"></i>
                <p class="mt-3 text-sm font-bold text-slate-800">This file type cannot be previewed inline.</p>
                <p class="mt-2 text-xs font-semibold text-slate-500">Download the attachment to view it on your device.</p>
              </div>
            </div>
          </div>

          <footer class="flex justify-end gap-2 border-t border-slate-100 p-4">
            <button type="button" :class="secondaryButton" @click="closeKycDocumentPreview">Close</button>
            <button type="button" :class="primaryButton" @click="downloadKycDocument(previewDocument.id)">
              <i class="pi pi-download"></i>
              Download attachment
            </button>
          </footer>
        </section>
      </div>
    </Teleport>

    <ConfirmationDialog
      :open="Boolean(pendingApprovalCustomer)"
      title="Approve customer?"
      description="This will activate the customer and create a default savings account if they do not already have one."
      confirm-label="Approve customer"
      :details="[
        { label: 'Customer', value: pendingApprovalCustomer ? `${pendingApprovalCustomer.firstName} ${pendingApprovalCustomer.lastName}` : '' },
        { label: 'Email', value: pendingApprovalCustomer?.email ?? '' },
        { label: 'Status', value: pendingApprovalCustomer?.status ?? '' }
      ]"
      :is-loading="approveMutation.isPending.value"
      @cancel="cancelApproval"
      @confirm="confirmApproval"
    />

    <ConfirmationDialog
      :open="Boolean(pendingRejectionCustomer)"
      title="Reject customer?"
      description="This will mark the registration as rejected and prevent the customer from signing in."
      confirm-label="Reject customer"
      tone="danger"
      :details="[
        { label: 'Customer', value: pendingRejectionCustomer ? `${pendingRejectionCustomer.firstName} ${pendingRejectionCustomer.lastName}` : '' },
        { label: 'Email', value: pendingRejectionCustomer?.email ?? '' },
        { label: 'Status', value: pendingRejectionCustomer?.status ?? '' }
      ]"
      :is-loading="rejectMutation.isPending.value"
      @cancel="cancelRejection"
      @confirm="confirmRejection"
    />
  </section>
</template>
