# CRM Backend (.NET 8)

Customer Relationship Management API for the Cognizant *Upgrade to Architect* case study.

Clean Architecture, JWT auth, EF Core on **Azure SQL / SQL Server**,
Serilog, FluentValidation, rate limiting, health checks, Swagger.

## Solution layout

```
backend/
├── src/
│   ├── CRM.Domain/          Entities, enums, domain exceptions
│   ├── CRM.Application/     DTOs, services, validators, mappers, abstractions
│   ├── CRM.Infrastructure/  EF Core DbContext, JWT, BCrypt, interceptors, migrations
│   └── CRM.Api/             Controllers, middleware, Program.cs, DI
└── tests/
    ├── CRM.UnitTests/
    └── CRM.IntegrationTests/
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An Azure SQL database (or local SQL Server / LocalDB) reachable from your machine.
  Allow your client IP in the Azure SQL **Firewall rules**.

## Quick start

```bash
# from backend/src/CRM.Api/
dotnet user-secrets set "ConnectionStrings:Default" "<your-azure-sql-connection-string>"
dotnet user-secrets set "Jwt:SigningKey" "<at-least-32-bytes>"

# from backend/
dotnet restore
dotnet run --project src/CRM.Api/CRM.Api.csproj
```

The API starts on `http://localhost:5171` (see `src/CRM.Api/Properties/launchSettings.json`).
On first run it applies pending EF migrations and seeds the demo accounts.

- Swagger UI: <http://localhost:5171/swagger>
- Health:     <http://localhost:5171/health>

### Seeded users (dev only)

| Username    | Password       | Role  |
|-------------|----------------|-------|
| `admin`     | `ChangeMe!123` | Admin |
| `demo.user` | `ChangeMe!123` | User  |

Change the seed password via `Admin:SeedPassword` in user-secrets before first run.

## Configuration

Settings live in `src/CRM.Api/appsettings.json` and `appsettings.Development.json`.
**Never commit secrets** — use `dotnet user-secrets` instead:

```bash
cd src/CRM.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)"
dotnet user-secrets set "ConnectionStrings:Default" "Server=tcp:<server>.database.windows.net,1433;Initial Catalog=CRMDb;User ID=<user>;Password=<pwd>;Encrypt=True;Connection Timeout=30"
dotnet user-secrets set "Admin:SeedPassword" "<strong-password>"
```

### Connection string examples

| Target               | Example |
|----------------------|---------|
| SQL Server / LocalDB | `Server=(localdb)\\MSSQLLocalDB;Database=CRM_Dev;Trusted_Connection=True;TrustServerCertificate=True` |
| Azure SQL            | `Server=tcp:<server>.database.windows.net,1433;Initial Catalog=CRM_Dev;User ID=<u>;Password=<p>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30` |

### Generating a new migration

```bash
# Use the DesignTimeDbContextFactory by exporting your dev connection string:
export CRM_DB_CONNECTION="<your-azure-sql-connection-string>"

dotnet ef migrations add <Name> \
  --project src/CRM.Infrastructure \
  --startup-project src/CRM.Infrastructure \
  --output-dir Persistence/Migrations
```

## Tests + coverage

```bash
dotnet test
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

## API surface

Base URL: `/api`

| Method | Endpoint                     | Auth         |
|--------|------------------------------|--------------|
| POST   | `/api/auth/login`            | anonymous    |
| POST   | `/api/auth/signup`           | anonymous    |
| GET    | `/api/auth/roles`            | JWT          |
| GET    | `/api/customers`             | JWT          |
| POST   | `/api/customers`             | JWT          |
| GET    | `/api/customers/{id}`        | JWT          |
| PUT    | `/api/customers/{id}`        | JWT + Admin  |
| DELETE | `/api/customers/{id}`        | JWT + Admin  |
| GET    | `/api/sales`                 | JWT          |
| POST   | `/api/sales`                 | JWT          |
| GET    | `/api/sales/{id}`            | JWT          |
| PUT    | `/api/sales/{id}`            | JWT          |
| DELETE | `/api/sales/{id}`            | JWT + Admin  |
| GET    | `/api/sales/export`          | JWT + Admin  |
| GET    | `/api/dashboard`             | JWT          |
| GET    | `/health`                    | anonymous    |

## Security highlights

- Passwords hashed with **BCrypt** (work factor 12)
- JWT HS256 with configurable signing key (min 32 bytes) and clock-skew
- Brute-force lockout: 5 failed logins → 15-minute lockout
- Constant-time login (dummy BCrypt verify on unknown user) to mitigate user-enumeration
- **RFC 7807** ProblemDetails on every error; correlation IDs surfaced via `X-Correlation-ID`
- IP rate limiting: login 10/min, signup 5/min, global 200/min
- Soft delete + audit trail (`CreatedAt/By`, `UpdatedAt/By`, `DeletedAt/By`) via EF Core SaveChanges interceptor
- Concurrency control via `RowVersion`

## License

Internal Cognizant case-study project — not for redistribution.
