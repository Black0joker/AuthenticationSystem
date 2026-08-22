# Identity/Auth Server — ASP.NET Core 10

## 1. Project Goal

Build a production-style authentication and authorization server using **ASP.NET Core 10 Web API**.

The project should not simply implement login and registration. It should demonstrate how a real backend handles:

* User registration
* Password hashing
* Login
* JWT access tokens
* Refresh tokens
* Token rotation
* Logout
* Token revocation
* Email verification
* Password reset
* Roles
* Permissions
* Role-based authorization
* Policy-based authorization
* Account locking
* Secure cookies
* HttpOnly cookies
* CSRF protection
* Token expiration
* Authentication vs authorization
* Claims
* Security auditing

The objective is to understand **why authentication systems are designed this way**, not just how to make endpoints work.

---

# 2. Recommended Architecture

Use a modular architecture rather than putting everything inside controllers.

```text
IdentityAuthServer
│
├── API
│   ├── Controllers
│   ├── Middleware
│   ├── Filters
│   └── Extensions
│
├── Application
│   ├── Authentication
│   ├── Users
│   ├── Roles
│   ├── Permissions
│   ├── PasswordReset
│   ├── EmailVerification
│   └── Common
│
├── Domain
│   ├── Entities
│   ├── Enums
│   ├── ValueObjects
│   └── Exceptions
│
├── Infrastructure
│   ├── Persistence
│   ├── Authentication
│   ├── Email
│   ├── Tokens
│   └── Security
│
└── Tests
    ├── Unit
    ├── Integration
    └── Security
```

A possible solution:

```text
src/
    IdentityAuth.Api/
    IdentityAuth.Application/
    IdentityAuth.Domain/
    IdentityAuth.Infrastructure/

tests/
    IdentityAuth.UnitTests/
    IdentityAuth.IntegrationTests/
```

---

# 3. Technology Stack

## Backend

* ASP.NET Core 10 Web API
* C#
* Entity Framework Core
* PostgreSQL or SQL Server
* ASP.NET Core Authentication/Authorization
* JWT Bearer authentication
* Dependency Injection
* Options pattern
* Middleware
* Policy-based authorization

## Development

* Swagger / OpenAPI
* Docker
* Docker Compose
* xUnit
* Integration testing with `WebApplicationFactory`
* Test database
* Logging

## Optional infrastructure

```text
PostgreSQL
Redis
MailHog / SMTP test server
Docker
```

Redis should be considered optional initially. Do not introduce it until the basic authentication system works.

---

# 4. Authentication Model

The system should use:

```text
Short-lived Access Token
        +
Long-lived Refresh Token
```

Example:

```text
Access Token
    ↓
JWT
    ↓
15 minutes

Refresh Token
    ↓
Opaque random token
    ↓
7–30 days
```

Do **not** make the refresh token a long-lived JWT just because JWT is being used for access tokens.

A useful design is:

```text
Access token → JWT

Refresh token → cryptographically random opaque value
```

The refresh token should be stored securely server-side, preferably as a hash.

---

# 5. Authentication vs Authorization

This distinction should be understood before implementation.

## Authentication

Answers:

> Who are you?

Example:

```text
User logs in
    ↓
Credentials verified
    ↓
Authentication succeeds
```

## Authorization

Answers:

> What are you allowed to do?

Example:

```text
Authenticated user
        ↓
Has Admin role?
        ↓
Has users.read permission?
        ↓
Allow / Deny
```

The project should demonstrate both concepts independently.

---

# 6. Domain Model

Start with the following entities.

## User

```text
User
----------------
Id
Email
NormalizedEmail
PasswordHash
FirstName
LastName
IsEmailVerified
IsLocked
LockoutEnd
FailedLoginAttempts
CreatedAt
UpdatedAt
LastLoginAt
SecurityStamp
```

The exact fields can evolve during implementation.

Important:

Never store:

```text
Password
```

Store only:

```text
PasswordHash
```

---

# 7. Role Model

Create:

```text
Role
----------------
Id
Name
NormalizedName
Description
CreatedAt
```

Relationships:

```text
User
  │
  └── UserRole
          │
          └── Role
```

A user can have multiple roles.

Example:

```text
User
 ├── Admin
 └── Support
```

---

# 8. Permission Model

Create:

```text
Permission
----------------
Id
Name
Description
```

Examples:

```text
users.read
users.create
users.update
users.delete

roles.read
roles.create
roles.update
roles.delete

reports.read
reports.export
```

