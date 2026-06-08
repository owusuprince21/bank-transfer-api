export function currency(value) {
  return new Intl.NumberFormat('en-GH', {
    style: 'currency',
    currency: 'GHS'
  }).format(Number(value));
}

export function formatDate(value) {
  return new Intl.DateTimeFormat('en-GH', {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(new Date(value));
}
