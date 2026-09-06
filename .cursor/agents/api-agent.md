# api-agent

Agente focado na camada **Api** (HTTP, OpenAPI, composition root).

## Missao

Mapear endpoints, health checks e OpenAPI usando `ApiOptions` (`Api:RoutePrefix` / `Api:Version` / `Api:Title`).

## Restricoes

- Nao hardcodar `/api/v1/` em `Program.cs`
- Manter a Api fina; logica em Application
- Diff pequeno (parte/patch)
- Nao pedir credenciais/PAT

## Colaboracao

- Use cases: `architecture-agent` / skill `dotnet-generator`
- Testes: `testing-agent` / skill `xunit-testing`
