# 🧠 CADR Backend

**CADR Backend** is the server-side part of the [CADR](https://github.com/CADR-PG) project — a robust application for collaborative asset management and real-time scene editing for 3D game development teams.

The frontend repository is here: [CADR Frontend](https://github.com/CADR-PG/CADR-front)

---

## 🌐 Production Access

API documentation and the live production instance are available at:

👉 [https://api.cadr.studio/docs/](https://api.cadr.studio/docs/)

---

## ⚙️ Technologies

The project is written in **C#** using **.NET 9** and **ASP.NET Core** framework.

Technologies and services used:

* 🧩 **.NET 9**
* 🚀 **ASP.NET Core**
* 🗄️ **Entity Framework Core**
* 🐘 **PostgreSQL**
* ☁️ **Azure Storage**

---

## 🧰 Running the Project Locally

### 🔐 Setting up HTTPS Certificates (one-time)

Before the first run, local development HTTPS certificates need to be configured.
Documentation: [ASP.NET Core – Docker HTTPS setup](https://learn.microsoft.com/en-us/aspnet/core/security/docker-https?view=aspnetcore-9.0)

#### Linux / macOS:

```bash
dotnet dev-certs https --clean
dotnet dev-certs https -ep ${HOME}/.aspnet/https/aspnetapp.pfx -p dev
dotnet dev-certs https --trust
```

#### Windows:

```powershell
dotnet dev-certs https --clean
dotnet dev-certs https -ep %USERPROFILE%/.aspnet/https/aspnetapp.pfx -p dev
dotnet dev-certs https --trust
```

### 🐳 Running the Project with Docker

From the project root directory:

```bash
docker compose -f tools/docker/docker-compose.yml up -d --build
```

Local development links:
- CADR API (HTTP): http://localhost:8080/
- CADR API (HTTPS): https://localhost:8081/
- mailpit (email & SMTP testing): http://localhost:8025/

---

## 🧩 Modules

### 👤 `users`
User management, authentication, and session handling:

- login / registration
- session based on **Access JWT** (http-only cookie)
- session refresh using **Refresh JWT**
- logout (removing tokens from cookies)
- retrieve user data (identity only - names and display name live in the `community` user profile)
- change email + confirmation email
- password change (after login)
- resend email confirmation

---

### 📁 `projects`
Project management module (currently linked to `users` module, planned to be separated):

- add a project (name, description)
- edit project details
- fetch user project list (with pagination)
- fetch raw scene file
- save raw scene file

---

## 📨 Messaging between modules

Modules communicate asynchronously with **integration events** (contracts live in `*.Contracts` projects) using
[Wolverine](https://wolverinefx.net/) and **Azure Service Bus** topics (one subscription per consuming module):

- **`API`** publishes events through Wolverine's **transactional outbox** (`IDbContextOutbox<TDbContext>`) - the event is committed in the same transaction as the business data,
- **`MessageBroker`** (separate process) listens to subscriptions and runs Wolverine **message handlers** (durable inbox, EF Core transactional middleware, retries).

Each process keeps its outbox/inbox in its own PostgreSQL schema (`wolverine_api`, `wolverine_message_broker`).
Modules register their handlers in `IModule.RegisterMessageBroker`.

Events carry identifiers only - consumers fetch current data through the public module APIs in `*.Contracts`
(e.g. `Users.Contracts.IUsersProvider`), which can be injected into message handlers.

| Event | Publisher | Topic | Consumers |
|-------|-----------|-------|-----------|
| `UserRegistered` (`UserId` only) | `users` (registration, first Google/GitHub login) | `users.user-registered` | `community` – fetches the user with `IUsersProvider`, creates user profile (display name = email part before `@`) & default settings |

Configuration (`Shared:Messaging`):
- `AzureServiceBusConnectionString` – required (`Development` and `Docker` use the emulator from `docker-compose.yml`),
- `AzureServiceBusManagementConnectionString` – required only by the emulator,
- `AutoProvision` (default `true`) – creates topics/subscriptions/queues on startup (requires the `Manage` claim).

---

## ⏱️ Scheduled jobs (Azure Functions)

**`FunctionsWorker`** is an Azure Functions host (isolated worker, .NET 10) that references the modules.
Functions are defined **in the modules** (`<Module>.Core/Functions/*Function.cs`), their dependencies are registered in `IModule.RegisterFunctions`.

| Function | Module | Trigger | Description |
|----------|--------|---------|-------------|
| `Community-SynchronizeUserProfiles` | `community` | timer `0 0 0 * * *` (daily, 00:00 UTC) | creates missing user profiles & default settings for every user of the `users` module (idempotent reconciliation - safety net for lost/dead-lettered `UserRegistered` events) |

Running locally: [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local) (`func start` in `src/FunctionsWorker`) with Azurite (`AzureWebJobsStorage=UseDevelopmentStorage=true`), or the `cadr.functions-worker` service in `docker-compose.yml`.
A timer function can be triggered manually with `POST /admin/functions/<name>` (master key required). In Docker:

```bash
# the first request with any key makes the host generate its keys
docker exec cadr-functions-worker curl -s -X POST localhost/admin/functions/Community-SynchronizeUserProfiles -H "x-functions-key: x" -d '{}'
KEY=$(docker exec cadr-functions-worker cat /azure-functions-host/Secrets/host.json | jq -r .masterKey.value)
docker exec cadr-functions-worker curl -s -X POST localhost/admin/functions/Community-SynchronizeUserProfiles -H "x-functions-key: $KEY" -H "Content-Type: application/json" -d '{}'
```

---

## 🔧 Planned Refactor

### Refactoring and Future Development:
- migrate to **.NET 10**
- separate `projects` module from `users`
- change project storage → **Azure Blob Storage**
- update CI/CD and Azure deployment process
- implement custom **discriminated unions** using `generic type`
- improve email client security
- add new **integration tests**
- (optional) implement **HATEOAS**