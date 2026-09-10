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

OpenAPI `servers` use HTTPS for Render (`Api:PublicBaseUrl` or request host + forwarded proto; never `http://…onrender.com`), so Scalar **Try request** works without mixed content.

See `HandilyCommerce.Api.http` — use `@ApiVersion` (must match `Api:Version`).

### Product version (FE footer)

`GET /{Api:RoutePrefix}/{Api:Version}/apiVersion` — today **`GET /api/v1/apiVersion`**.

- `version` = **product Versioning** from `Directory.Build.props` (`<Version>`, stamped on the assembly)
- `apiRouteVersion` = route segment from `Api:Version` (unchanged URL prefix)

```json
{ "version": "0.3.1", "service": "handily-commerce-backend", "apiRouteVersion": "v1" }
```

Release process: [docs/VERSIONING.md](docs/VERSIONING.md).

### Changelog (What's new)

`GET /{Api:RoutePrefix}/{Api:Version}/changelog` — today **`GET /api/v1/changelog`**.

Source for the FE “What's new” modal. Entries are created after merge of PRs labeled **Release Major** or **Release Mirror** (not Patch). Persistence port is `IChangelogRepository`; the current adapter is temporary JSON (`changelog.json` with stable Guid `id`s). **Next micro-PR (B1 SQL)** adds DB + EF repository and migrates the seed — API/Application contracts unchanged.

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

Newest first. CORS already allows `GET` / `OPTIONS` for Pages and local Angular. After SQL, append workflow becomes an INSERT into `ChangelogEntries`.


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


## Deploy on Render (free, no database)

This repo includes a Docker image (`Dockerfile`) and a Render Blueprint (`render.yaml`) for a **free** web service — **no Postgres/database** yet.

1. Create a [Render](https://render.com) account and connect the GitHub repo `junioeusebio/handily-commerce-backend`.
2. Use **New → Blueprint** and select this repository (Render reads `render.yaml`), or create a **Web Service** with Docker runtime pointing at `./Dockerfile`.
3. Choose the **free** plan. Expect **cold starts** after idle (first request can take several seconds).
4. Health check: `GET /api/v1/health` (also configured as `healthCheckPath` in the Blueprint).
5. No database resources are provisioned in `render.yaml` for now — add Postgres later when needed.

Local Docker (optional):

```bash
docker build -t handily-commerce-backend .
docker run --rm -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Production handily-commerce-backend
```
