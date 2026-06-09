export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5018';

async function request(path, options = {}) {
  const isFormData = options.body instanceof FormData;
  const response = await fetch(`${API_BASE_URL}${path}`, {
    credentials: 'include',
    headers: {
      ...(isFormData ? {} : { 'Content-Type': 'application/json' }),
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

export function getCurrentSession(options = {}) {
  return request('/api/auth/me', options);
}

export function registerCustomer(requestBody) {
  return request('/api/customers/register', {
    method: 'POST',
    body: JSON.stringify(requestBody)
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

export function createAccount(customerId, requestBody) {
  return request('/api/accounts', {
    method: 'POST',
    body: JSON.stringify({ customerId, ...requestBody })
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

export function getAdminCustomers(status) {
  const query = status ? `?status=${encodeURIComponent(status)}` : '';
  return request(`/api/admin/customers${query}`);
}

export function approveCustomer(customerId) {
  return request(`/api/admin/customers/${customerId}/approve`, {
    method: 'POST'
  });
}

export function rejectCustomer(customerId, reason) {
  return request(`/api/admin/customers/${customerId}/reject`, {
    method: 'POST',
    body: JSON.stringify({ reason })
  });
}

export function getFinancialControls() {
  return request('/api/financial-controls/me');
}

export function updateFinancialControls(requestBody) {
  return request('/api/financial-controls/me', {
    method: 'PUT',
    body: JSON.stringify(requestBody)
  });
}

export function deleteFinancialControls() {
  return request('/api/financial-controls/me', {
    method: 'DELETE'
  });
}

export function getNotifications() {
  return request('/api/notifications/me');
}

export function markNotificationAsRead(notificationId) {
  return request(`/api/notifications/${notificationId}/read`, {
    method: 'POST'
  });
}

export function markAllNotificationsAsRead() {
  return request('/api/notifications/me/read-all', {
    method: 'POST'
  });
}

export function deleteNotification(notificationId) {
  return request(`/api/notifications/${notificationId}`, {
    method: 'DELETE'
  });
}

export function getKycDocuments() {
  return request('/api/kyc-documents/me');
}

export function uploadKycDocument(documentType, file) {
  const formData = new FormData();
  formData.append('documentType', documentType);
  formData.append('file', file);

  return request('/api/kyc-documents', {
    method: 'POST',
    body: formData
  });
}

export function getKycDocumentFileUrl(documentId, options = {}) {
  const query = options.download ? '?download=true' : '';
  return `${API_BASE_URL}/api/kyc-documents/${documentId}/file${query}`;
}

export async function getKycDocumentFile(documentId) {
  const response = await fetch(getKycDocumentFileUrl(documentId), {
    credentials: 'include'
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({}));
    throw new Error(errorBody.message ?? `Document preview failed with status ${response.status}`);
  }

  return response.blob();
}

export function getAdminKycDocumentFileUrl(documentId, options = {}) {
  const query = options.download ? '?download=true' : '';
  return `${API_BASE_URL}/api/admin/kyc-documents/${documentId}/file${query}`;
}

export async function getAdminKycDocumentFile(documentId) {
  const response = await fetch(getAdminKycDocumentFileUrl(documentId), {
    credentials: 'include'
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({}));
    throw new Error(errorBody.message ?? `Document preview failed with status ${response.status}`);
  }

  return response.blob();
}