Then:

```text
Role
  │
  └── RolePermission
          │
          └── Permission
```

The final authorization structure becomes:

```text
User
 ↓
UserRole
 ↓
Role
 ↓
RolePermission
 ↓
Permission
```

This allows:

```text
Admin
    users.read
    users.create
    users.update
    users.delete

Support
    users.read
    users.update
```

---

# 9. Refresh Token Model

Create a database entity similar to:

```text
RefreshToken
----------------
Id
UserId
TokenHash
ExpiresAt
CreatedAt
RevokedAt
ReplacedByTokenId
CreatedByIp
RevokedByIp
```

This enables token rotation and revocation.

Example:

```text
Refresh Token A
      ↓
used
      ↓
revoked
      ↓
Refresh Token B created
```

If an old token is reused, the server should treat that as suspicious token reuse.

---

# 10. Email Verification Model

Create something similar to:

```text
EmailVerificationToken
----------------
Id
UserId
TokenHash
ExpiresAt
CreatedAt
UsedAt
```

Flow:

```text
Register
   ↓
Create user
   ↓
Generate random verification token
   ↓
Hash token
   ↓
Store hash
   ↓
Send verification email
   ↓
User clicks link
   ↓
Verify token
   ↓
Mark email verified
```

The raw token should not need to be stored in the database.

---

# 11. Password Reset Model

Create:

```text
PasswordResetToken
----------------
Id
UserId
TokenHash
ExpiresAt
CreatedAt
UsedAt
```

Flow:

```text
POST /forgot-password
        ↓
Find account
        ↓
Generate random token
        ↓
Store hash
        ↓
Send email
```

Then:

```text
POST /reset-password
        ↓
Validate token
        ↓
Validate expiration
        ↓
Hash new password
        ↓
Update user
        ↓
Invalidate reset token
        ↓
Revoke existing sessions/tokens
```

---

# 12. Password Hashing

Never implement password hashing yourself.

Use a mature password hashing implementation.

The system should support:

```text
Password
   ↓
Password Hasher
   ↓
Password Hash
```

During login:

```text
Password
   ↓
Verify against stored hash
   ↓
Success / Failure
```

The project should teach:

* Hashing vs encryption
* Salt
* Work factor
* Password verification
* Password hash upgrading
* Why passwords should never be reversible
* Why plaintext passwords must never be stored

---

# 13. Registration

Endpoint:

```http
POST /api/auth/register
```

Request:

```json
{
  "email": "user@example.com",
  "password": "StrongPassword123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

Flow:

```text
Request
  ↓
Validate input
  ↓
Normalize email
  ↓
Check duplicate account
  ↓
Validate password
  ↓
Hash password
  ↓
Create User
  ↓
Assign default role
  ↓
Create email verification token
  ↓
Send verification email
  ↓
Return response
```

Do not automatically expose sensitive account information.

---

# 14. Login

Endpoint:

```http
POST /api/auth/login
```

Request:

```json
{
  "email": "user@example.com",
  "password": "StrongPassword123!"
}
```

Flow:

```text
Login request
     ↓
Find user
     ↓
Check account lock
     ↓
Verify password
     ↓
Check email verification
     ↓
Reset failed attempts
     ↓
Create access token
     ↓
Create refresh token
     ↓
Store refresh token hash
     ↓
Return authentication result
```

Failed login:

```text
Invalid password
      ↓
Increment FailedLoginAttempts
      ↓
Threshold reached?
      ↓
Lock account
```

---

# 15. Account Locking

Implement progressive protection against brute-force attacks.

Example policy:

```text
5 failed attempts
        ↓
Account locked
        ↓
15-minute lockout
```

The exact values should be configurable.

Configuration:

```text
Authentication:
    MaxFailedLoginAttempts
    LockoutDuration
```

The project should explain:

* Brute-force attacks
* Account enumeration
* Lockout abuse
* Rate limiting
* Temporary vs permanent lockout

Account locking should eventually be combined with API rate limiting.

---

# 16. JWT Access Token

The access token should contain appropriate claims.

Example conceptual payload:

```json
{
  "sub": "user-id",
  "email": "user@example.com",
  "jti": "token-id",
  "role": "Admin",
  "permissions": [
    "users.read",
    "users.update"
  ],
  "iss": "identity-auth-server",
  "aud": "identity-api",
  "iat": 1750000000,
  "exp": 1750000900
}
```

Learn each claim:

```text
sub
iss
aud
exp
iat
jti
role
```

Do not put unnecessary sensitive information inside JWTs.

JWT payloads are encoded, not encrypted.

---

# 17. Access Token Lifetime

Keep access tokens short-lived.

Example:

```text
Access Token:
15 minutes
```

The exact lifetime should be configurable.

Concept:

```text
Access token expires
        ↓
