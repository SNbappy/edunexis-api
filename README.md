# EduNexis - University Course Management Platform

EduNexis is the backend service powering the EduNexis Course Management Platform. It is built using **ASP.NET Core (.NET 10)** following **Clean Architecture** guidelines, incorporating CQRS patterns, custom database caching, and robust security protocols.

---

## 🛠️ Tech Stack

* **Runtime & Framework**: ASP.NET Core (.NET 10.0)
* **Language**: C# 14
* **Database**: MySQL (via [Pomelo.EntityFrameworkCore.MySql](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql))
* **Mediator Pattern**: [Mediator.NET](https://github.com/mgravell/Mediator.Abstractions) (reflection-free, source-generated mediator)
* **Object Mapping**: Mapster
* **Validations**: FluentValidation
* **Caching**: Redis (via StackExchange.Redis)
* **Security & Auth**: JWT Bearer + BCrypt Password Hashing
* **Cloud Storage**: Cloudinary (via CloudinaryDotNet)
* **Emails**: FluentEmail Smtp
* **Logging**: Serilog (structured logging to console and rolling files)

---

## 📐 Architecture & Project Structure

The codebase is organized into four projects under `src/` conforming to Clean Architecture principles:

```bash
src/
├── EduNexis.Domain/        # Core business models, entities, and domain logic (28 entities)
│   ├── Common/             # Auditable base entities
│   ├── DomainEvents/       # Domain event triggers
│   ├── Entities/           # Database models (User, Course, Formula, CTEvent, etc.)
│   ├── Enums/              # Strong-typed role and type system enums
│   ├── Exceptions/         # Domain-specific custom exceptions
│   └── Interfaces/         # Repository & service interface contracts
│
├── EduNexis.Application/   # Use cases, DTOS, validators, and CQRS handlers
│   ├── Abstractions/       # Current user contexts and security abstractions
│   ├── Behaviors/          # Pipeline behaviors (Logging, Validation)
│   ├── DTOs/               # Data Transfer Objects
│   ├── Features/           # CQRS Command/Query folders (Announcements, Marks, Auth, etc.)
│   └── Validators/         # FluentValidation rules
│
├── EduNexis.Infrastructure/# Databases, external integrations, caching, and mail services
│   ├── Persistence/        # EF Core AppDbContext, Repositories, Unit of Work, Migrations
│   └── Services/           # Cloudinary storage, BCrypt hashers, and SMTP mail templates
│
└── EduNexis.API/           # Web API host, endpoints, custom middlewares, and filters
    ├── Controllers/        # 15 REST API Controllers mapping incoming routes to CQRS queries
    ├── Middleware/         # Global Exception handling middleware
    └── Hubs/               # Real-time WebSocket services (if configured)
```

---

## 🚀 Getting Started

### Prerequisites
* **.NET SDK**: `10.0` or later
* **MySQL Database Server**: `v8.0` or later
* **Redis Server**: (Optional) For high-performance response caching

### Configuration

Create or modify `src/EduNexis.API/appsettings.json` (or setup environment variables) with the following connection values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=edunexis;User=root;Password=yourpassword;"
  },
  "Jwt": {
    "Secret": "YOUR_SUPER_SECRET_MIN_256_BIT_KEY_HERE",
    "Issuer": "EduNexis",
    "Audience": "EduNexisUsers",
    "ExpiryMinutes": 60
  },
  "Cloudinary": {
    "CloudName": "your-cloud-name",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret",
    "DefaultCoverUrl": "https://res.cloudinary.com/..."
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  },
  "Database": {
    "RunMigrationsOnStartup": false
  }
}
```

### Installation

1. Navigate to the API root:
   ```bash
   cd edunexis-api
   ```

2. Restore NuGet dependencies:
   ```bash
   dotnet restore EduNexis.slnx
   ```

3. Run EF database migrations (if setting up the DB for the first time):
   ```bash
   dotnet ef database update --project src/EduNexis.Infrastructure --startup-project src/EduNexis.API
   ```

4. Launch the application:
   ```bash
   dotnet run --project src/EduNexis.API
   ```

---

## 🐳 Docker Deployment

The application includes a multi-stage `Dockerfile` in the root directory. To build and run the API container locally:

```bash
# Build the Docker image
docker build -t edunexis-api .

# Run the container mapping port 8080
docker run -d -p 5041:8080 --name edunexis-backend edunexis-api
```

---

## ⚠️ Important Database Note (Clever Cloud Connection Limit)

Free Clever Cloud MySQL tiers are capped at **5 concurrent connections**. To optimize connection usage:
1. **Migrations on Startup** are turned off by default (`RunMigrationsOnStartup: false`). Running automated migrations on container restarts consumes connection pools and crashes simultaneous deployments.
2. In production, manual schema migrations are applied using the pre-generated [migration.sql](migration.sql) file.
