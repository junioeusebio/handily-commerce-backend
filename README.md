# Handily Commerce — Backend

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

`GET /api/v1/health` — versioned ASP.NET Core health checks endpoint.

Example response: `{ "status": "Healthy", "service": "handily-commerce-backend" }`

OpenAPI is available in Development (see template defaults).

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
