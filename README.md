# Personal Finance API

Backend REST API untuk **Personal Finance Management System** yang dibangun menggunakan **ASP.NET Core Web API, Entity Framework Core, dan PostgreSQL**.

API ini dirancang untuk mengelola keuangan pribadi secara terstruktur, mulai dari autentikasi pengguna, pengelolaan akun keuangan, kategori, transaksi, budget, hingga financial goals.

Project ini dikembangkan dengan pendekatan **backend-focused**, dengan fokus pada API design, database management, authentication, business logic, data validation, dan system scalability.

---

## 🚀 Features

### 🔐 Authentication

* Google OAuth 2.0 authentication
* JWT authentication
* JWT stored using HttpOnly Cookie
* Protected API endpoints
* User-based data isolation
* Authentication session checking

### 💳 Account Management

* Create financial accounts
* Update account information
* Delete accounts
* Track account balance
* Support multiple account types
* Prevent deletion of accounts with existing transactions

### 💰 Transaction Management

* Record income and expense transactions
* Automatically update account balance
* Validate account ownership
* Validate category ownership
* Validate transaction type against category type
* Prevent expenses from exceeding account balance
* Update transactions with balance reversal
* Delete transactions with automatic balance reversal

### 🏷️ Category Management

* Income and expense categories
* Global categories
* User-specific categories
* User data isolation
* Category validation

### 📊 Budget Management

* Create budgets by category
* Define budget periods
* Validate category ownership
* Track spending based on transaction data

### 🎯 Financial Goals

* Create financial goals
* Define target amounts
* Set optional target dates
* Track goal allocation
* Calculate remaining amount
* Calculate progress
* Automatically allocate available account balance to goals

### 📈 Dashboard

Provides aggregated financial information including:

* Total income
* Total expense
* Net transaction value
* Total account balance
* Financial goal allocation
* Available money
* Monthly transaction summary
* Yearly transaction summary
* Category summary
* Account summary
* Budget information

---

# 🛠️ Technology Stack

| Technology            | Purpose                         |
| --------------------- | ------------------------------- |
| C#                    | Programming language            |
| ASP.NET Core          | Web API framework               |
| Entity Framework Core | ORM & database access           |
| PostgreSQL            | Relational database             |
| Npgsql                | PostgreSQL provider for EF Core |
| JWT                   | API authentication              |
| Google OAuth 2.0      | Social authentication           |
| Swagger / OpenAPI     | API documentation & testing     |
| Git & GitHub          | Version control                 |

---

# 🏗️ Architecture

The backend follows a layered structure to separate API controllers, business logic, data access, entities, and request models.

```text
Client / Frontend
       │
       ▼
┌──────────────────────┐
│   ASP.NET Core API   │
└──────────────────────┘
       │
       ▼
┌──────────────────────┐
│     Controllers      │
└──────────────────────┘
       │
       ├──────────────► DTOs
       │
       ▼
┌──────────────────────┐
│      Services        │
└──────────────────────┘
       │
       ▼
┌──────────────────────┐
│     AppDbContext     │
│   Entity Framework   │
└──────────────────────┘
       │
       ▼
┌──────────────────────┐
│      PostgreSQL      │
└──────────────────────┘
```

---

# 🗄️ Database Design

The application uses a relational database with PostgreSQL.

The main entities are:

```text
Users
 │
 ├── Accounts
 │      │
 │      └── Transactions
 │
 ├── Categories
 │      ├── Transactions
 │      └── Budgets
 │
 ├── Transactions
 │
 ├── Budgets
 │
 └── FinancialGoals
```

## Entity Relationship

```text
┌─────────────┐
│    Users    │
└──────┬──────┘
       │
       ├───────────────┐
       │               │
       ▼               ▼
┌─────────────┐   ┌──────────────┐
│  Accounts   │   │  Categories  │
└──────┬──────┘   └──────┬───────┘
       │                 │
       │                 │
       └────────┬────────┘
                ▼
        ┌──────────────┐
        │ Transactions │
        └──────────────┘

┌─────────────┐
│    Users    │
└──────┬──────┘
       │
       ├───────────────► Budgets
       │
       └───────────────► FinancialGoals
```

---

## Database Tables

### `users`

Stores application user information and authentication data.

| Column          | Type           | Description                            |
| --------------- | -------------- | -------------------------------------- |
| `id`            | UUID           | Primary key                            |
| `full_name`     | VARCHAR        | User's full name                       |
| `email`         | VARCHAR        | Unique email                           |
| `password_hash` | VARCHAR / NULL | Password hash for local authentication |
| `google_id`     | VARCHAR / NULL | Google account identifier              |
| `created_at`    | TIMESTAMP      | Creation timestamp                     |
| `updated_at`    | TIMESTAMP      | Last update timestamp                  |

