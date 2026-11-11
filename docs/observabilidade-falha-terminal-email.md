# Falha terminal do e-mail de lead (log para Loki)

Evento estável emitido quando uma linha de `EmailSendLogs` **passa** de `Pending` ou `Retrying` para `Failed` ou `GivenUp` e o `Update` no banco **termina com sucesso**.

Não há migration. O reenvio, o backoff e o que é gravado em `EmailSendLogs` (incluindo `ErrorMessage`, destinatário e assunto) não mudam. Este documento descreve só o log.

## Quando emite

| Situação | Evento |
| --- | --- |
| Save durável em `Failed` (4xx não retryable, lead ausente, etc.) | um evento |
| Save durável em `GivenUp` (tentativas transitórias esgotadas) | um evento, só na transição final |
| `Retrying` | não |
| `Sent` | não |
| `Skipped` | não |
| `Update` lança exceção | não (a linha segue aberta; o worker continua registrando a falha de lote já existente) |
| Poll seguinte, ou uma linha já final devolvida pelo claim | não |

O claim só pega linhas abertas. O código ainda exige que o status **anterior** ao save seja aberto, para não repetir o evento se uma linha final for processada de novo.

## Severidade

| Camada | Valor |
| --- | --- |
| `Microsoft.Extensions.Logging` | `Warning` |
| Label Loki `level` | `warning` |

O sink `Serilog.Sinks.Grafana.Loki` 9 (o mesmo de `LoggingSetup`, com `handleLogLevelAsLabel` no padrão `true`) grava o nível no label `level` com o vocabulário `trace`, `debug`, `info`, `warning`, `error`, `fatal`. `LogLevel.Warning` vira `warning`. Isso foi lido no POST capturado pelo teste `LeadNotificationTerminalFailureLogTests`, que usa `LoggingSetup.Configure` e o handler HTTP do sink — sem chamar o Grafana.

`LoggingSetup` deixa o nível mínimo em `Information`, então `Warning` segue no pipeline. Se alguém subir esse mínimo acima de `Warning`, a linha continua sendo salva e o evento deixa de sair.

Alertas já existentes (somente leitura; este PR não altera regras):

- **Handily API errors** — `{app="handily-commerce-backend",env="production",level=~"error|fatal"}`. Um `Error`/`Fatal` deste desfecho também incrementaria esse alerta de API. Por isso o evento fica em `warning`.
- **Handily lead notification batch failed** — `{app="handily-commerce-backend",env="production"} |= "Lead notification batch could not be processed"`. Isso é exceção do lote no `LeadNotificationWorker`, não uma linha `Failed`/`GivenUp`. A frase não entra neste evento.

Nada aqui cria ou edita alerta no Grafana.

## Contrato do evento

| Campo | Valor estável |
| --- | --- |
| Nome | `LeadNotificationTerminalFailure` |
| `EventId` | `6102` (`EventId.Name` = o mesmo nome) |
| `MessageTemplate` | `LeadNotificationTerminalFailure Status={Status} AttemptCount={AttemptCount} EmailSendLogId={EmailSendLogId} LeadId={LeadId}` |
| `Status` | texto `Failed` ou `GivenUp` |
| `AttemptCount` | número já persistido na linha. Pode ser `0` quando o lead não existe: a tentativa não incrementa o contador (comportamento anterior, só reportado). |
| `EmailSendLogId` | `Guid` da linha `EmailSendLogs` |
| `LeadId` | `Guid` do lead, ou JSON `null` se a linha não tem lead |

Não entram no log: nome, e-mail, telefone, município, assunto, destinatário, corpo, `ErrorMessage`, id/resposta crua do provedor, texto de exceção, token ou connection string.

`SourceContext`, `Application`, `Environment` e `Version` continuam nos enrichers já usados pelo host. Não fazem parte do filtro sugerido.

## Serialização real do sink

O teste grava o body do `POST /loki/api/v1/push` produzido por `LoggingSetup` (labels `app` e `env`, `traceIdMode` no body, formatter padrão `LokiJsonTextFormatter`). Labels do stream neste POST:

```json
{"app":"handily-commerce-backend","env":"production","level":"warning"}
```

O Grafana guarda como **linha** só o JSON do evento (o segundo item de `values`), não o envelope `streams`. Forma observada para `Failed` com lead (guids ilustrativos; o teste confere os campos, não um guid fixo):

```json
{
  "Message": "LeadNotificationTerminalFailure Status=\"Failed\" AttemptCount=1 EmailSendLogId=<guid> LeadId=<guid>",
  "MessageTemplate": "LeadNotificationTerminalFailure Status={Status} AttemptCount={AttemptCount} EmailSendLogId={EmailSendLogId} LeadId={LeadId}",
  "Status": "Failed",
  "AttemptCount": 1,
  "EmailSendLogId": "<guid>",
  "LeadId": "<guid>",
  "EventId": { "Id": 6102, "Name": "LeadNotificationTerminalFailure" },
  "SourceContext": "HandilyCommerce.Application.Leads.LeadNotificationService",
  "Application": "handily-commerce-backend",
  "Environment": "Production",
  "Version": "<Version de Directory.Build.props>"
}
```

Detalhes que a consulta precisa respeitar:

- No `Message` renderizado pelo Serilog, strings vão entre aspas (`Status=\"Failed\"` dentro do JSON). O campo estruturado `Status` é a string simples `Failed` ou `GivenUp`. Não filtre a linha com `|= "Status=Failed"`: esse texto não aparece.
- `AttemptCount` é número JSON (`0`, `1`, …). A consulta sugerida não filtra esse campo.
- Sem lead, `LeadId` é JSON `null` (não a string `"null"`). O `Message` traz `LeadId=null`. Não exija `LeadId` preenchido.
- Não há propriedade `Exception`.
- `EventId` é objeto aninhado. A consulta sugerida não depende de achatar `EventId.Name`.

Amostras de **outros** eventos já em produção (produto `0.15.0`, sem este evento) usam o mesmo tipo de linha JSON e os labels `app`, `env` e `level` (`info` para `Information`). Nessas amostras o Grafana Cloud também mostrou `service_name` e o metadado `detected_level`. O POST deste sink **não** envia esses dois; o teste também não os vê. O seletor abaixo usa só `app` e `env`, como os alertas atuais.

## LogQL sugerida

**Não executada no Grafana e não verificada em produção.** O evento ainda não existe lá. A expressão espelha o alerta de lote (`count_over_time` > 0 em 5 minutos) e os nomes de campo do JSON acima.

```logql
sum(count_over_time({app="handily-commerce-backend",env="production"} |= "LeadNotificationTerminalFailure" | json | MessageTemplate="LeadNotificationTerminalFailure Status={Status} AttemptCount={AttemptCount} EmailSendLogId={EmailSendLogId} LeadId={LeadId}" | Status=~"Failed|GivenUp" [5m]))
```

O trecho `|= "LeadNotificationTerminalFailure"` casa o literal do template (e do `Message`). `| json | Status=~"Failed|GivenUp"` usa o campo de topo, não o `Message` com aspas. `level="warning"` não entra no seletor de propósito: o alerta genérico de API continua só em `error|fatal`.

Antes de gravar a regra no Grafana, rode a consulta no Explore e confira uma linha real. Este repositório não fez isso.