Client sends refresh token
        ↓
Server validates refresh token
        ↓
New access token
```

This limits the damage caused by an exposed access token.

---

# 18. Refresh Token Rotation

Endpoint:

```http
POST /api/auth/refresh
```

Flow:

```text
Client
  ↓
Refresh Token A
  ↓
Server validates token
  ↓
Token A revoked
  ↓
Token B created
  ↓
New Access Token created
  ↓
Return Token B
```

Database:

```text
Token A
   │
   ├── RevokedAt
   └── ReplacedBy → Token B
```

Never allow the same refresh token to be used indefinitely.

---

# 19. Refresh Token Reuse Detection

This is an important advanced feature.

Suppose:

```text
Refresh Token A
       ↓
used legitimately
       ↓
revoked
       ↓
Token B created
```

An attacker later obtains Token A and tries to use it.

The server sees:

```text
Token A = already revoked
```

This may indicate token theft.

The system should be designed to respond by revoking the relevant token family/session.

Concept:

```text
Token Family
     │
     ├── Token A
     ├── Token B
     └── Token C
```

If reuse is detected, revoke the appropriate token family.

---

# 20. Logout

Endpoint:

```http
POST /api/auth/logout
```

Logout should not simply mean:

```text
Delete JWT from browser
```

The server should revoke the refresh token/session.

Flow:

```text
Logout
   ↓
Identify refresh token/session
   ↓
Revoke token
   ↓
Clear authentication cookie if used
```

Because access tokens are stateless, an already-issued access JWT may remain valid until expiration unless additional server-side revocation mechanisms are introduced.

This is one reason access tokens should be short-lived.

---

# 21. Token Revocation

Implement explicit revocation.

Possible reasons:

```text
Logout
Password reset
Password change
Admin revocation
Account compromise
Refresh token reuse
Account deletion
Security event
```

Consider a token/session model that allows:

```text
RevokeSession(userId)
RevokeRefreshToken(tokenId)
RevokeAllSessions(userId)
```

For example:

```text
POST /api/auth/logout-all
```

can be added later.

---

# 22. Cookies

Study two possible approaches.

## Option A — Access token in Authorization header

```http
Authorization: Bearer <access-token>
```

Refresh token can be stored in a secure HttpOnly cookie.

## Option B — Cookie-based authentication

Use secure cookies for authentication state.

The project should understand:

```text
HttpOnly
Secure
SameSite
Domain
Path
Expiration
```

A secure refresh-token cookie should generally be:

```text
HttpOnly
Secure
SameSite
```

with an appropriately restricted path.

---

# 23. HttpOnly

Understand why:

```text
HttpOnly
```

prevents JavaScript from directly reading a cookie.

This reduces exposure of sensitive tokens to some XSS scenarios.

But:

```text
HttpOnly ≠ XSS protection
```

XSS must still be prevented.

---

# 24. CSRF

If authentication relies on cookies, learn CSRF carefully.

The browser automatically sends cookies.

Therefore:

```text
Cookie authentication
        ↓
CSRF becomes relevant
```

Study:

* SameSite cookies
* Anti-forgery tokens
* CSRF attack flow
* Origin checking
* Same-origin policy
* CORS vs CSRF

Important distinction:

```text
CORS ≠ CSRF protection
```

---

# 25. Email Verification

Endpoint:

```http
GET /api/auth/verify-email?token=...
```

or:

```http
POST /api/auth/verify-email
```

Flow:

```text
User registers
      ↓
Email verification required
      ↓
User receives email
      ↓
Clicks link
      ↓
Token validated
      ↓
Email marked verified
```

Add:

```http
POST /api/auth/resend-verification
```

with appropriate rate limiting.

---

# 26. Password Reset

Endpoints:

```http
POST /api/auth/forgot-password
POST /api/auth/reset-password
```

Forgot-password response should not reveal whether an email exists.

Bad:

```text
User does not exist.
```

Better:

```text
If an account exists, a password reset email has been sent.
```

This prevents account enumeration.

Reset token should:

* Be cryptographically random
* Be short-lived
* Be single-use
* Be stored hashed
* Be invalidated after successful use

---

# 27. Current User

Endpoint:

```http
GET /api/users/me
```

Flow:

```text
Request
  ↓
