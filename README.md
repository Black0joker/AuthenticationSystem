# IdentityAuth Server

A production-grade authentication and authorization server built with **ASP.NET Core 10 Web API**, demonstrating enterprise security patterns including JWT authentication, refresh token rotation, role-based access control, permission-based authorization, and comprehensive security hardening.

---

## Architecture

```
src/
    IdentityAuth.Api/            → Controllers, Middleware, Health Checks, Extensions
    IdentityAuth.Application/    → Services, DTOs, Interfaces (Business Logic)
    IdentityAuth.Domain/         → Entities, Enums, Exceptions
    IdentityAuth.Infrastructure/ → EF Core, Repositories, Security, Migrations

tests/
    IdentityAuth.UnitTests/          → 57 unit tests
    IdentityAuth.IntegrationTests/   → 95 integration tests
```

**Clean Architecture** with strict layer separation:
- **Domain** → No dependencies (pure entities and enums)
- **Application** → Depends on Domain (services, DTOs, interfaces)
- **Infrastructure** → Implements Application interfaces (EF Core, hashing, tokens)
- **API** → Thin controllers delegating to Application services

---

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Framework | ASP.NET Core 10 Web API |
| Language | C# 14 |
| ORM | Entity Framework Core 10 |
| Database | SQL Server 2022 |
| Authentication | JWT Bearer Tokens |
| Password Hashing | PBKDF2 (Rfc2898DeriveBytes) |
| Testing | xUnit + WebApplicationFactory |
| Containerization | Docker + Docker Compose |
| API Documentation | OpenAPI |

---

## Implementation Status

### Phase 1 — Project Foundation
- [x] Create ASP.NET Core 10 Web API
- [x] Create solution structure (Clean Architecture)
- [x] Configure EF Core
- [x] Configure database (SQL Server)
- [x] Add migrations
- [x] Configure Swagger/OpenAPI
- [x] Configure dependency injection
- [x] Configure centralized error handling (ExceptionHandlingMiddleware)
- [x] Add basic logging
- [x] Add environment-specific configuration

### Phase 2 — User Registration
- [x] Create User entity
- [x] Create User configuration (EF Core Fluent API)
- [x] Create User repository/data access
- [x] Add email normalization
- [x] Add registration DTO
- [x] Add validation (FluentValidation-style)
- [x] Implement password hashing (PBKDF2)
- [x] Implement duplicate email detection
- [x] Create registration service
- [x] Create register endpoint (`POST /api/auth/register`)
- [x] Add tests

### Phase 3 — Login
- [x] Implement password verification
- [x] Implement login service
- [x] Implement failed login tracking
- [x] Implement lockout (5 attempts → 15 min lock)
- [x] Add login endpoint (`POST /api/auth/login`)
- [x] Add security events
- [x] Add tests
- [x] Test brute-force scenarios

### Phase 4 — JWT
- [x] Configure JWT authentication
- [x] Create token service (IJwtTokenService)
- [x] Generate claims (sub, email, role, jti, given_name, family_name)
- [x] Configure issuer
- [x] Configure audience
- [x] Configure expiration (15 min access token)
- [x] Validate signatures
- [x] Add `jti`
- [x] Protect endpoints with `[Authorize]`
- [x] Add tests

### Phase 5 — Refresh Tokens
- [x] Create RefreshToken entity
- [x] Generate cryptographically random tokens
- [x] Hash refresh tokens before storage (SHA-256)
- [x] Store expiration (7 days)
- [x] Implement refresh endpoint (`POST /api/auth/refresh`)
- [x] Implement token rotation
- [x] Implement revocation
- [x] Detect reuse (token family tracking via ReplacedByTokenId)
- [x] Add token-family/session concepts
- [x] Add tests

### Phase 6 — Logout
- [x] Implement logout endpoint (`POST /api/auth/logout`)
- [x] Revoke refresh token
- [x] Add logout security event
- [x] Test revoked tokens

### Phase 7 — Email Verification
- [x] Create verification-token entity (EmailVerificationToken)
- [x] Generate random token
- [x] Hash token (SHA-256)
- [x] Implement email service abstraction (IEmailService)
- [x] Add development email provider (ConsoleEmailService)
- [x] Send verification email
- [x] Implement verification endpoint (`POST /api/auth/verify-email`)
- [x] Add expiration (24 hours)
- [x] Make tokens single-use
- [x] Add tests

