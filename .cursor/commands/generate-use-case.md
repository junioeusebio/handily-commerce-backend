# generate-use-case

Gere um caso de uso alinhado ao hexagonal deste repo.

## Entrada esperada

- Nome do use case / port (ex.: `GetOrder`, `IOrderPort`)
- Se precisa de port novo em Domain ou so Application

## Regras

1. Port (interface) em `HandilyCommerce.Domain` se for contrato de negocio
2. Implementacao em `HandilyCommerce.Application`
3. Registrar em `AddApplication()`
4. Sem ASP.NET / EF na Application
5. Parte/patch pequena; nao misturar endpoint HTTP nesta tarefa salvo pedido explicito

## Saida

- Arquivos criados
- Assinatura do port e exemplo de registro DI