JWT authentication
  ↓
Extract sub claim
  ↓
Find user
  ↓
Return safe user information
```

Response:

```json
{
  "id": "...",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "roles": [
    "User"
  ],
  "permissions": [
    "profile.read"
  ]
}
```

Never return:

```text
PasswordHash
Security secrets
RefreshTokenHash
Internal security metadata
```

---

# 28. Role-Based Authorization

Example:

```csharp
[Authorize(Roles = "Admin")]
```

Endpoint:

```http
GET /api/admin/users
```

Only users with:

```text
Admin
```

can access it.

Understand:

```text
Authentication
     ↓
User authenticated
     ↓
Authorization
     ↓
Role checked
```

---

# 29. Policy-Based Authorization

Move beyond roles.

Example policy:

```text
RequirePermission("users.read")
```

Conceptually:

```text
[Authorize(Policy = "users.read")]
```

Create a custom authorization requirement:

```text
PermissionRequirement
```

and an authorization handler:

```text
PermissionAuthorizationHandler
```

The handler should determine whether the current user possesses the required permission.

---

# 30. Claims

Learn the relationship:

```text
User
 ↓
Authentication
 ↓
ClaimsPrincipal
 ↓
Claims
 ↓
Authorization
```

Useful claims:

```text
sub
email
role
permission
jti
```

Understand:

* Claims are statements about an identity.
* Roles can be represented as claims.
* Permissions can be represented as claims.
* Authorization policies can inspect claims.

---

# 31. Authorization Strategy

Use multiple layers.

```text
Authentication
      ↓
Role authorization
      ↓
Permission authorization
      ↓
Resource authorization
```

Example:

```text
Admin
    users.delete
        ↓
Can delete users

But:
    Can Admin delete THIS user?
        ↓
Resource-level authorization
```

This introduces the concept of **resource-based authorization**.

---

# 32. CORS

Configure CORS explicitly.

Do not use:

```text
AllowAnyOrigin
AllowAnyHeader
AllowAnyMethod
```

blindly in production.

Configure trusted frontend origins.

Example:

```text
https://app.example.com
```

Study:

```text
CORS
Origin
Credentials
Preflight
OPTIONS
SameSite
```

---

# 33. Security Headers

Introduce appropriate security headers.

Study:

```text
Content-Security-Policy
X-Content-Type-Options
Referrer-Policy
Permissions-Policy
```

Some headers may be better handled by the reverse proxy depending on deployment architecture.

---

# 34. Rate Limiting

Add rate limiting to sensitive endpoints.

Especially:

```text
/register
/login
/forgot-password
/reset-password
/resend-verification
/refresh
```

Example conceptual policy:

```text
Login:
5 attempts / minute / IP
```

Do not hard-code these values into business logic.

Make them configurable.

---

# 35. Account Enumeration Protection

The system should avoid revealing whether an account exists.

Sensitive endpoints include:

```text
Register
Forgot password
Email verification
Login
```

For example:

```text
POST /forgot-password
```

should produce a generic response.

Also consider timing differences where appropriate.

---

# 36. Database Design

Initial database relationship:

```text
Users
 │
 ├── UserRoles ── Roles
 │                  │
 │                  └── RolePermissions ── Permissions
 │
 ├── RefreshTokens
 │
 ├── EmailVerificationTokens
 │
 └── PasswordResetTokens
```

Later:

```text
Users
 │
 └── Sessions
        │
        ├── RefreshTokens
        └── SecurityEvents
```

Use indexes for:

```text
NormalizedEmail
RefreshTokenHash
PasswordResetTokenHash
EmailVerificationTokenHash
```

and appropriate foreign keys.

---

# 37. Security Events / Audit Log

Add an audit/security event entity.

Example:

```text
SecurityEvent
----------------
Id
UserId
EventType
IpAddress
UserAgent
CreatedAt
Metadata
```

Events:

```text
UserRegistered
LoginSucceeded
LoginFailed
AccountLocked
PasswordChanged
PasswordResetRequested
PasswordResetCompleted
EmailVerified
RefreshTokenCreated
RefreshTokenRevoked
RefreshTokenReuseDetected
Logout
```

This makes the project much closer to a real authentication system.

---

# 38. Session Management

Eventually introduce sessions.

Example:

```text
User
 │
 ├── Session A
 │      └── Refresh Token
 │
 ├── Session B
 │      └── Refresh Token
 │
 └── Session C
        └── Refresh Token
