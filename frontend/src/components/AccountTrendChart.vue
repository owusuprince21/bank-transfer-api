<script setup>
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { Chart } from 'chart.js/auto';
import { currency } from '../utils/formatters';
import { cardBase } from './ui';

const props = defineProps({
  transactions: {
    type: Array,
    required: true
  },
  selectedAccount: {
    type: Object,
    default: null
  }
});

const canvas = ref(null);
let chart;

const sortedTransactions = computed(() => {
  return [...props.transactions].sort((a, b) => new Date(a.createdAtUtc) - new Date(b.createdAtUtc));
});

const chartData = computed(() => {
  return {
    labels: sortedTransactions.value.map((transaction) => {
      return new Intl.DateTimeFormat('en-GH', {
        month: 'short',
        day: 'numeric'
      }).format(new Date(transaction.createdAtUtc));
    }),
    balances: sortedTransactions.value.map((transaction) => Number(transaction.balanceAfterTransaction))
  };
});

function renderChart() {
  if (!canvas.value || chartData.value.labels.length === 0) {
    chart?.destroy();
    chart = null;
    return;
  }

  chart?.destroy();
  chart = new Chart(canvas.value, {
    type: 'line',
    data: {
      labels: chartData.value.labels,
      datasets: [
        {
          label: 'Balance',
          data: chartData.value.balances,
          borderColor: '#059669',
          backgroundColor: 'rgba(5, 150, 105, 0.14)',
          borderWidth: 3,
          fill: true,
          pointBackgroundColor: '#047857',
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
          display: false
        },
        tooltip: {
          callbacks: {
            label(context) {
              return `Balance: ${currency(context.parsed.y)}`;
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
          grid: {
            color: 'rgba(148, 163, 184, 0.18)'
          },
          ticks: {
            color: '#64748b',
            callback(value) {
              return currency(value);
            }
          }
        }
      }
    }
  });
}

watch(chartData, renderChart, { deep: true, flush: 'post', immediate: true });

onBeforeUnmount(() => {
  chart?.destroy();
});
</script>

<template>
  <section :class="[cardBase, 'grid gap-4 p-4']">
    <div class="grid items-start gap-4 md:grid-cols-[1fr_auto]">
      <div>
        <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Balance Trend</p>
        <h2 class="text-lg font-black tracking-tight text-slate-950">
          {{ selectedAccount?.accountType ?? 'Account' }} performance
        </h2>
      </div>
      <div class="rounded-xl bg-emerald-50 px-3 py-2 text-right">
        <span class="text-[10px] font-bold uppercase text-emerald-700">Current</span>
        <strong class="block text-sm text-emerald-950">
          {{ currency(selectedAccount?.balance ?? 0) }}
        </strong>
      </div>
    </div>

    <div v-if="transactions.length" class="h-64">
      <canvas ref="canvas"></canvas>
    </div>

    <div v-else class="grid h-64 place-items-center rounded-2xl border border-dashed border-slate-200 bg-slate-50 text-xs font-semibold text-slate-500">
      Make a deposit, withdrawal, or transfer to build the account trend.
    </div>
  </section>
</template>
