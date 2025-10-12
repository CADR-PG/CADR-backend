# CADR-backend

## 📘 Project Description

**CADR-backend** is the server-side component of the **CADR** system, built using **ASP.NET Core**.  
The application serves as the backend — responsible for user authentication, data management, and communication with the database through a REST API.  
The project was designed with a focus on **security, scalability**, and **clean architecture**.

---

## ⚙️ Technologies and Tools

- **C# / .NET 8.0** – main language and application platform
- **ASP.NET Core Web API** – framework for building modern REST APIs
- **Entity Framework Core** – ORM for database access
- **JWT (JSON Web Token)** – user authentication using tokens stored in HttpOnly cookies
- **PostgreSQL / MSSQL** – database (depending on configuration)
- **Docker & Docker Compose** – application containerization
- **Scalar** – automatic API documentation

---

## 🚀 Installation and Running

### 1️⃣ Requirements

- .NET SDK 9.0 or newer  
- Docker (optional, for containerized setup)  
- PostgreSQL  
- Visual Studio / Rider / VS Code  

---

### 2️⃣ Clone the Repository

```bash
git clone https://github.com/CADR-PG/CADR-backend.git
cd CADR-backend
```

---

### 3️⃣ Environment Configuration

Create an `.env` file or configure environment variables inside `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=cadr;Username=XXXX;Password=XXXXX"
  },
  "Jwt": {
    "Issuer": "XXXXXX",
    "Audience": "xxxxx",
    "Key": "xxxxx"
  }
}
```

---

### 4️⃣ Database Migrations

Apply migrations (if the project uses them):

```bash
dotnet ef database update
```

---

### 5️⃣ Run the Application

#### 💻 Locally:

```bash
dotnet run --project src/CADR-backend
```
The app will be available at:  
👉 `https://localhost:5001`  
👉 `http://localhost:5000`

#### 🐳 Using Docker:

```bash
docker-compose up --build
```

---

## 🗂️ Project Structure

```
CADR-backend/
├── src/
│   ├── CADR.Api/              # Main API application
│   ├── CADR.Core/             # Domain logic and models
│   ├── CADR.Infrastructure/   # Data access layer, EF Core
│   └── CADR.Application/      # Services, DTOs, business logic
├── tests/                     # Unit/integration tests
├── docker-compose.yml
├── README.md
└── LICENSE
```

---

## 📄 API Documentation

After running the app, open:  
📍 `https://localhost:5001/swagger/index.html`  
Here you'll find the full API documentation in Swagger UI format.

---

## 🔒 Authentication

The system uses **JWT tokens** stored in **HttpOnly cookies**, enhancing security.  
It also supports **refresh tokens** for seamless session renewal without re-login.

---

## 🧠 Key Features

- User registration and login  
- JWT-based authentication  
- CRUD operations for main resources  
- Database integration via EF Core  
- Data validation and clean, layered architecture  

---

## 📜 License

This project is released under the **MIT License** — you are free to use, modify, and distribute the code, provided that attribution to the authors is maintained.

---

### ❤️ Thanks for your support!

If you like this project — give it a ⭐ on GitHub!