```

Then support:

```text
Current session
All sessions
Revoke session
Revoke all sessions
```

This is more useful than thinking about refresh tokens as isolated strings.

---

# 39. API Endpoint Map

## Authentication

```http
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
POST /api/auth/logout-all
```

## Email

```http
POST /api/auth/verify-email
POST /api/auth/resend-verification
```

## Password

```http
POST /api/auth/forgot-password
POST /api/auth/reset-password
POST /api/auth/change-password
```

## User

```http
GET /api/users/me
```

## Administration

```http
GET    /api/admin/users
GET    /api/admin/users/{id}
POST   /api/admin/users/{id}/lock
POST   /api/admin/users/{id}/unlock
POST   /api/admin/users/{id}/revoke-sessions
```

## Roles

```http
GET    /api/admin/roles
POST   /api/admin/roles
PUT    /api/admin/roles/{id}
DELETE /api/admin/roles/{id}
```

## Permissions

```http
GET    /api/admin/permissions
POST   /api/admin/roles/{id}/permissions
DELETE /api/admin/roles/{id}/permissions/{permissionId}
```

Do not implement all administrative endpoints at once. Add them after the core authentication flow works.

---

# 40. Suggested Response Model

Standardize API responses.

For validation:

```json
{
  "type": "validation_error",
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "email": [
      "Invalid email address."
    ]
  }
}
```

Use ASP.NET Core's problem-details approach rather than inventing a completely different error protocol.

---

# 41. Exception Handling

Implement centralized exception handling.

Architecture:

```text
Controller
    ↓
Application
    ↓
Exception
    ↓
Global exception handler
    ↓
ProblemDetails
```

Never expose:

```text
Stack traces
Database errors
Internal exceptions
Secrets
```

to clients in production.

---

# 42. Configuration

Create strongly typed configuration.

Example:

```text
JwtOptions
PasswordOptions
RefreshTokenOptions
LockoutOptions
EmailOptions
CorsOptions
```

Configuration should include things such as:

```text
Issuer
Audience
Signing configuration
AccessTokenLifetime
RefreshTokenLifetime
LockoutDuration
MaxFailedAttempts
FrontendUrl
```

Secrets should not be committed to Git.

Use:

```text
User Secrets
Environment Variables
Secret Manager
Vault
```

depending on deployment.

---

# 43. Key Management

Do not treat JWT signing keys as ordinary application configuration.

Learn:

```text
Symmetric signing
Asymmetric signing
Key rotation
Key storage
Key identifiers
```

For the learning project, a symmetric key can be acceptable initially.

Later, experiment with:

```text
RSA / ECDSA
```

and understand why production identity infrastructure may use asymmetric signing.

---

# 44. Token Claims vs Database State

A major learning objective:

JWTs are useful because they can be validated without querying the database for every request.

But that creates a problem:

```text
JWT says:
role = Admin
```

while the database may now say:

```text
User role = User
```

Therefore learn the trade-off between:

```text
Stateless authorization
```

and:

```text
Centralized/revocable authorization
```

This is one of the most important concepts in the project.

---

# 45. Security Stamp / Session Version

Introduce a mechanism for invalidating old authentication state.

For example:

```text
User.SecurityStamp
```

or a session/security version.

When security-sensitive events occur:

```text
Password changed
Password reset
Account compromised
All sessions revoked
```

increment/change the security version.

The exact implementation can vary depending on the token architecture.

---

# 46. Project Implementation Phases

## Phase 1 — Project Foundation

* [ ] Create ASP.NET Core 10 Web API
* [ ] Create solution structure
* [ ] Configure EF Core
* [ ] Configure database
* [ ] Add migrations
* [ ] Configure Swagger/OpenAPI
* [ ] Configure dependency injection
* [ ] Configure centralized error handling
* [ ] Add basic logging
* [ ] Add environment-specific configuration

Goal:

```text
Empty API
   ↓
Database connected
   ↓
