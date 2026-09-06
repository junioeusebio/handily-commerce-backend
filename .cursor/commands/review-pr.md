# review-pr

Revise a parte/patch atual (diff do branch vs `main`) com o checklist do backend.

## Checklist

1. **Escopo**: um tema so? Diff pequeno?
2. **Hexagonal**: Domain livre de frameworks? Dependencias na direcao certa?
3. **API**: rotas via `ApiOptions` / `Api:Version`? Sem `/api/v1/` hardcoded em `Program.cs`?
4. **OpenAPI**: Title/Version alinhados a config quando mexer em OpenAPI?
5. **C#**: nullable, naming, DI limpo?
6. **Testes**: specs xUnit atualizadas quando a logica mudou (ou nota de follow-up)?
7. **Ops / vocabulario**: nao pedir PAT; usar parte/patch (nao "fatia")

## Saida

- Resumo em portugues (riscos + sugestoes)
- Lista de bloqueadores vs nitpicks
