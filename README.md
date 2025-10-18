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
docker compose -f tools/docker/docker-compose.api.yml -p cadr up
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
- retrieve and update user data
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