Clean architecture ready
```

---

# 47. Phase 2 — User Registration

* [ ] Create User entity
* [ ] Create User configuration
* [ ] Create User repository/data access
* [ ] Add email normalization
* [ ] Add registration DTO
* [ ] Add validation
* [ ] Implement password hashing
* [ ] Implement duplicate email detection
* [ ] Create registration service
* [ ] Create register endpoint
* [ ] Add tests

Goal:

```text
POST /api/auth/register
```

works securely.

---

# 48. Phase 3 — Login

* [ ] Implement password verification
* [ ] Implement login service
* [ ] Implement failed login tracking
* [ ] Implement lockout
* [ ] Add login endpoint
* [ ] Add security events
* [ ] Add tests
* [ ] Test brute-force scenarios

Goal:

```text
POST /api/auth/login
```

works.

---

# 49. Phase 4 — JWT

* [ ] Learn JWT structure
* [ ] Configure JWT authentication
* [ ] Create token service
* [ ] Generate claims
* [ ] Configure issuer
* [ ] Configure audience
* [ ] Configure expiration
* [ ] Validate signatures
* [ ] Add `jti`
* [ ] Protect endpoints with `[Authorize]`
* [ ] Add tests

Goal:

```text
Login
  ↓
Access Token
  ↓
Authorization: Bearer ...
  ↓
Protected API
```

---

# 50. Phase 5 — Refresh Tokens

* [ ] Create RefreshToken entity
* [ ] Generate cryptographically random tokens
* [ ] Hash refresh tokens before storage
* [ ] Store expiration
* [ ] Implement refresh endpoint
* [ ] Implement token rotation
* [ ] Implement revocation
* [ ] Detect reuse
* [ ] Add token-family/session concepts
* [ ] Add tests

Goal:

```text
Access Token expires
        ↓
Refresh Token
        ↓
New Access Token
```

---

# 51. Phase 6 — Logout

* [ ] Implement logout
* [ ] Revoke refresh token
* [ ] Clear authentication cookie where applicable
* [ ] Add logout security event
* [ ] Implement logout-all
* [ ] Test revoked tokens

Goal:

```text
POST /api/auth/logout
```

actually invalidates the session rather than merely deleting something on the client.

---

# 52. Phase 7 — Email Verification

* [ ] Create verification-token entity
* [ ] Generate random token
* [ ] Hash token
* [ ] Implement email service abstraction
* [ ] Add development email provider
* [ ] Send verification email
* [ ] Implement verification endpoint
* [ ] Implement resend endpoint
* [ ] Add expiration
* [ ] Make tokens single-use
* [ ] Add rate limiting
* [ ] Add tests

Use a local mail server such as MailHog during development.

---

# 53. Phase 8 — Password Reset

* [ ] Create password reset entity
* [ ] Implement forgot-password endpoint
* [ ] Generate secure random token
* [ ] Hash token
* [ ] Send reset email
* [ ] Implement reset endpoint
* [ ] Make token single-use
* [ ] Add expiration
* [ ] Revoke existing sessions after reset
* [ ] Prevent account enumeration
* [ ] Add tests

---

# 54. Phase 9 — Roles

* [ ] Create Role entity
* [ ] Create UserRole relationship
* [ ] Seed default roles
* [ ] Create role claims
* [ ] Configure role authorization
* [ ] Create admin endpoint
* [ ] Test role restrictions

Initial roles:

```text
User
Admin
Support
```

---

# 55. Phase 10 — Permissions

* [ ] Create Permission entity
* [ ] Create RolePermission relationship
* [ ] Seed permissions
* [ ] Create permission requirement
* [ ] Create authorization handler
* [ ] Register policies
* [ ] Create permission-based endpoints
* [ ] Test authorization failures
* [ ] Test role → permission inheritance

Goal:

```text
Role
 ↓
Permissions
 ↓
Policy
 ↓