---

### `accounts`

Stores the user's financial accounts.

| Column         | Type      | Description             |
| -------------- | --------- | ----------------------- |
| `id`           | UUID      | Primary key             |
| `user_id`      | UUID      | Owner of the account    |
| `name`         | VARCHAR   | Account name            |
| `account_type` | VARCHAR   | Account type            |
| `balance`      | NUMERIC   | Current account balance |
| `created_at`   | TIMESTAMP | Creation timestamp      |
| `updated_at`   | TIMESTAMP | Last update timestamp   |

Examples of account types:

```text
cash
bank
e-wallet
```

---

### `categories`

Stores income and expense categories.

| Column          | Type        | Description                                 |
| --------------- | ----------- | ------------------------------------------- |
| `id`            | UUID        | Primary key                                 |
| `user_id`       | UUID / NULL | User owner; NULL represents global category |
| `name`          | VARCHAR     | Category name                               |
| `category_type` | VARCHAR     | `income` or `expense`                       |
| `created_at`    | TIMESTAMP   | Creation timestamp                          |
| `updated_at`    | TIMESTAMP   | Last update timestamp                       |

The application supports both:

* Global categories
* User-specific categories

Global categories can be used by multiple users without duplicating the category for every user.

---

### `transactions`

Stores financial income and expense records.

| Column             | Type        | Description           |
| ------------------ | ----------- | --------------------- |
| `id`               | UUID        | Primary key           |
| `user_id`          | UUID        | Transaction owner     |
| `account_id`       | UUID        | Related account       |
| `category_id`      | UUID        | Related category      |
| `transaction_type` | VARCHAR     | `income` or `expense` |
| `amount`           | NUMERIC     | Transaction amount    |
| `transaction_date` | TIMESTAMP   | Transaction date      |
| `description`      | TEXT / NULL | Optional description  |
| `created_at`       | TIMESTAMP   | Creation timestamp    |
| `updated_at`       | TIMESTAMP   | Last update timestamp |

Transaction processing automatically affects the related account balance.

```text
Income
    ↓
Account Balance increases

Expense
    ↓
Account Balance decreases
```

---

### `budgets`

Stores spending limits for specific categories and periods.

| Column         | Type      | Description            |
| -------------- | --------- | ---------------------- |
| `id`           | UUID      | Primary key            |
| `user_id`      | UUID      | Budget owner           |
| `category_id`  | UUID      | Related category       |
| `amount`       | NUMERIC   | Budget amount          |
| `period_start` | TIMESTAMP | Start of budget period |
| `period_end`   | TIMESTAMP | End of budget period   |
| `created_at`   | TIMESTAMP | Creation timestamp     |

---

### `financial_goals`

Stores the user's financial targets.

| Column           | Type             | Description              |
| ---------------- | ---------------- | ------------------------ |
| `id`             | UUID             | Primary key              |
| `user_id`        | UUID             | Goal owner               |
| `name`           | VARCHAR          | Goal name                |
| `target_amount`  | NUMERIC          | Target amount            |
| `current_amount` | NUMERIC          | Current allocated amount |
| `target_date`    | TIMESTAMP / NULL | Optional target date     |
| `created_at`     | TIMESTAMP        | Creation timestamp       |
| `updated_at`     | TIMESTAMP        | Last update timestamp    |

Financial goals use a **virtual allocation model**. The system does not transfer money between accounts. Instead, available account balances are logically allocated to goals for financial planning.

---

# 💡 Financial Calculation

The application distinguishes between **transaction net value** and **actual account balance**.

### Transaction Net

```text
Total Income - Total Expense
```

For example:

```text
Income       : Rp1.000.000
Expense      : Rp110.000
-------------------------
Transaction Net: Rp890.000
```

Transaction net represents the net movement from recorded transactions.

### Account Balance

Account balance also includes the account's opening balance.

```text
Opening Balance
+ Income
- Expense
----------------
Current Balance
```

Example:

```text
Opening Balance : Rp1.000.001
Income          : Rp1.000.000
Expense         : Rp110.000
---------------------------
Account Balance : Rp1.890.001
```

Therefore, financial goals use the **actual account balance** as the available financial pool rather than transaction net alone.

---

# 🔄 Transaction Balance Logic

When a transaction is created:

```text
Income
    Account Balance += Amount

Expense
    Account Balance -= Amount
```

When a transaction is updated:

```text
1. Reverse previous transaction
2. Validate new transaction
3. Apply new transaction
```

When a transaction is deleted:

```text
Reverse transaction effect
        ↓
Restore account balance
```

These operations are handled atomically using database transactions to maintain data consistency.

