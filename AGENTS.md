# AGENTS.md — Handily Commerce Backend

Guia rapido para agentes (Cursor) neste repositorio.

## Projeto

- ASP.NET Core **.NET 10**, layout **hexagonal** (Domain / Application / Infrastructure / Api)
- Versao publica da API: config **`Api:Version`** (hoje `v1`) + `Api:RoutePrefix`
- Detalhes: `ARCHITECTURE.md` e `.cursor/rules/`

## Stack planejada

- Testes unitarios: **xUnit** (ainda a configurar)
- OpenAPI nativo; Swagger UI depois, lendo o mesmo `Api:Version` / Title

## Agentes especializados

| Agente | Arquivo | Foco |
| --- | --- | --- |
| Api | `.cursor/agents/api-agent.md` | Endpoints, OpenAPI, `ApiOptions` |
| Arquitetura | `.cursor/agents/architecture-agent.md` | Ports/adapters e fronteiras |
| Testing | `.cursor/agents/testing-agent.md` | xUnit (futuro) |

## Skills

- `.cursor/skills/dotnet-generator` — gerar port/use case/endpoint
- `.cursor/skills/hexagonal-refactor` — mover/extrair respeitando camadas
- `.cursor/skills/xunit-testing` — adicionar testes xUnit

## Comandos Cursor

- `generate-use-case`, `generate-endpoint`, `add-tests`, `review-pr` em `.cursor/commands/`

## Regras de ouro

1. Um PR = uma parte/patch focada (nao use a palavra "fatia").
2. Domain nao depende de Api/Infrastructure.
3. Rotas versionadas via `ApiOptions` — sem hardcode `/api/v1/` em `Program.cs`.
4. Nao alterar codigo da API em PRs so de config Cursor, salvo necessidade explicita.
5. Nunca solicitar PAT; usar `gh` autenticado no ambiente.
