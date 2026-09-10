# Architecture

Handily Commerce Backend follows a **hexagonal (ports & adapters)** layout so the domain stays free of frameworks and I/O details.

## Layers

| Project | Role |
|---------|------|
| **Domain** | Core models and ports (e.g. `IHealthPort`, `ServiceHealth`). No ASP.NET or infrastructure types. |
| **Application** | Use-case implementations of domain ports (e.g. `HealthService`). Exposes `AddApplication()`. |
| **Infrastructure** | Outbound adapters (health-check bridge today; DB/messaging later). Exposes `AddInfrastructure()`. |
| **Api** | Inbound HTTP adapter and composition root: wires DI and maps endpoints. |

Dependency direction: **Api → Application + Infrastructure → Domain** (Infrastructure also refs Application). Domain never depends on outer layers.

## Health check

- Domain port: `IHealthPort` → `ServiceHealth`
- Application: `HealthService` returns Healthy + service name `handily-commerce-backend`
- Infrastructure: `ApplicationHealthCheck` adapts the port to `IHealthCheck`
- Api maps health with a JSON `ResponseWriter` at a path built from `ApiOptions` (e.g. `/api/v1/health` today)

## Ping / version

- Domain port: `IPingPort` → `ServicePing` (`service`, `apiVersion`, `status`)
- Application: `PingService` builds an `ok` probe; `apiVersion` is supplied by the Api adapter from `Api:Version`
- Api: thin `MapGet` at `apiOptions.Path("ping")` (e.g. `/api/v1/ping` today)

## API version (FE footer)

- Domain port: `IApiVersionPort` → `ServiceApiVersion` (`version`, `service`, `apiRouteVersion`)
- **Product Versioning** (`version`): from assembly InformationalVersion / `Directory.Build.props` `<Version>` (e.g. `0.1.0`) — what the FE footer shows as API
- **Route version** (`apiRouteVersion`): from `Api:Version` (e.g. `v1`) — URL segment only
- Application: `ApiVersionService` builds the payload; Api adapter supplies both values
- Api: thin `MapGet` at `apiOptions.Path("apiVersion")` (e.g. `/api/v1/apiVersion` today)
- Example JSON: `{ "version": "0.1.0", "service": "handily-commerce-backend", "apiRouteVersion": "v1" }`


## Changelog (What's new)

- Domain: `ChangelogEntry` + driven port `IChangelogPort` + outbound port `IChangelogStore`
- Application: `ChangelogService` loads via the store and returns entries **newest first**
- Infrastructure: `JsonFileChangelogStore` reads embedded `Changelog/changelog.json` (committed; not a DB yet)
- Api: thin `MapGet` at `apiOptions.Path("changelog")` (e.g. `/api/v1/changelog` today)
- CI: on merged PRs labeled `Release Major` or `Release Mirror`, workflow `changelog-on-merge.yml` runs `scripts/append-changelog` and commits the JSON update (`contents: write`)

## CORS

- Default policy allows origin `https://junioeusebio.github.io` (GitHub Pages) and localhost:4200 for Angular dev
- Methods: `GET`, `OPTIONS`
- `UseCors()` is registered in `Program.cs` **before** `Map*` endpoints

## API versioning (configuration-driven)

HTTP routes are built from the `Api` section in `appsettings.json`:

```json
"Api": {
  "RoutePrefix": "api",
  "Version": "v1",
  "Title": "Handily Commerce API"
}
```

- Path pattern: `/{RoutePrefix}/{Version}/...` (e.g. `/api/v1/health`).
- To introduce a breaking change, set `Api:Version` to `v2` (or keep parallel documents later) — **no hardcoded `/api/v1/` in `Program.cs`**.
- OpenAPI document `Info.Version` / `Info.Title` use the same keys via `AddOpenApi` + document transformer, so Swagger UI (when added) stays aligned.
