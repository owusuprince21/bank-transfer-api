import { z } from 'zod';

export const loginSchema = z.object({
  email: z.email('Enter a valid email address.'),
  password: z.string().min(1, 'Password is required.')
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
  openingDeposit: z
    .number()
    .finite('Enter a valid opening deposit.')
    .min(0, 'Opening deposit cannot be negative.')
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

export function getFirstZodError(error) {
  return error.issues?.[0]?.message ?? 'Please check the form and try again.';
}