---

# 🎯 Financial Goal Allocation

Financial goals are allocated sequentially based on priority.

The current implementation orders goals by:

```text
1. Target Date
2. Creation Date
```

Example:

```text
Available Account Balance
        │
        ▼
┌──────────────────┐
│ Financial Goal 1 │ → Allocation
└──────────────────┘
        │
        ▼
┌──────────────────┐
│ Financial Goal 2 │ → Remaining allocation
└──────────────────┘
        │
        ▼
┌──────────────────┐
│ Financial Goal 3 │ → Remaining allocation
└──────────────────┘
```

This is a **virtual allocation**, meaning the system does not physically move money between accounts.

---

# 🔐 Authentication Flow

The application supports Google OAuth 2.0 authentication.

```text
User
 │
 ▼
Frontend
 │
 ▼
Google OAuth
 │
 ▼
ASP.NET Core API
 │
 ├── Find/Create User
 │
 ├── Generate JWT
 │
 ▼
HttpOnly Cookie
 │
 ▼
Authenticated API Requests
```

JWT authentication is handled through an HttpOnly cookie to reduce direct JavaScript access to the authentication token.

---

# 📡 API Endpoints

Main API resources include:

```text
/auth
/accounts
/categories
/transactions
/budget
/financialgoal
/dashboard
```

Swagger / OpenAPI is provided for API exploration and testing.

When running locally:

```text
https://localhost:7253/swagger
```

> The exact URL may differ depending on the local launch profile.

---

# 📂 Project Structure

```text
PersonalFinance.Api/
│
├── Controllers/
│   ├── AccountsController.cs
│   ├── AuthController.cs
│   ├── BudgetController.cs
│   ├── CategoriesController.cs
│   ├── DashboardController.cs
│   ├── FinancialGoalController.cs
│   └── TransactionController.cs
│
├── Data/
│   └── AppDbContext.cs
│
├── DTOs/
│   ├── Accounts/
│   ├── Budget/
│   ├── Categories/
│   ├── FinancialGoal/
│   └── Transaction/
│
├── Entities/
│   ├── Account.cs
│   ├── Budget.cs
│   ├── Category.cs
│   ├── FinancialGoal.cs
│   ├── Transaction.cs
│   └── User.cs
│
├── Migrations/
│
├── Services/
│   └── JwtService.cs
│
├── Properties/
│   └── launchSettings.json
│
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
└── PersonalFinance.Api.csproj
```

---

# ⚙️ Getting Started

## Prerequisites

Make sure the following are installed:

* .NET SDK 10
* PostgreSQL 18
* Git
* Visual Studio Code or Visual Studio

---

## Clone Repository

```bash
git clone https://github.com/marco-sihombing/personal-finance-api.git

cd personal-finance-api
```

---

## Configure Database

Create a PostgreSQL database:

```sql
CREATE DATABASE personal_finance;
```

Configure the development connection string in:

```text
appsettings.Development.json
```

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=personal_finance;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
  }
}
```

> Do not commit `appsettings.Development.json` or other files containing credentials to GitHub.

---

## Run Database Migration

If the repository already contains migrations:

```bash
dotnet ef database update
```

If EF Core CLI is not installed:

```bash
dotnet tool install --global dotnet-ef
```

---

## Run Application

```bash
dotnet restore
dotnet build
dotnet run
```

The API will run according to the configured launch profile.

Swagger can then be accessed through:

```text
https://localhost:7253/swagger
```

---

# 🔧 Configuration

The application uses environment-specific configuration.

Example:

```text
appsettings.json
appsettings.Development.json
appsettings.example.json
```

Sensitive configuration such as:

* Database password
* JWT secret
* Google OAuth Client Secret

should not be committed to source control.

---

# 🧪 Development

Useful commands:

```bash
dotnet restore
dotnet build
dotnet run
dotnet ef migrations add MigrationName
dotnet ef database update
```

---

# 📌 Design Principles

This project applies several backend development principles:

* **Separation of Concerns**
* **DTO-based API requests**
* **Entity Framework Core**
* **Relational database design**
* **User data isolation**
* **Input validation**
* **Authentication & Authorization**
* **Atomic database transactions**
* **Consistent financial calculations**
* **RESTful API design**
* **Scalable application structure**

---

# 🔮 Future Development

Planned improvements include:

* Centralized Financial Goal Service
* More comprehensive automated testing
* Unit and integration tests
* Refresh token mechanism
* Role-based authorization
* More advanced financial analytics
* Machine Learning-based spending analysis
* Financial recommendations
* Docker deployment
* Cloud/VPS deployment
* CI/CD pipeline

---

# 📄 License

This project is developed for educational, portfolio, and personal development purposes.
