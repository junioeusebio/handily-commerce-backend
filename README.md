# Handily Commerce — Backend

[![CI](https://github.com/junioeusebio/handily-commerce-backend/actions/workflows/ci.yml/badge.svg)](https://github.com/junioeusebio/handily-commerce-backend/actions/workflows/ci.yml)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=junioeusebio_handily-commerce-backend&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=junioeusebio_handily-commerce-backend)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=junioeusebio_handily-commerce-backend&metric=coverage)](https://sonarcloud.io/summary/new_code?id=junioeusebio_handily-commerce-backend)

ASP.NET Core Web API (.NET 10) for **Handily Commerce**.

Backend pair of the frontend: https://github.com/junioeusebio/handily-commerce-frontend  
(Note: the frontend may still be renaming from `handily-angular`.)

## Requirements

- .NET 10 SDK (`net10.0`)

## Run

```bash
dotnet run --project src/HandilyCommerce.Api
```

### Health check

`GET /{Api:RoutePrefix}/{Api:Version}/health` — today that resolves to **`GET /api/v1/health`**.

The version is **configuration-driven** (`Api:Version` in `appsettings.json`). Changing it to `v2` updates the HTTP route **without** editing `Program.cs`.

Example response: `{ "status": "Healthy", "service": "handily-commerce-backend" }`

### Ping / version

`GET /{Api:RoutePrefix}/{Api:Version}/ping` — today **`GET /api/v1/ping`**.

Stable JSON contract; `apiVersion` comes from the same `Api:Version` config (not hardcoded):

```json
{ "service": "handily-commerce-backend", "apiVersion": "v1", "status": "ok" }
```

### OpenAPI / Scalar UI

Interactive docs (Scalar) and the raw OpenAPI document are available in **all environments**, including Production on Render (demo):

| Resource | Path |
| --- | --- |
| Scalar UI | `/scalar` (or `/scalar/v1`) |
| OpenAPI JSON | `/openapi/v1.json` |

Document **Info.Title** / **Info.Version** come from the same `Api` section (`AddOpenApi` transformer), so the UI stays in sync with config.

OpenAPI `servers` use the public HTTPS base URL, so Scalar **Try request** works without mixed content.

See `HandilyCommerce.Api.http` — use `@ApiVersion` (must match `Api:Version`).

### Product version (FE footer)

`GET /{Api:RoutePrefix}/{Api:Version}/apiVersion` — today **`GET /api/v1/apiVersion`**.

- `version` = **product Versioning** from `Directory.Build.props` (`<Version>`, stamped on the assembly)
- Docker image builds must `COPY Directory.Build.props` into the build context (see `Dockerfile`); omitting it falls back to MSBuild default `1.0.0`.
- `apiRouteVersion` = route segment from `Api:Version` (unchanged URL prefix)

```json
{ "version": "0.5.0", "service": "handily-commerce-backend", "apiRouteVersion": "v1" }
```

Release process: [docs/VERSIONING.md](docs/VERSIONING.md).

### Changelog (What's new)

`GET /{Api:RoutePrefix}/{Api:Version}/changelog` — today **`GET /api/v1/changelog`**.

Source for the FE “What's new” modal. Entries are created after merge of PRs labeled **Release Major** or **Release Mirror** (not Patch) via a bot PR (`chore/changelog-pr-<n>`, never a direct push to `main`). Persistence port is `IChangelogRepository`; runtime adapter is **EF Core** (`EfChangelogRepository`) against **Supabase Postgres** (`ChangelogEntries`, seed via migration `HasData`). The committed `changelog.json` remains for the merge workflow append until a later PR writes new entries to the DB.

```json
[
  {
    "id": "a10c0000-0010-4000-8000-00000000000a",
    "title": "feat(A2): Scalar OpenAPI UI",
    "summary": "Adds Scalar UI over the existing OpenAPI document in all environments, including Production on Render.",
    "productVersion": "0.2.0",
    "mergedAt": "2026-09-09T14:39:41Z",
    "prNumber": 10,
    "label": "Release Mirror"
  }
]
```

Newest first. CORS already allows `GET` / `OPTIONS` for Pages and local Angular.

### Products (catalog)

`GET /{Api:RoutePrefix}/{Api:Version}/products` — today **`GET /api/v1/products`**.

Foundation for FE catalog (A4/later). Returns products with nested items (empty array OK until data is seeded). Model: `Product` + child `Item` (FK), EF migration `AddProductsAndItems`. No fake seed required.

```json
[]
```

### Leads

`POST /api/v1/leads` with `{ name, email, municipality, message? }` → `201 { id }`; invalid fields → `400`.

### Database (Postgres)

EF Core + Npgsql. **Never commit credentials.** Locally, use user-secrets with a placeholder connection string:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=<host>;Port=<port>;Database=<db>;Username=<user>;Password=<password>;SSL Mode=Require" --project src/HandilyCommerce.Api
dotnet ef database update --project src/HandilyCommerce.Infrastructure --startup-project src/HandilyCommerce.Api
```

Tests run without a live DB (EF InMemory). In Development, the API applies migrations on startup when a connection string is set. In hosted environments, configure via the service's environment variables (see your private notes).


## Structure (hexagonal)

```
src/
  HandilyCommerce.Domain/           # core domain (no layer deps)
  HandilyCommerce.Application/      # use cases; refs Domain
  HandilyCommerce.Infrastructure/   # outbound adapters; refs Application + Domain
  HandilyCommerce.Api/              # inbound HTTP; refs Application + Infrastructure
```

- `HandilyCommerce.slnx` — solution
- See [ARCHITECTURE.md](ARCHITECTURE.md) for ports/adapters and API versioning notes.
- Cursor agents/rules: [AGENTS.md](AGENTS.md) and `.cursor/`


## Logs

Logs always go to the console and, when enabled, to a hosted log backend. Unhandled errors return `application/problem+json` with a `traceId` (no exception details); search that `traceId` in the log UI to find the error. Configure via the service's environment variables (see your private notes).

## Auth

A login endpoint issues a short-lived JWT used by protected routes; catalog, courses, leads and ping stay public. Credentials and signing keys are configured via the service's environment variables (see your private notes) — never in the repo.

## Deploy

The repo ships a `Dockerfile` and a `render.yaml` Blueprint. Configure secrets via the service's environment variables (see your private notes).

Local Docker (optional):

```bash
docker build -t handily-commerce-backend .
docker run --rm -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Production handily-commerce-backend
```
