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

Health check: `GET /api/health` → `{ "status": "ok", "service": "handily-commerce-backend" }`

OpenAPI is available in Development (see template defaults).

## Structure

- `HandilyCommerce.slnx` — solution
- `src/HandilyCommerce.Api` — Web API project (`net10.0`)
