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
| **Produto (semver)** | `Directory.Build.props` → `<Version>` | Rodapé FE (**API**), campo `version` em `GET /api/v1/apiVersion` |
| **Rota da API** | `appsettings` → `Api:Version` (ex.: `v1`) | Segmento de URL `/api/v1/...` e campo opcional `apiRouteVersion` |

Quem abre o PR deve:

1. Aplicar o label correto (`Release Major` | `Release Mirror` | `Release Patch`).
2. Atualizar `<Version>` em `Directory.Build.props` com o bump correspondente (não confundir com `Api:Version`).
3. Incluir `Signed-off-by: Júnio Eusébio` e pedir review de `junioeusebio`.

Não há bump automático: o workflow só valida se o label está presente.
