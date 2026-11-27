# Option 3 setup

The application applies EF Core migrations and installs the SQL Server audit
triggers when it starts. Set `ConnectionStrings__AutoTrack` to a SQL Server
connection string before running outside local development. The default
connection string targets `(localdb)\MSSQLLocalDB`. The trigger script uses
`SESSION_CONTEXT` and `FOR JSON`, so use SQL Server 2016 or later.

For the first Owner account, set `BootstrapOwner__Email` and
`BootstrapOwner__Password` in the environment. `BootstrapOwner__FullName` is
optional. The password must be at least 12 characters and include uppercase,
lowercase, numeric, and non-alphanumeric characters. These settings are only
used to create or promote that account; do not put production credentials in
`appsettings.json`.

## API routes

- `GET /api/service-history/{vehicleNo}` — completed work, parts, date, and
  total for a vehicle. Customers can retrieve only vehicles linked to their
  own account.
- `GET /api/customer-portal/profile`, `/vehicles`, and `/appointments` —
  Customer-only routes filtered by the authenticated Identity user ID.
- `PUT /api/customer-portal/profile` — updates the caller's phone number.
- `GET /api/users`, `POST /api/users`, and
  `PUT /api/users/{id}/role|active` — Owner-only user administration.
- `GET /api/audit-logs?user=&date=YYYY-MM-DD&action=` — Owner-only audit
  search; returns at most 500 newest entries.

The APIs use the Identity application cookie. For unsafe API requests, first
call `GET /api/antiforgery-token`, retain its antiforgery cookie, and send the
returned token in the `X-CSRF-TOKEN` header.

The triggers write row snapshots to `AUDIT_LOG`; the `USER_ACCOUNT` trigger
omits password hashes and authentication/security stamps. Actor names are
obtained from SQL session context set by the application for each request.
