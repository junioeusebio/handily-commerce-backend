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

OpenAPI (`MapOpenApi`) is available in Development. Document **Info.Title** / **Info.Version** come from the same `Api` section, so future Swagger UI stays in sync.

See `HandilyCommerce.Api.http` — use `@ApiVersion` (must match `Api:Version`).

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
