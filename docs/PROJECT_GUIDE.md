# Guia do projeto FIAP X API

Este documento explica a modelagem e as decisões principais da `fiapx-api`.

## Objetivo da API

A API é responsável por coordenar o início do processamento de videos. Ela não extrai frames. Quem faz isso é o serviço `fiapx-processor`, executado operacionalmente por workers.

Responsabilidades da API:

- Criar o processing job.
- Gerar URL assinada para upload direto no S3.
- Confirmar que o upload foi concluído.
- Publicar a solicitação de processamento na fila SQS.
- Expor consulta de status para polling.
- Expor metadados e redirecionamento de download do ZIP final.

## Estrutura em camadas

`FiapX.Api`:

- Controllers REST.
- Middlewares HTTP.
- Extensions de configuração do módulo HTTP.
- Swagger.
- Current user baseado em JWT ou headers locais.

`FiapX.Application`:

- Casos de uso.
- Requests e resultados dos casos de uso.
- Validators de entrada dos casos de uso.
- Abstrações para storage, mensageria, usuário atual e repositório.
- Orquestração do fluxo.

`FiapX.Domain`:

- Entidades e regras de negócio.
- Status do processamento.
- Mensagens padronizadas.
- Exceptions de negócio.

`FiapX.Infra.Data`:

- Modelo DynamoDB.
- Mapeamento entre DynamoDB e domínio.
- Repositório de processing jobs.

`FiapX.Infra.Storage`:

- Adapter S3.
- URL assinada de upload.
- URL assinada de download.
- Consulta de metadados do objeto.

`FiapX.Infra.Messaging`:

- Adapter SQS.
- Envelope de evento com `headers` e `payload`.
- Propagação de `traceparent`.

`FiapX.Infra.CrossCutting.IoC`:

- Registro de dependências.
- Configuração dos clients AWS.
- Configuração de options tipadas.

## Entidades de domínio

`ProcessingJob`:

Representa o ciclo de vida do processamento. Veio diretamente do contrato REST e do fluxo arquitetural.

Campos principais:

- `Id`: identificador do processamento.
- `UserId`, `UserName`, `UserEmail`: dados do usuário autenticado, usados para ownership e notificação futura.
- `Description`, `Author`, `ClientReference`: metadados opcionais enviados pelo cliente no cadastro.
- `Status`: `upload_pending`, `queued`, `processing`, `succeeded` ou `failed`.
- `InputFile`: referência ao video original no S3.
- `OutputPrefix`: prefixo S3 onde o processor grava frames e ZIP.
- `ProgressPercentage`: progresso atualizado pelo processor no DynamoDB.
- `EstimatedCompletionTime`: TTC do contrato.
- `Messages`: mensagens padronizadas do processamento.
- `ResultFile`: ZIP final, quando houver sucesso.

`ProcessingInputFile`:

Representa o arquivo original enviado pelo usuário. Guarda nome original, MIME type, tamanho e referência S3.

`S3ObjectReference`:

Value Object para `bucket`, `key` e `region`. Evita passar strings soltas de S3 pelo código.

`ProcessingMessage`:

Mensagem de status padronizada pelo contrato. Contém `code`, `message` e `severity`.

`FileResult`:

Representa o ZIP final com os frames extraídos.

## Estados do processamento

`upload_pending`:

A API criou o job e devolveu a URL assinada, mas o upload ainda não foi confirmado.

`queued`:

O upload foi confirmado e a mensagem foi publicada na fila `video-processing-requested`.

`processing`:

O worker do processor começou a extrair frames.

`succeeded`:

O ZIP foi criado no S3 e o arquivo final está disponível.

`failed`:

O processamento terminou com erro.

## Contrato REST implementado

Endpoints principais:

- `GET /v1/processing-jobs` lista jobs do usuário.
- `POST /v1/processing-jobs` cria job e devolve URL assinada de upload.
- `POST /v1/processing-jobs/{processingJobId}/upload-completion` confirma upload e publica evento.
- `GET /v1/processing-jobs/{processingJobId}` consulta status ou devolve `303 See Other` quando concluído com sucesso.
- `GET /v1/files/{fileId}` devolve metadados do ZIP final.
- `GET /v1/files/{fileId}/content` devolve `303 See Other` para a URL assinada de download.

## Contrato de mensageria implementado

A API publica na fila lógica `VideoProcessingRequested`, configurada por padrão como `video-processing-requested`.

Envelope publicado:

```json
{
  "headers": {
    "eventId": "uuid",
    "eventType": "video.processing.requested",
    "eventVersion": "1.0",
    "traceparent": "00-...",
    "occurredAt": "2026-06-10T00:00:00Z",
    "source": "fiapx-api"
  },
  "payload": {
    "processingJobId": "uuid",
    "userId": "auth0|abc123",
    "description": "Demo video",
    "author": "Fulano de Tal",
    "clientReference": "ticket-123",
    "inputFile": {
      "bucket": "fiapx-media",
      "key": "videos/<id>/original.mp4",
      "region": "us-east-1",
      "originalFileName": "video.mp4",
      "contentType": "video/mp4",
      "sizeBytes": 12345
    },
    "outputPrefix": "frames/<id>/",
    "requestedAt": "2026-06-10T00:00:00Z"
  }
}
```

## DynamoDB

Tabela: `fiapx-processing-jobs`.

