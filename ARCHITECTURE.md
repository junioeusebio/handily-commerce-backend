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
- Api maps **`GET /api/v1/health`** with a JSON `ResponseWriter`

## API versioning

HTTP routes are versioned under `/api/v1/...`. Prefer adding `/api/v2/...` for breaking changes rather than mutating v1 contracts in place.