Endpoint
```

---

# 56. Phase 11 — Cookies and CSRF

* [ ] Understand browser cookie behavior
* [ ] Configure HttpOnly
* [ ] Configure Secure
* [ ] Configure SameSite
* [ ] Decide access-token transport
* [ ] Store refresh token securely
* [ ] Understand CSRF
* [ ] Add CSRF protection where required
* [ ] Configure CORS
* [ ] Test cross-origin behavior

Do not simply copy cookie settings from a tutorial. Understand why each flag exists.

---

# 57. Phase 12 — Rate Limiting

* [ ] Add ASP.NET Core rate limiting
* [ ] Create login policy
* [ ] Create password-reset policy
* [ ] Create registration policy
* [ ] Create refresh policy
* [ ] Test rate-limit behavior
* [ ] Return appropriate status codes

---

# 58. Phase 13 — Security Hardening

* [ ] Review password policy
* [ ] Review account lockout
* [ ] Review token lifetimes
* [ ] Review refresh-token rotation
* [ ] Review token revocation
* [ ] Review CSRF protection
* [ ] Review CORS
* [ ] Review security headers
* [ ] Review sensitive logging
* [ ] Review error responses
* [ ] Review account enumeration
* [ ] Review secrets
* [ ] Review JWT signing keys
* [ ] Review database indexes
* [ ] Review audit logs

---

# 59. Phase 14 — Testing

Build three levels of tests.

## Unit tests

Test:

```text
Password hashing
Token generation
Token validation
Password reset logic
Lockout logic
Permission evaluation
```

## Integration tests

Test:

```text
Register
Login
Refresh
Logout
Verify email
Forgot password
Reset password
Authorization
```

## Security tests

Test:

```text
Expired JWT
Invalid JWT
Wrong audience
Wrong issuer
Invalid signature
Revoked refresh token
Refresh-token reuse
Locked account
Invalid password
CSRF
CORS
Rate limiting
Account enumeration
Privilege escalation
```

---

# 60. Important Test Scenarios

At minimum:

### Registration

```text
Valid registration → success
Duplicate email → safe error
Weak password → validation error
Invalid email → validation error
```

### Login

```text
Correct password → success
Wrong password → failure
Repeated failures → lockout
Locked account → denied
Unverified account → appropriate behavior
```

### Access token

```text
Valid token → success
Expired token → unauthorized
Invalid signature → unauthorized
Wrong audience → unauthorized
Wrong issuer → unauthorized
```

### Refresh token

```text
Valid token → new token
Expired token → denied
Revoked token → denied
Reused token → security response
```

### Authorization

```text
User → normal endpoint
User → admin endpoint → denied
Admin → admin endpoint → allowed
Support → users.read → allowed
Support → users.delete → denied
```

---

# 61. Security Threats to Study

The project should explicitly teach protection against:

```text
Brute-force attacks
Credential stuffing
Password spraying
Account enumeration
Session theft
Token theft
Refresh-token theft
Refresh-token replay
XSS
CSRF
CORS misconfiguration
Privilege escalation
JWT tampering
JWT confusion/misconfiguration
Weak password hashing
Secret leakage
Authorization bypass
```

---

# 62. JWT Deep-Dive Curriculum

Learn JWT in this order:

```text
1. JWT structure
2. Header
3. Payload
4. Signature
5. Base64URL
6. Claims
7. Registered claims
8. Issuer
9. Audience
10. Expiration
11. Not-before
12. JWT ID
13. Signing algorithms
14. Signature validation
15. Key management
16. Key rotation
17. Token lifetime
18. Revocation limitations
```

Important mental model:

```text
JWT ≠ authentication system

JWT is one mechanism used inside an authentication system.
```

---

# 63. Refresh Token Deep-Dive

Understand why access and refresh tokens are separated.

```text
Access Token
-------------
Short lifetime
Used frequently
Contains claims
Potentially stateless

Refresh Token
-------------
Longer lifetime
Used less frequently
Should be protected strongly
Can be revoked
Should be rotated
```

Then study:

```text
Refresh token rotation
Token families
Replay detection
Session management
Revocation
Sliding expiration
Absolute expiration
```

---

# 64. Password Security Deep-Dive

Learn:

```text
Plaintext
   ↓
Hashing
   ↓
Salt
   ↓
Work factor
   ↓
Verification
```

Compare:

```text
Hashing vs encryption
```

Understand why this is wrong:

```text
Encrypt(password)
```

for normal password storage.

The system should use a password hashing algorithm designed specifically for passwords.

---

# 65. Authorization Deep-Dive

Study authorization in levels:

```text
Role-based
     ↓
Claim-based
     ↓
Policy-based
     ↓
Permission-based
     ↓
Resource-based
```

Example:

```text
Role:
Admin

Permission:
users.delete

Policy:
Require users.delete