Chave primária:

- `id`, string.

Índices globais:

- `userId-index`, para listar jobs do usuário autenticado.
- `resultFileId-index`, para localizar o job a partir do arquivo final.

A API e o processor compartilham essa tabela por trade-off do hackathon. Em uma evolução v2, o ideal seria cada serviço ter seu próprio banco e propagar progresso por evento.

## S3

Bucket padrão local: `fiapx-media`.

Prefixos:

- `videos/{processingJobId}/original.ext` para o video original.
- `frames/{processingJobId}/` para frames e ZIP final.

A API não recebe o binário do video. Ela apenas gera uma URL assinada para upload direto no S3.

## Segurança

Localmente, `Authentication:Enabled=false` permite simular o usuário por headers:

- `X-User-Id`.
- `X-User-Name`.
- `X-User-Email`.

Em produção ou AWS Student, é possível ativar JWT:

```json
{
  "Authentication": {
    "Enabled": true,
    "Authority": "https://seu-tenant.auth0.com/",
    "Audience": "fiapx-api"
  }
}
```

Com autenticação ativa, a API exige token nos controllers. Em `DEBUG`, seguindo o padrão do Mechanics, a API aceita Bearer token sem validar assinatura/expiração para facilitar testes locais. Fora de `DEBUG`, a validação usa `Authority` e `Audience`, normalmente apontando para o Auth0.

## Validação

A validação foi separada em camadas:

- Validators de requests de caso de uso ficam na Application e usam FluentValidation.
- Queries HTTP simples ficam na API e usam validação de modelo do ASP.NET Core.
- O `RequestValidationFilter` executa os validators antes do controller chamar o caso de uso.
- A Application mantém regras que dependem de estado externo ou orquestração, como objeto ausente no S3, divergência de tamanho e checksum.
- O domínio mantém invariantes para não permitir objetos em estado inválido, mesmo se o caso de uso for chamado fora da API.

Exemplos de validação de request:

- `CreateProcessingJobRequestValidator` valida `inputFile`, metadados e tipo MIME de video.
- `CompleteProcessingJobUploadRequestValidator` valida campos opcionais de confirmação de upload.
- `ListProcessingJobsQuery` valida `page` e `size` na API; `status` é convertido do contrato HTTP para o enum de domínio antes de chamar a Application.

## Trace Context

A API usa `System.Diagnostics.Activity` em formato W3C. Quando existe uma requisição com `traceparent`, o ASP.NET Core propaga o contexto. Quando a API publica no SQS, ela inclui o `traceparent` no envelope do evento.

O header `traceparent` também é devolvido nas respostas HTTP.

## Cache e compressão

A API configura compressão HTTP com Brotli e Gzip para:

- `application/json`.
- `application/problem+json`.

Status de processamento em andamento usa `Cache-Control: no-store`.

Metadados de arquivo final usam cache privado curto.

## Padrões de projeto usados

Clean Architecture:

Separação entre API, Application, Domain e Infrastructure.

Repository:

`IProcessingJobRepository` abstrai DynamoDB da Application.

Adapter:

`S3StorageService` adapta S3 para `IStorageService`; `MessagePublisher` adapta SQS para `IMessagePublisher`.

Factory Method:

`ProcessingJob.Create` cria um novo agregado válido. `ProcessingJob.Restore` recria estado vindo do banco sem disparar regras de criação.

Value Object:

`S3ObjectReference` encapsula a referência S3.

Options Pattern:

`AwsCredentialsOptions`, `StorageOptions`, `MessagingOptions` e `TableNames` carregam configuração tipada.

Error Handling:

`GlobalExceptionHandler` usa `IExceptionHandler` e `IProblemDetailsService` do ASP.NET Core para centralizar erros em `application/problem+json`.

Middleware:

`TraceContextResponseMiddleware` centraliza o header `traceparent`.

Request Validation Filter:

`RequestValidationFilter` aplica FluentValidation nos argumentos das actions, seguindo o padrão usado nos projetos Mechanics.

Dependency Injection:

O `Program.cs` registra explicitamente a parte HTTP da API, como controllers, Swagger, compressão, autenticação e middlewares. As extensões `AddDataRepositories`, `AddStorage`, `AddMessaging`, `AddAppServices` e `AddRequestValidators` continuam separando os módulos de infraestrutura e aplicação, seguindo o estilo dos projetos Mechanics e FIAP X Notifier.

Registros explícitos:

Os app services e repositórios são registrados explicitamente. Para o tamanho atual do projeto, isso é mais simples do que marker interfaces e reflection.

HATEOAS simples:

As respostas incluem `_links` para navegação entre status, confirmação de upload e resultado. A montagem dos links fica na API, porque links conhecem rotas HTTP.

## Referências

- W3C Trace Context: https://www.w3.org/TR/trace-context/
- W3C Baggage: https://www.w3.org/TR/baggage/
- RFC 9457 Problem Details: https://www.rfc-editor.org/rfc/rfc9457.html
- OpenTelemetry: https://opentelemetry.io/docs/
- ASP.NET Core Response Compression: https://learn.microsoft.com/aspnet/core/performance/response-compression
- Amazon S3 presigned URLs: https://docs.aws.amazon.com/AmazonS3/latest/userguide/using-presigned-url.html
- Amazon SQS: https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html
- Amazon DynamoDB: https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html
