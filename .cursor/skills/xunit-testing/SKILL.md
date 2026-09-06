---
name: xunit-testing
description: Adiciona testes xUnit (planejado) para use cases e adaptadores do Handily Commerce Backend.
---

# Skill: xunit-testing

## Quando usar

Cobrir use cases/ports novos ou regressoes em uma parte/patch. Hoje a solucao ainda pode nao ter projeto de teste — criar um se pedido.

## Unit

- Projeto xUnit referenciando Application/Domain
- Stubs/fakes de ports; sem HTTP/DB reais
- Nomes claros de cenario

## Integracao (opcional)

- `WebApplicationFactory` na Api
- Validar health path com `Api:Version` de teste

## Boas praticas

- Testes proximos ao comportamento, nao a detalhes de framework
- Incluir na mesma parte/patch quando a logica nao for trivial
- Vocabulario: parte/patch
