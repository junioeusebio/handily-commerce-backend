# generate-endpoint

Gere um endpoint HTTP na Api usando o prefixo versionado de `ApiOptions`.

## Entrada esperada

- Rota relativa (ex.: `orders`, `orders/{id}`)
- Metodo HTTP e contrato de request/response
- Use case / port de Application a chamar

## Regras

1. Path via `apiOptions.Path("...")` — **nunca** hardcodar `/api/v1/`
2. Composition root fino; logica em Application
3. Atualizar `.http` com `@ApiVersion`
4. OpenAPI ja herda `Info.Version` da config
5. Diff revisavel (parte/patch)

## Saida

- Trecho em `Program.cs` (ou arquivo de endpoints se existir)
- Exemplo `.http`
