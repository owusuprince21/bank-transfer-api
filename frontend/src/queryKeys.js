export const queryKeys = {
  customer: (customerId) => ['customer', customerId],
  accountTransactions: (accountId) => ['account-transactions', accountId],
  financialControls: () => ['financial-controls'],
  kycDocuments: () => ['kyc-documents'],
  notifications: () => ['notifications']
};
