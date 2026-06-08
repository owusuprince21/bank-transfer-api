const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5018';

async function request(path, options = {}) {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...options.headers
    },
    ...options
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({}));
    throw new Error(errorBody.message ?? `Request failed with status ${response.status}`);
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

export function login(email, password) {
  return request('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password })
  });
}

export function logout() {
  return request('/api/auth/logout', {
    method: 'POST'
  });
}

export function getCustomer(customerId) {
  return request(`/api/customers/${customerId}`);
}

export function createAccount(customerId, accountType, openingDeposit) {
  return request('/api/accounts', {
    method: 'POST',
    body: JSON.stringify({ customerId, accountType, openingDeposit })
  });
}

export function getAccountTransactions(accountId) {
  return request(`/api/accounts/${accountId}/transactions`);
}

export function getAccountByNumber(accountNumber) {
  return request(`/api/accounts/by-number/${encodeURIComponent(accountNumber)}`);
}

export function deposit(accountId, amount, description) {
  return request(`/api/accounts/${accountId}/deposits`, {
    method: 'POST',
    body: JSON.stringify({ amount, description })
  });
}

export function withdraw(accountId, amount, description) {
  return request(`/api/accounts/${accountId}/withdrawals`, {
    method: 'POST',
    body: JSON.stringify({ amount, description })
  });
}

export function transfer(sourceAccountId, destinationAccountId, amount, description) {
  return request(`/api/accounts/${sourceAccountId}/transfers`, {
    method: 'POST',
    body: JSON.stringify({ destinationAccountId, amount, description })
  });
}