### Phase 8 — Password Reset
- [x] Create password reset entity (PasswordResetToken)
- [x] Implement forgot-password endpoint (`POST /api/auth/forgot-password`)
- [x] Generate secure random token
- [x] Hash token (SHA-256)
- [x] Send reset email
- [x] Implement reset endpoint (`POST /api/auth/reset-password`)
- [x] Make token single-use
- [x] Add expiration (1 hour)
- [x] Revoke existing sessions after reset
- [x] Prevent account enumeration (generic response)
- [x] Add tests

### Phase 9 — Roles
- [x] Create Role entity
- [x] Create UserRole relationship (many-to-many)
- [x] Seed default roles (Admin, User)
- [x] Create role claims in JWT
- [x] Configure role authorization (`[Authorize(Policy = "AdminOnly")]`)
- [x] Create admin endpoint (`GET /api/profile/admin`)
- [x] Test role restrictions (403 for non-admin)

### Phase 10 — Permissions
- [x] Create Permission entity
- [x] Create RolePermission relationship (many-to-many)
- [x] Seed permissions (users.read, users.write, users.delete, roles.manage, security.audit, profile.read, profile.write)
- [x] Create role → permission assignments
- [x] Test authorization failures

### Phase 11 — Rate Limiting
- [x] Add ASP.NET Core rate limiting middleware
- [x] Create login policy (5 req/min per IP)
- [x] Create password-reset policy (5 req/min per IP)
- [x] Create registration policy (5 req/min per IP)
- [x] Create refresh policy (10 req/min per IP)
- [x] Create general policy (100 req/min per IP)
- [x] Create global fallback (200 req/min per IP)
- [x] Return 429 with Retry-After header
- [x] Environment-aware (disabled in Development/Test)

### Phase 12 — Health Checks, Correlation ID & Request Logging
- [x] Add health check endpoints (`/health/live`, `/health/ready`)
- [x] Add database health check
- [x] Implement CorrelationIdMiddleware (X-Correlation-Id)
- [x] Implement RequestLoggingMiddleware (structured logging with timing)
- [x] Add security headers middleware
- [x] Configure CORS

### Phase 13 — Database Migrations & Seed Data
- [x] Create SQL Server migration (InitialCreate)
- [x] Implement idempotent DatabaseSeeder
- [x] Seed roles (Admin, User)
- [x] Seed permissions (7 permissions)
- [x] Seed role-permission assignments
- [x] Seed default admin user
- [x] Configuration-driven auto-migrate/auto-seed

### Phase 14 — Docker Containerization & Deployment
- [x] Create multi-stage Dockerfile (build → publish → runtime)
- [x] Non-root container user
- [x] Docker HEALTHCHECK
- [x] docker-compose.yml (API + SQL Server 2022)
- [x] docker-compose.override.yml (development overrides)
- [x] .dockerignore
- [x] .env.example (secrets template)
- [x] Makefile (developer commands)
- [x] SQL Server health check via sqlcmd

---

## API Endpoints

### Authentication
| Method | Endpoint | Description | Rate Limit |
|--------|----------|-------------|-----------|
| POST | `/api/auth/register` | Register new user | 5/min |
| POST | `/api/auth/login` | Authenticate user | 5/min |
| POST | `/api/auth/refresh` | Refresh access token | 10/min |
| POST | `/api/auth/logout` | Revoke refresh token | 10/min |
| POST | `/api/auth/verify-email` | Verify email token | 10/min |
| POST | `/api/auth/forgot-password` | Request password reset | 5/min |
| POST | `/api/auth/reset-password` | Reset password with token | 10/min |

### Account (Authenticated)
| Method | Endpoint | Description | Rate Limit |
|--------|----------|-------------|-----------|
| POST | `/api/account/change-password` | Change password | 10/min |
| PUT | `/api/account/profile` | Update profile | 100/min |

### Profile (Authenticated)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/profile/me` | Get current user profile |
| GET | `/api/profile/admin` | Admin-only endpoint |

### Health
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health/live` | Liveness probe |
| GET | `/health/ready` | Readiness probe (DB check) |

---

## Security Features

### Authentication
- **JWT Access Tokens** — 15-minute lifetime, HMAC-SHA256 signing
- **Refresh Token Rotation** — Single-use tokens with family tracking
- **Reuse Detection** — Revoked token reuse triggers family revocation
- **Account Lockout** — 5 failed attempts → 15-minute lockout
- **Password Hashing** — PBKDF2 with 100,000 iterations, random salt
- **Security Stamp** — Invalidates tokens on security events

### Authorization
- **Role-Based** — Admin, User roles
- **Policy-Based** — `AdminOnly`, `UserOnly` policies
- **Permission Model** — Granular permissions (users.read, roles.manage, etc.)
- **Claims-Based** — JWT contains role and permission claims

### Protection
- **Rate Limiting** — Tiered per-IP limits on sensitive endpoints
- **Account Enumeration Prevention** — Generic responses on forgot-password
- **Security Headers** — X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy
- **CORS** — Explicit origin whitelist
- **Token Hashing** — All tokens stored as SHA-256 hashes
- **Audit Trail** — SecurityEvent entity logs all auth events

---

## Database Schema

```
Users
 ├── UserRoles ──── Roles
 │                    └── RolePermissions ──── Permissions
 ├── RefreshTokens
 ├── EmailVerificationTokens
 ├── PasswordResetTokens
 └── SecurityEvents
