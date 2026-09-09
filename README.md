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

See `HandilyCommerce.Api.http` — use `@ApiVersion` (must match `Api:Version`).

### Product version (FE footer)

`GET /{Api:RoutePrefix}/{Api:Version}/apiVersion` — today **`GET /api/v1/apiVersion`**.

- `version` = **product Versioning** from `Directory.Build.props` (`<Version>`, stamped on the assembly)
- `apiRouteVersion` = route segment from `Api:Version` (unchanged URL prefix)

```json
{ "version": "0.2.0", "service": "handily-commerce-backend", "apiRouteVersion": "v1" }
```

Release process: [docs/VERSIONING.md](docs/VERSIONING.md).

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
