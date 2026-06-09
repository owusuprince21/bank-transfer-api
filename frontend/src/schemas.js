import { z } from 'zod';

export const loginSchema = z.object({
  email: z.email('Enter a valid email address.'),
  password: z.string().min(1, 'Password is required.')
});

export const registerSchema = z.object({
  firstName: z.string().min(1, 'First name is required.').max(100),
  lastName: z.string().min(1, 'Last name is required.').max(100),
  email: z.email('Enter a valid email address.'),
  password: z.string().min(8, 'Password must be at least 8 characters.').max(100),
  phoneNumber: z.string().max(30).optional(),
  dateOfBirth: z.string().min(1, 'Date of birth is required.'),
  address: z.string().max(500).optional(),
  nationalIdNumber: z.string().min(1, 'National ID number is required.').max(80),
  occupation: z.string().max(120).optional(),
  employerName: z.string().max(160).optional(),
  monthlyIncome: z.number().finite().min(0).optional(),
  requestedAccountType: z.string().max(40).optional()
});

export const transactionSchema = z.object({
  type: z.enum(['deposit', 'withdrawal']),
  amount: z
    .number()
    .finite('Enter a valid amount.')
    .positive('Amount must be greater than zero.'),
  description: z.string().max(500, 'Description must be 500 characters or fewer.').optional()
});

export const createAccountSchema = z.object({
  accountType: z.string().min(1, 'Choose an account type.').max(40),
  currency: z.string().length(3, 'Currency must be a 3-letter code.'),
  openingDeposit: z
    .number()
    .finite('Enter a valid opening deposit.')
    .min(0, 'Opening deposit cannot be negative.'),
  dailyTransferLimit: z.number().finite('Enter a valid transfer limit.').min(0),
  dailyWithdrawalLimit: z.number().finite('Enter a valid withdrawal limit.').min(0),
  allowInternationalTransfers: z.boolean()
});

export const transferSchema = z.object({
  destinationAccountId: z.string().min(1, 'Choose a recipient account.'),
  accountNumber: z.string().min(1, 'Enter a recipient account number.'),
  amount: z
    .number()
    .finite('Enter a valid amount.')
    .positive('Amount must be greater than zero.'),
  description: z.string().max(500, 'Description must be 500 characters or fewer.').optional()
});

export const spendingControlSchema = z.object({
  monthlySpendLimit: z.number().finite('Enter a valid monthly limit.').min(0),
  singleTransactionLimit: z.number().finite('Enter a valid transaction limit.').min(0),
  savingsTarget: z.number().finite('Enter a valid savings target.').min(0),
  blockTransfersWhenLimitReached: z.boolean()
});

export function getFirstZodError(error) {
  return error.issues?.[0]?.message ?? 'Please check the form and try again.';
}
