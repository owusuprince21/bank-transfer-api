# ApiDemo Banking Monorepo

This workspace has two apps:

- `backend/` - ASP.NET Core banking API with SQL Server, EF Core, Scalar UI, and customer login.
- `frontend/` - Vue customer dashboard for sign-in, deposits, withdrawals, transfers, account creation, balances, and transaction history.

## Run Backend

```powershell
cd backend
dotnet run --urls http://localhost:5018
```

Scalar UI:

```text
http://localhost:5018/scalar
```

## Run Frontend

```powershell
cd frontend
npm install
npm run dev
```

Vue app:

```text
http://localhost:5173
```

## Demo Login

When the backend runs in Development, it seeds a demo customer:

```text
Email: demo.customer@example.com
Password: Password123!
```

Existing development customers with an empty password hash are also assigned:

```text
Password: Password123!
```

## Dashboard Features

- Left sidebar profile and navigation.
- Account creation.
- Deposits and withdrawals.
- Send money to existing customer accounts.
- Ghana cedi currency formatting.
- Idle auto-logout after 3 minutes of inactivity.
- Sonner toast notifications.
