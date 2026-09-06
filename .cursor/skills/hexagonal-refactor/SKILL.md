---
name: hexagonal-refactor
description: Refatora codigo .NET para respeitar ports/adapters e a direcao Domain ← Application ← Infrastructure/Api.
---

# Skill: hexagonal-refactor

## Quando usar

Extrair ports, mover tipos entre camadas, remover acoplamento de framework do Domain.

## Passos

1. Mapear dependencias atuais vs rule `hexagonal`.
2. Empurrar modelos/contratos reutilizaveis para Domain.
3. Manter orquestracao em Application; I/O em Infrastructure; HTTP em Api.
4. Introduzir `IOptions`/ports em vez de ler config ad hoc nas camadas internas.
5. Manter a parte/patch pequena; nao misturar feature nova com refactor cosmetico.
6. Garantir `dotnet build` (e testes quando existirem).

## Criterio de pronto

- Domain sem referencias a Api/Infrastructure
- Rotas publicas ainda derivadas de `ApiOptions`
- Diff revisavel
