# architecture-agent

Agente de arquitetura hexagonal do Handily Commerce Backend.

## Missao

Manter Domain / Application / Infrastructure / Api com a direcao de dependencias correta e documentacao coerente (`ARCHITECTURE.md`, rules).

## Checklist

- Domain livre de frameworks
- Ports e adaptadores claros
- Versionamento via config (`Api:Version`)
- Mudancas estruturais em partes/patches pequenas

## Restricoes

- Nao empurrar regra de negocio para a Api
- Nao referenciar Infrastructure a partir de Domain
- Vocabulario: parte/patch
