---
name: dotnet-generator
description: Gera ports, use cases e endpoints .NET 10 no layout hexagonal HandilyCommerce com ApiOptions.
---

# Skill: dotnet-generator

## Quando usar

Criar artefatos novos (port, application service, endpoint) sem quebrar camadas.

## Passos

1. Confirmar a camada (Domain vs Application vs Infrastructure vs Api).
2. Port em Domain; implementacao em Application; registro em `AddApplication()`.
3. Adaptador de saida em Infrastructure + `AddInfrastructure()` se precisar de I/O.
4. Endpoint na Api com `apiOptions.Path(...)`.
5. Atualizar `.http` (`@ApiVersion`) e docs se a superficie publica mudar.
6. Opcional: esboco de teste xUnit (skill `xunit-testing`).

## Anti-padroes

- Hardcodar `/api/v1/` em `Program.cs`
- Referenciar ASP.NET dentro de Domain/Application
- Misturar varias features nao relacionadas na mesma parte/patch
