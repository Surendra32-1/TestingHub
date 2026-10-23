# TeamHub authentication

The API registers this module with `AddTeamHubAuth(builder.Configuration)`.
All authentication endpoints, Dapper persistence, JWT services, and their
contracts live in this project. Authentication schema setup is in
`../TeamHub.Infrastructure/authentication-schema.sql`; apply it to the existing
TeamHub database after the employee and role tables have been created.

Configure `Authentication:Jwt:SecretKey` through a secret provider or the
`Authentication__Jwt__SecretKey` environment variable. Use a randomly generated
secret of at least 32 bytes and keep it consistent across API instances. Do not
put the key in `appsettings.json` or source control. Issuer and audience are
configured in the API's `appsettings.json`. Configure `Frontend:AllowedOrigins`
for the deployed Angular origin; refresh cookies require HTTPS and trusted
origins.

The HR account-creation endpoint requires an active `HR` role. Provision the
first HR account through a controlled deployment/database initialization
process; there is intentionally no unauthenticated account-bootstrap endpoint.
Assign only active IDs from the existing `Roles` table when creating an
employee account.

`../TeamHub.API/auth.http` contains example calls to all authentication
endpoints. The refresh token is kept in an HttpOnly, Secure cookie and only its
SHA-256 hash is stored in the database.