```

**SQL Server types:** `uniqueidentifier`, `nvarchar`, `datetime2`, `bit`, `int`

---

## Getting Started

### Prerequisites
- .NET 10 SDK
- SQL Server 2022 (local or Docker)
- Docker & Docker Compose (for containerized deployment)

### Local Development

```bash
# Restore and build
dotnet build IdentityAuthServer.slnx

# Run tests
dotnet test IdentityAuthServer.slnx

# Run the API
dotnet run --project src/IdentityAuth.Api
```

### Docker Deployment

```bash
# Configure secrets
cp .env.example .env

# Start all services (API + SQL Server)
docker compose up -d

# Verify
curl http://localhost:8080/health/live

# View logs
docker compose logs -f api
```

### Makefile Commands

```bash
make build          # Build solution
make test           # Run all tests
make run            # Run API locally
make docker-build   # Build Docker images
make docker-up      # Start containers
make docker-down    # Stop containers
make docker-clean   # Stop + remove volumes
make migrate        # Run EF migrations
make migration NAME=MigrationName  # Create new migration
```

---

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=IdentityAuthDb;User Id=sa;Password=...;TrustServerCertificate=True"
  },
  "JwtSettings": {
    "SecretKey": "...",
    "Issuer": "IdentityAuthServer",
    "Audience": "IdentityAuthClient",
    "AccessTokenExpirationMinutes": 15
  },
  "Database": {
    "AutoMigrate": true,
    "AutoSeed": true
  },
  "RateLimiting": {
    "Enabled": true
  }
}
```

### Environment Variables (Docker)
| Variable | Description |
|----------|-------------|
| `SQLSERVER_PASSWORD` | SQL Server SA password |
| `JWT_SECRET_KEY` | JWT signing key (≥32 chars) |
| `CORS_ALLOWED_ORIGINS` | Allowed CORS origins |

---

## Testing

### Test Coverage
- **57 unit tests** — Password hashing, JWT generation, token validation, password policies, lockout logic
- **95 integration tests** — Full HTTP pipeline via WebApplicationFactory (InMemory DB)

### Test Categories
| Category | Tests |
|----------|-------|
| Registration | Valid, duplicate email, weak password, invalid email, missing fields |
| Login | Valid, wrong password, locked account, unverified email, empty fields |
| JWT | Valid token, expired, invalid signature, wrong audience/issuer |
| Refresh Tokens | Rotation, reuse detection, expiry, revocation |
| Email Verification | Valid token, expired, used, invalid |
| Password Reset | Full flow, enumeration prevention, expiry |
| Authorization | Role-based, admin-only, forbidden |
| Account Management | Change password, update profile |
| Security Events | Login events, logout, token events |

### Run Tests
```bash
dotnet test IdentityAuthServer.slnx --verbosity minimal
```

---

## Security Audit Trail

All authentication events are recorded in the `SecurityEvents` table:

| Event Type | Trigger |
|-----------|--------|
| UserRegistered | Successful registration |
| LoginSucceeded | Successful login |
| LoginFailed | Failed login attempt |
| AccountLocked | Lockout threshold reached |
| EmailVerified | Email verification completed |
| PasswordChanged | Password change |
| PasswordResetRequested | Forgot password |
| PasswordResetCompleted | Reset password |
| RefreshTokenCreated | New refresh token issued |
| RefreshTokenRevoked | Token revoked (logout/reuse) |
| RefreshTokenReuseDetected | Reuse of revoked token |
| Logout | User logout |

---

## Default Credentials (Development Only)

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@identityauth.com | Admin@123456 |

> ⚠️ Change these credentials before deploying to production.

---

## Pipeline Order

```
CorrelationId → RequestLogging → ExceptionHandling → SecurityHeaders
    → HTTPS → CORS → RateLimiting → Authentication → Authorization
    → HealthChecks → Controllers
```

---

## License

This project is for educational purposes.