Resource:
Can this admin delete THIS specific user?
```

This gives you a foundation for authorization systems used in larger applications.

---

# 66. Suggested Final Architecture

After completing all phases:

```text
                         ┌─────────────────┐
                         │     Client      │
                         └────────┬────────┘
                                  │
                                  ▼
                         ┌─────────────────┐
                         │   ASP.NET API   │
                         └────────┬────────┘
                                  │
                 ┌────────────────┼────────────────┐
                 │                │                │
                 ▼                ▼                ▼
          Authentication   Authorization       Users
                 │                │
                 ▼                ▼
           JWT / Sessions    Roles / Policies
                 │                │
                 └────────┬───────┘
                          ▼
                    Application
                          │
                          ▼
                     Domain
                          │
                          ▼
                  Infrastructure
                          │
              ┌───────────┼───────────┐
              ▼           ▼           ▼
          Database      Email       Cache
```

---

# 67. Recommended Development Order

Do not try to build everything simultaneously.

Use this order:

```text
Foundation
    ↓
User
    ↓
Password hashing
    ↓
Registration
    ↓
Login
    ↓
JWT
    ↓
Protected endpoints
    ↓
Refresh tokens
    ↓
Token rotation
    ↓
Logout/revocation
    ↓
Email verification
    ↓
Password reset
    ↓
Roles
    ↓
Permissions
    ↓
Policies
    ↓
Cookies
    ↓
CSRF
    ↓
Rate limiting
    ↓
Audit logging
    ↓
Security hardening
    ↓
Integration/security tests
```

This order prevents the project from becoming an unmanageable collection of partially implemented authentication features.

---

# 68. Definition of Done

The project is complete when a user can perform:

```text
Register
   ↓
Verify email
   ↓
Login
   ↓
Receive access + refresh authentication
   ↓
Call protected API
   ↓
Access token expires
   ↓
Refresh authentication
   ↓
Receive rotated refresh token
   ↓
Continue using API
   ↓
Logout
   ↓
Refresh token becomes invalid
```

And:

```text
Forgot password
   ↓
Receive reset email
   ↓
Reset password
   ↓
Existing sessions revoked
   ↓
Login with new password
```

And:

```text
User
 ↓
Role
 ↓
Permissions
 ↓
Policy
 ↓
Authorized endpoint
```

And administrators can:

```text
Lock account
Unlock account
Revoke sessions
Inspect security events
Manage roles
Manage permissions
```

---

# 69. Final Learning Checklist

## Authentication

* [ ] Authentication vs authorization
* [ ] Password hashing
* [ ] Login
* [ ] Logout
* [ ] JWT
* [ ] Claims
* [ ] Access tokens
* [ ] Refresh tokens
* [ ] Token expiration
* [ ] Token rotation
* [ ] Token revocation
* [ ] Session management

## Browser Security

* [ ] Cookies
* [ ] HttpOnly
* [ ] Secure
* [ ] SameSite
* [ ] CSRF
* [ ] CORS
* [ ] XSS
* [ ] Security headers

## Authorization

* [ ] Roles
* [ ] Claims
* [ ] Permissions
* [ ] Policies
* [ ] Authorization handlers
* [ ] Resource authorization
* [ ] Privilege escalation prevention

## Account Security

* [ ] Email verification
* [ ] Password reset
* [ ] Account locking
* [ ] Brute-force protection
* [ ] Rate limiting
* [ ] Account enumeration protection
* [ ] Security events
* [ ] Session revocation

## Production Thinking

* [ ] Secret management
* [ ] Signing-key management
* [ ] Key rotation
* [ ] Secure configuration
* [ ] Logging
* [ ] Monitoring
* [ ] Database indexes
* [ ] Integration tests
* [ ] Security tests
* [ ] Docker deployment

---

# 70. The Main Principle

The most important thing to learn from this project is that authentication is **not just a login endpoint**.

A real system looks more like:

```text
                 ┌──────────────┐
                 │     User     │
                 └──────┬───────┘
                        │
                        ▼
                ┌───────────────┐
                │ Authentication│
                └───────┬───────┘
                        │
              ┌─────────┴─────────┐
              ▼                   ▼
          Access Token       Refresh Token
              │                   │
              ▼                   ▼
        API Authorization     Session State
              │                   │
              └─────────┬─────────┘
                        ▼
                 ┌─────────────┐
                 │Authorization│
                 └──────┬──────┘
                        │
              ┌─────────┼─────────┐
              ▼         ▼         ▼
            Roles   Permissions  Policies
                        │
                        ▼
                 Protected Resource
```

The goal is to understand **every box in this diagram and why it exists**.

Once this project is finished, you should be able to take the same concepts and apply them to almost any ASP.NET Core backend: e-commerce, SaaS, dashboards, mobile APIs, admin systems, multi-tenant applications, and microservices.
