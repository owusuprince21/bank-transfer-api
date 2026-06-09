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

const transactionTypeNames = {
  0: 'Deposit',
  1: 'Withdrawal',
  2: 'TransferSent',
  3: 'TransferReceived'
};

export function transactionTypeLabel(value) {
  return transactionTypeNames[value] ?? value;
}

export function isCreditTransaction(value) {
  const transactionType = transactionTypeLabel(value);
  return transactionType === 'Deposit' || transactionType === 'TransferReceived';
}

export function isDebitTransaction(value) {
  const transactionType = transactionTypeLabel(value);
  return transactionType === 'Withdrawal' || transactionType === 'TransferSent';
}
