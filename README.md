<div align="center">

# EduNexis API

**The backend for [EduNexis](https://edunexis.vercel.app) — a learning management system built at Jashore University of Science and Technology.**

ASP.NET Core 10 · Clean Architecture · CQRS · MySQL · Redis · Cloudinary

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-14-239120?style=flat-square&logo=csharp&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-9-512BD4?style=flat-square)
![MySQL](https://img.shields.io/badge/MySQL-8-4479A1?style=flat-square&logo=mysql&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-cache-DC382D?style=flat-square&logo=redis&logoColor=white)
![Swagger](https://img.shields.io/badge/OpenAPI-Swagger-85EA2D?style=flat-square&logo=swagger&logoColor=black)

</div>

---

## Table of contents

- [Overview](#overview)
- [Architecture](#architecture)
  - [Project dependencies](#project-dependencies)
  - [Request lifecycle](#request-lifecycle)
  - [CQRS and the pipeline](#cqrs-and-the-pipeline)
  - [Course-scoped authorisation](#course-scoped-authorisation)
- [Domain model](#domain-model)
- [API surface](#api-surface)
- [Cross-cutting concerns](#cross-cutting-concerns)
  - [Authentication and authorisation](#authentication-and-authorisation)
  - [Serialisation contract](#serialisation-contract)
  - [Error handling](#error-handling)
  - [Caching](#caching)
  - [File storage](#file-storage)
  - [Email and SMS](#email-and-sms)
  - [Third-party analysis](#third-party-analysis)
  - [Logging](#logging)
- [Tech stack](#tech-stack)
- [Project layout](#project-layout)
- [Configuration](#configuration)
- [Running locally](#running-locally)
- [Database and migrations](#database-and-migrations)
- [Tests](#tests)
- [Deployment](#deployment)
- [Known constraints and deliberate decisions](#known-constraints-and-deliberate-decisions)
- [Related repository](#related-repository)

---

## Overview

This service is the entire backend for EduNexis: courses, enrolment, attendance, materials, assignments and submissions, class tests, vivas and presentations, weighted grading formulas, published results, notifications, profiles and the public faculty directory.

It is a single ASP.NET Core web API organised into four projects following Clean Architecture, with every use case expressed as a CQRS command or query dispatched through a source-generated mediator.

| | |
|---|---|
| **Runtime** | ASP.NET Core 10.0, C# 14 |
| **Projects** | `Domain`, `Application`, `Infrastructure`, `API` |
| **Domain entities** | 32 |
| **Controllers** | 14 (+ a shared `BaseController`) |
| **Endpoints** | ~118 |
| **Feature slices** | 13 |
| **EF Core migrations** | 25 |
| **Docs** | Swagger UI at `/swagger`, health probe at `/health` |

---

## Architecture

### Project dependencies

Dependencies point inward. The domain knows nothing about EF Core, HTTP or Cloudinary.

```
┌───────────────────────────────────────────────────────────┐
│  EduNexis.API                                             │
│  controllers · exception middleware · JSON converters     │
│  JWT bearer setup · CORS · Swagger · Serilog              │
└───────────────┬───────────────────────────┬───────────────┘
                │                           │
                ▼                           ▼
┌───────────────────────────────┐   ┌───────────────────────────────┐
│  EduNexis.Application         │   │  EduNexis.Infrastructure      │
│  commands · queries · DTOs    │   │  AppDbContext · repositories  │
│  handlers · validators        │◄──│  unit of work · migrations    │
│  pipeline behaviours          │   │  Cloudinary · Redis · email   │
│  abstractions                 │   │  JWT · BCrypt · OTP · SMS     │
└───────────────┬───────────────┘   └───────────────┬───────────────┘
                │                                   │
                └─────────────┬─────────────────────┘
                              ▼
              ┌───────────────────────────────────┐
              │  EduNexis.Domain                  │
              │  entities · enums · exceptions    │
              │  repository & service interfaces  │
              └───────────────────────────────────┘
```

### Request lifecycle

```
HTTP request
     │
     ▼
ExceptionMiddleware ......... maps domain exceptions to HTTP status codes
     │
     ▼
Serilog request logging
     │
     ▼
CORS  →  Authentication (JWT)  →  Authorization (role policies)
     │
     ▼
Controller ................... thin; reads claims, builds a command/query
     │
     ▼
Mediator.Send()
     │
     ▼
ArchivedCourseGuardBehavior .. rejects writes to archived courses
     │
     ▼
Handler ...................... use case logic
     │
     ├─► IUnitOfWork ......... repositories over EF Core / MySQL
     ├─► ICacheService ....... Redis
     ├─► IFileStorageService . Cloudinary
     ├─► IEmailService ....... Brevo HTTP API
     └─► ISender ............. nested commands (e.g. raise a notification)
     │
     ▼
ApiResponse<T> ............... uniform success / message / data / errors envelope
```

### CQRS and the pipeline

Every use case is a `record` implementing `ICommand<T>` or `IQuery<T>`, paired with a handler, and lives in a feature folder next to the DTOs and validators it uses.

```csharp
public record SubmitAssignmentCommand(
    Guid AssignmentId,
    Guid StudentId,
    SubmissionType SubmissionType,
    string? TextContent,
    IReadOnlyList<IncomingFile> Files,
    IReadOnlyList<string> Links,
    IReadOnlyList<Guid>? KeepAttachmentIds = null
) : ICommand<ApiResponse<SubmissionDto>>, ICourseScopedWrite
{
    public async ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => (await uow.GetRepository<Assignment>().GetByIdAsync(AssignmentId, ct))?.CourseId;
}
```

Dispatch uses **[Mediator](https://github.com/martinothamar/Mediator)** with a source generator rather than reflection, so handler resolution is compile-time and there is no runtime scanning cost.

Pipeline behaviours are resolved from DI, and **registration order is execution order**. Only `ArchivedCourseGuardBehavior` is registered today — `ValidationBehavior` and `LoggingBehavior` exist but are deliberately left unregistered (see [known constraints](#known-constraints-and-deliberate-decisions)).

### Course-scoped authorisation

Almost every write in the system belongs to a course, and the check "may this user write to this course?" is easy to forget in a new handler. Rather than repeat it, a command declares itself course-scoped:

```csharp
public interface ICourseScopedWrite
{
    ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct);
}
```

`ArchivedCourseGuardBehavior` intercepts anything implementing it, resolves the owning course and rejects writes to archived courses before the handler runs.

This is enforced by an **architecture test**, not by convention: `CourseScopedWriteCoverageTests` reflects over every command in the assembly and fails the build if one is neither guarded nor on an explicit exemption list *with a stated reason*. Adding an unguarded course write breaks CI.

---

## Domain model

32 entities, grouped by the part of a course they serve.

| Area | Entities |
|---|---|
| **Identity** | `User`, `UserProfile`, `UserEducation`, `UserPublication`, `PasswordResetToken` |
| **Courses** | `Course`, `CourseMember`, `JoinRequest`, `TeacherQuota` |
| **Stream** | `Announcement`, `AnnouncementComment` |
| **Materials** | `Material` |
| **Assignments** | `Assignment`, `AssignmentSubmission`, `SubmissionAttachment`, `AssignmentComment`, `PlagiarismReport` |
| **Attendance** | `AttendanceSession`, `AttendanceRecord` |
| **Assessments** | `CTEvent`, `CTSubmission`, `PresentationEvent`, `PresentationMark` |
| **Results** | `GradingFormula`, `FormulaComponent`, `FinalMark`, `GradeComplaint`, `GradeComplaintMessage` |
| **Platform** | `Notification`, `NotificationPreference`, `PlatformSetting`, `AuditLog` |

Entities are behaviour-bearing rather than anaemic — state changes go through named methods (`Course.Archive()`, `AssignmentSubmission.Grade()`, `AttendanceSession.Update()`) with private setters, so invariants live with the data.

**14 enums** give the model a strongly typed vocabulary: `UserRole`, `CourseType`, `MaterialType`, `SubmissionType`, `SubmissionAttachmentKind`, `AttendanceStatus`, `CTStatus`, `PresentationStatus`, `PresentationFormat`, `FormulaComponentType`, `JoinRequestStatus`, `ComplaintStatus`, `NotificationType`, `PublicationType`.

### Soft deletion

Two independent flags, because they mean different things:

- `IsDeletedByOwner` — a teacher moved the course to their own 30-day recycle bin. Restorable, and hidden from every listing.
- `IsDeleted` — the audit-level soft delete on `BaseEntity`.

There is no global EF query filter, so both are filtered explicitly at each call site.

---

## API surface

All routes are prefixed `/api`. Everything requires a bearer token except `/api/auth/*` and `/api/public/*`.

| Controller | Endpoints | Responsibility |
|---|:---:|---|
| `AuthController` | 11 | Register, login, OTP verification, resend OTP, refresh, logout, forgot/reset/change password, `me`, sync |
| `CoursesController` | 20 | CRUD, my-courses, lookup by join code, quota, members, join requests and review, archive/unarchive, recycle bin (list, restore, permanent delete), student join/leave |
| `ProfileController` | 18 | Own and public profiles, photo and cover upload, education, publications, public-visibility slug, user course lists |
| `AssignmentsController` | 12 | Assignment CRUD, submit (multipart, multi-attachment), my-submission, submissions list, grade, class comments |
| `CTController` | 9 | Class-test CRUD, publish/unpublish, best/worst/average script upload, marks entry |
| `PresentationsController` | 9 | Vivas, presentations and other tests; draft/publish lifecycle, per-student marks |
| `AnnouncementsController` | 8 | Stream posts, attachments, pin/unpin, comments |
| `AdminController` | 6 | Platform settings, teacher quota grants and ledger |
| `NotificationsController` | 6 | Feed, mark read, mark all read, delete, preferences |
| `AttendanceController` | 5 | Session list, create, update, delete, course summary |
| `MarksController` | 5 | Grading formula get/save, calculate, publish, marks read |
| `PublicController` | 4 | Faculty list, faculty by slug, departments, platform stats |
| `MaterialsController` | 3 | Upload file, add folder/link, delete |
| `AnalysisController` | 2 | AI-content detection, web-plagiarism lookup |

Every response is wrapped in the same envelope, so the client has one shape to handle:

```json
{ "success": true, "message": "Success", "data": { }, "errors": null }
```

Interactive documentation is served at **`/swagger`**, with the bearer scheme wired in so endpoints can be exercised from the browser. `/health` returns status and a UTC timestamp for uptime probes.

---

## Cross-cutting concerns

### Authentication and authorisation

- **JWT bearer** tokens, HMAC-SHA256, validating issuer, audience, signing key and lifetime with `ClockSkew = TimeSpan.Zero` so expiry is exact.
- Passwords hashed with **BCrypt**.
- Sign-up is gated to `@just.edu.bd` and `@student.just.edu.bd`; the domain determines whether the account becomes a `Teacher` or a `Student`.
- Email ownership is proven by a **6-digit OTP** before the account can be used.
- Role policies are declared per endpoint (`[Authorize(Roles = "Teacher,SuperAdmin,DepartmentAdmin")]`), with `ICurrentUserService` exposing the caller's id and role to handlers without threading `HttpContext` through the application layer.

### Serialisation contract

Two deliberate choices keep the client honest:

- **Enums serialise as strings.** `"Text"`, not `0` — the API stays readable and reordering an enum cannot silently change the contract.
- **All `DateTime` values serialise as ISO-8601 UTC with a `Z` suffix**, via custom `Iso8601UtcDateTimeConverter` / `Iso8601UtcNullableDateTimeConverter`. Without this, a browser `Date` parser treats a bare timestamp as local time, which shifted every deadline and attendance date by the timezone offset.

### Error handling

`ExceptionMiddleware` is the single place HTTP status codes are chosen. Domain exceptions (`NotFoundException`, `ForbiddenException`, validation failures) map to their proper codes and are returned inside the standard envelope; anything unexpected is logged in full and returned as a generic 500 without leaking internals.

### Caching

`ICacheService` over **Redis** (`StackExchange.Redis`) with `Microsoft.Extensions.Caching.StackExchangeRedis`. Redis is optional — the service degrades to passthrough when it is not configured, so local development needs no Redis instance.

### File storage

`IFileStorageService` is implemented by `CloudinaryStorageService`. Submissions, materials, CT answer scripts, profile photos and cover images all upload to Cloudinary under a per-feature folder prefix, and only the resulting URL plus filename and byte size are persisted. Uploads happen *before* the submission row is touched, so a storage failure leaves the previous submission intact rather than half-replaced.

### Email and SMS

- **Email goes out over the Brevo HTTP API**, not SMTP — Render's free tier blocks outbound SMTP ports, so `EmailService` POSTs to `https://api.brevo.com/v3/smtp/email` over HTTPS. Bulk sends are chunked to Brevo's per-call recipient limit. Templates are rendered with **Scriban**.
- **SMS is vendor-neutral by design.** Bangladeshi bulk-SMS providers (BulkSMSBD, SSL Wireless, MIM, Alpha Net) all expose the same shape — an endpoint, an API key, a sender id, a number and a message — so the endpoint is a configured template string and switching provider is a config change, not a code change.

Notification delivery is driven by `NotificationPreference`, and the catalogue is covered by tests asserting every `NotificationType` is manageable from settings, that SMS eligibility is a strict subset of email eligibility, and that no catalogue entry references a type that no longer exists.

### Third-party analysis

`AnalysisController` proxies two optional services, both key-gated — absent a key the endpoint returns a clean "not configured" response rather than failing:

- **AI-content detection** via ZeroGPT, normalised into a score, a human/AI split and a low/medium/high band.
- **Web-plagiarism lookup** via Copyleaks.

Cross-submission similarity is intentionally *not* here: it runs in the browser against already-uploaded files (Jaccard bigram over extracted PDF text), which keeps large PDF parsing off the server.

### Logging

**Serilog**, configured from `appsettings.json`, writing structured events to console and rolling files under `logs/`, plus `UseSerilogRequestLogging()` for a single well-formed line per request.

---

## Tech stack

| Concern | Package | Version |
|---|---|---|
| Framework | ASP.NET Core | 10.0 |
| Mediator / CQRS | `Mediator.Abstractions`, `Mediator.SourceGenerator` | 3.0.1 |
| ORM | `Microsoft.EntityFrameworkCore` | 9.0.3 |
| MySQL provider | `Pomelo.EntityFrameworkCore.MySql` | 9.0.0 |
| Validation | `FluentValidation` | 12.1.1 |
| Mapping | `Mapster` | 7.4.0 |
| Cache | `StackExchange.Redis` | 2.11.3 |
| Auth | `Microsoft.AspNetCore.Authentication.JwtBearer` | 9.0.0 |
| Hashing | `BCrypt.Net-Next` | 4.0.3 |
| Storage | `CloudinaryDotNet` | 1.28.0 |
| Email templating | `Scriban` | 7.0.6 |
| Logging | `Serilog.AspNetCore` | 10.0.0 |
| API docs | `Swashbuckle.AspNetCore` | 6.9.0 |
| Tests | xUnit | — |

---

## Project layout

```
edunexis-api/
├── EduNexis.slnx
├── Dockerfile
├── migration.sql                       # generated script for manual production migration
│
├── src/
│   ├── EduNexis.Domain/                # no external dependencies
│   │   ├── Common/                     # BaseEntity, auditing, soft delete
│   │   ├── Entities/                   # 32 entities
│   │   ├── Enums/                      # 14 enums
│   │   ├── Exceptions/                 # domain exceptions
│   │   └── Interfaces/
│   │       ├── Repositories/           # IUnitOfWork, ICourseRepository, …
│   │       └── Services/               # IEmailService, IFileStorageService, …
│   │
│   ├── EduNexis.Application/
│   │   ├── Abstractions/               # ICurrentUserService, ICourseScopedWrite, IAuthSettings
│   │   ├── Behaviors/                  # ArchivedCourseGuard, Validation, Logging
│   │   ├── Common/Slugs/               # public-profile slug generation
│   │   ├── DTOs/
│   │   ├── Extensions/                 # entity → DTO helpers
│   │   └── Features/                   # one folder per slice, Commands/ + Queries/
│   │       ├── Admin/          Announcements/   Assignments/
│   │       ├── Attendance/     Auth/            Courses/
│   │       ├── CT/             Marks/           Materials/
│   │       ├── Notifications/  Presentations/   Profile/
│   │       └── Public/
│   │
│   ├── EduNexis.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs         # DbSets + fluent config in OnModelCreating
│   │   │   ├── Migrations/             # 25 migrations
│   │   │   └── Repositories/           # base + per-aggregate, unit of work
│   │   └── Services/
│   │       ├── Auth/                   # JWT, BCrypt, OTP, reset tokens, current user
│   │       ├── Cache/                  # Redis
│   │       ├── Email/                  # Brevo sender + Scriban templates
│   │       ├── Sms/                    # generic HTTP gateway
│   │       └── Storage/                # Cloudinary
│   │
│   └── EduNexis.API/
│       ├── Program.cs                  # composition root
│       ├── Controllers/                # 14 controllers + BaseController
│       ├── Extensions/                 # ClaimsPrincipal helpers
│       ├── Middleware/                 # ExceptionMiddleware
│       └── Serialization/              # ISO-8601 UTC DateTime converters
│
└── tests/
    └── EduNexis.UnitTests/             # architecture + catalogue tests
```

---

## Configuration

`src/EduNexis.API/appsettings.json`, overridable by environment variables (`Jwt__Secret`, `ConnectionStrings__DefaultConnection`, …). **Do not commit real secrets** — the file is a template.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=edunexis_db;User=root;Password=yourpassword;",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Secret": "YOUR_SUPER_SECRET_MIN_256_BIT_KEY",
    "Issuer": "EduNexis",
    "Audience": "EduNexisUsers",
    "ExpiryMinutes": 60
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173", "https://edunexis.vercel.app" ]
  },
  "Cloudinary": {
    "CloudName": "your-cloud-name",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret",
    "DefaultCoverUrl": "https://res.cloudinary.com/..."
  },
  "Email": {
    "ApiKey": "your-brevo-api-key",
    "FromEmail": "no-reply@yourdomain",
    "FromName": "EduNexis"
  },
  "Sms": {
    "Endpoint": "https://bulksmsbd.net/api/smsapi?api_key={apiKey}&senderid={senderId}&number={number}&message={message}",
    "ApiKey": "your-key",
    "SenderId": "your-sender-id"
  },
  "PlagiarismServices": {
    "ZeroGptApiKey": "YOUR_ZEROGPT_API_KEY",
    "CopyleaksEmail": "",
    "CopyleaksApiKey": ""
  },
  "Database": {
    "RunMigrationsOnStartup": false
  }
}
```

| Key | Required | Notes |
|---|:---:|---|
| `ConnectionStrings:DefaultConnection` | ● | MySQL 8 |
| `Jwt:Secret` | ● | 256-bit minimum |
| `Cors:AllowedOrigins` | ● | Empty array blocks the browser client |
| `Cloudinary:*` | ● | Needed for any upload |
| `Email:ApiKey` | ○ | Without it OTP emails fail and accounts cannot verify |
| `ConnectionStrings:Redis` | ○ | Caching only |
| `Sms:*` | ○ | SMS notifications |
| `PlagiarismServices:*` | ○ | AI / web-plagiarism endpoints report "not configured" |

The port is taken from the `PORT` environment variable (default `5041`) and the host binds `0.0.0.0`, which is what container platforms expect.

---

## Running locally

### Prerequisites

- .NET SDK **10.0**
- MySQL **8.0+**
- Redis *(optional)*

### Steps

```bash
cd edunexis-api

# 1. configure src/EduNexis.API/appsettings.json (see above)

# 2. restore
dotnet restore EduNexis.slnx

# 3. create the schema
dotnet ef database update \
  --project src/EduNexis.Infrastructure \
  --startup-project src/EduNexis.API

# 4. run
dotnet run --project src/EduNexis.API
```

- API → `http://localhost:5041`
- Swagger → `http://localhost:5041/swagger`
- Health → `http://localhost:5041/health`

Point the front end at it with `VITE_API_BASE_URL=http://localhost:5041/api`.

---

## Database and migrations

25 EF Core migrations, applied by default **manually** rather than on startup.

```bash
# add a migration
dotnet ef migrations add MigrationName \
  --project src/EduNexis.Infrastructure \
  --startup-project src/EduNexis.API

# apply
dotnet ef database update \
  --project src/EduNexis.Infrastructure \
  --startup-project src/EduNexis.API

# generate an idempotent script for production
dotnet ef migrations script --idempotent \
  --project src/EduNexis.Infrastructure \
  --startup-project src/EduNexis.API \
  --output migration.sql
```

To apply migrations at boot instead, set `Database:RunMigrationsOnStartup=true` — see the caveat below before enabling it in production.

---

## Tests

```bash
dotnet test EduNexis.slnx
```

`EduNexis.UnitTests` holds **architecture and catalogue tests** rather than handler unit tests — the invariants that are cheap to break and expensive to notice:

| Test | Guards against |
|---|---|
| `Every_course_scoped_command_is_guarded_or_explicitly_exempt` | A new course write shipping without an authorisation guard |
| `Exempt_commands_state_a_reason` | Silent exemptions accumulating on the allowlist |
| `Coverage_test_actually_finds_commands` | The reflection scan quietly matching nothing and passing vacuously |
| `Every_notification_type_is_manageable_from_settings` | A notification users cannot turn off |
| `Catalogue_has_no_entry_for_a_type_that_no_longer_exists` | Stale catalogue entries after an enum change |
| `Catalogue_has_no_duplicates` | Duplicate settings rows |
| `Sms_is_a_strict_subset_of_email_eligibility` | SMS sent for something not even emailable |
| `Channel_eligible_types_all_appear_in_the_catalogue` | A channel-eligible type missing from settings |

---

## Deployment

A multi-stage `Dockerfile` sits in the repository root.

```bash
docker build -t edunexis-api .
docker run -d -p 5041:8080 \
  -e ConnectionStrings__DefaultConnection="Server=...;Database=...;User=...;Password=...;" \
  -e Jwt__Secret="..." \
  -e Cors__AllowedOrigins__0="https://edunexis.vercel.app" \
  --name edunexis-backend edunexis-api
```

The container respects `PORT`, so it drops onto Render, Railway, Fly or Azure App Service without changes. Configure the same keys as environment variables using the `__` separator for nesting.

---

## Known constraints and deliberate decisions

These are documented rather than hidden, because each one is a trade-off someone reading the code will otherwise trip over.

**`ValidationBehavior` is not registered.** It exists in `Behaviors/` but was never wired into the pipeline, so FluentValidation validators run only where a controller invokes them explicitly. Registering it globally would begin rejecting requests across the whole API at once and needs its own testing pass before being switched on. `LoggingBehavior` is unregistered for the same reason. Mediator's generated pipeline resolves behaviours from DI, so an unregistered behaviour simply never runs — it is not auto-discovered.

**Configuration file-watching is disabled.** `builder.Configuration.Sources.Clear()` then re-adding with `reloadOnChange: false` is intentional. Render's free tier has a low per-container inotify watch limit, and restarts under memory pressure were exhausting it, after which every subsequent boot crashed with *"configured user limit (128) on the number of inotify instances has been reached"* before the app could start. Config changes ship as full container redeploys anyway.

**Startup migrations are off by default.** Free Clever Cloud MySQL caps concurrent connections at **5**. Auto-migrating on every container restart competes with the still-running instance for that pool and crashes the new deploy. Production schema changes are applied from `migration.sql`.

**Email uses Brevo's HTTP API rather than SMTP** because Render's free tier blocks outbound SMTP ports. `FluentEmail.Smtp` is still referenced but the active sender is Brevo over HTTPS.

**There is no global EF query filter for soft deletes.** Both `IsDeleted` and `IsDeletedByOwner` are filtered explicitly per query. This is more error-prone than a global filter — a query that forgets one will leak deleted rows — but it keeps the two flags' different meanings visible at the call site.

**Cross-submission similarity runs client-side.** Parsing dozens of PDFs per assignment on a free-tier container is not viable, so extraction and scoring happen in the browser; only the resulting report is persisted.

---

## Related repository

The React front end lives in **`edunexis-web`**, which has its own README with a full feature walkthrough and screenshots.

---

<div align="center">

Built and maintained by **[Md. Sabbir Hossain Bappy](https://www.linkedin.com/in/snbappy/)** at the **[CyberSecurity Lab](https://nowsin.me/)**<br/>
Department of Computer Science and Engineering, Jashore University of Science and Technology

</div>
