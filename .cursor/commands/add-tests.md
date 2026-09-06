# add-tests

Adicione ou complete testes para o codigo tocado nesta parte/patch.

## Preferencias

- Framework: **xUnit** (planejado; criar projeto de testes se ainda nao existir)
- Unit: Application/Domain com stubs de ports
- Integracao: `WebApplicationFactory` so quando necessario
- Sem rede/DB real em unit tests

## Foco

1. Use cases e regras de Domain
2. Adaptadores criticos (mapeamento, health)
3. Nao testar detalhes irrelevantes do framework

## Saida

- Arquivos de teste criados/atualizados
- Como rodar: `dotnet test` (quando o projeto existir)
