# Versionamento semântico (BE)

Processo de release por labels em cada Pull Request.

## Labels → bump

| Label | Bump | Significado |
| --- | --- | --- |
| `Release Major` | **X**.0.0 | Breaking change |
| `Release Mirror` | x.**Y**.0 | Feature / minor (**Mirror = minor** neste projeto) |
| `Release Patch` | x.y.**Z** | Correção / patch |

Exatamente **um** desses labels é obrigatório em todo PR.

## Fonte da versão (BE)

Há duas versões distintas:

| O quê | Onde | Uso |
| --- | --- | --- |
| **Produto (Versioning)** | `Directory.Build.props` → `<Version>` | Rodapé FE (**API**), campo `version` em `GET /api/v1/apiVersion` |
| **Rota da API** | `appsettings` → `Api:Version` (ex.: `v1`) | Segmento de URL `/api/v1/...` e campo opcional `apiRouteVersion` |

Quem abre o PR deve:

1. Aplicar o label correto (`Release Major` | `Release Mirror` | `Release Patch`).
2. Atualizar `<Version>` em `Directory.Build.props` com o bump correspondente (não confundir com `Api:Version`).
3. Incluir `Signed-off-by: Júnio Eusébio` e pedir review de `junioeusebio`.

Não há bump automático: o workflow só valida se o label está presente.

## Changelog (Major / Mirror)

Merged PRs with `Release Major` or `Release Mirror` should appear in `GET /api/v1/changelog` (FE “What's new”).

- Seed (temporary JSON): `src/HandilyCommerce.Infrastructure/Changelog/changelog.json` (each entry has a fixed Guid `id`)
- Port: `IChangelogRepository` — JSON adapter today; **next micro-PR (B1 SQL)** swaps to EF Core and migrates seed to `ChangelogEntries`
- Automation: `.github/workflows/changelog-on-merge.yml` + `scripts/append-changelog` (new Guid `id`, title = PR title; summary = first paragraph / Descrição); after SQL this becomes an INSERT
- `Release Patch` does **not** create a changelog entry
