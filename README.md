# fiapx-api

API REST do FIAP X para cadastro de processamento de videos, upload direto no S3, consulta de status assíncrono e download do ZIP final com frames extraídos.

A implementação segue os contratos em `../fiapx-docs/docs/contracts/specs`:

- REST: `openapi.yaml`.
- Mensageria: `asyncapi.yaml`.

## Fluxo principal

1. Cliente cadastra um processing job na API.
2. API grava o job no DynamoDB com status `upload_pending`.
3. API devolve uma URL assinada para upload direto no S3.
4. Cliente faz `PUT` do video no S3 usando a URL assinada.
5. Cliente confirma o upload na API.
6. API valida o objeto no S3, muda o status para `queued` e publica `video.processing.requested` na fila `video-processing-requested`.
7. O `fiapx-processor` consome a fila, extrai frames e atualiza progresso no DynamoDB.
8. O `fiapx-processor` grava o ZIP no S3 e publica `video.processing.completed`.
9. O `fiapx-notifier` envia o e-mail.
10. Cliente consulta a API e baixa o ZIP por redirecionamento `303 See Other`.

## Rodando local com Docker

Pré-requisitos:

- Docker Desktop.
- .NET 8 SDK, apenas se for rodar fora do Docker.
- AWS CLI, apenas para inspecionar recursos LocalStack.

Suba API + LocalStack:

```powershell
docker compose up --build
```

Acesse:

- API: `http://localhost:5000/api`.
- Swagger: `http://localhost:5000/api/swagger`.
- LocalStack: `http://localhost:4566`.

O LocalStack cria automaticamente:

- Bucket S3: `fiapx-dev-artifacts-000000000000`.
- Prefixos S3: `videos/` e `frames/`.
- Tabela DynamoDB: `fiapx-dev-videos-db`.
- Fila SQS: `fiapx-dev-video-processing-requested`.
- DLQ SQS: `fiapx-dev-video-processing-requested-dlq`.
- Fila SQS: `fiapx-dev-video-processing-completed`.
- DLQ SQS: `fiapx-dev-video-processing-completed-dlq`.

## Rodando local sem Docker

Suba apenas o LocalStack:

```powershell
docker compose up localstack
```

Rode a API pelo .NET:

```powershell
$env:AppInfo__RoutePrefix="api"
dotnet run --project src/FiapX.Api/FiapX.Api.csproj --urls http://localhost:5000
```

## Teste manual do fluxo

Crie um processing job:

```powershell
curl -X POST http://localhost:5000/api/v1/processing-jobs `
  -H "Content-Type: application/json" `
  -H "Authorization: Bearer <jwt-com-sub-name-email>" `
  -d '{"inputFile":{"originalFileName":"sample.mp4","contentType":"video/mp4","sizeBytes":12345},"description":"Demo video","author":"Local User","clientReference":"demo-001"}'
```

A resposta contém `id` e `upload.url`. Faça upload do arquivo para essa URL:

```powershell
curl -X PUT "<upload.url>" `
  -H "Content-Type: video/mp4" `
  --data-binary "@C:\\caminho\\para\\sample.mp4"
```

Confirme o upload:

```powershell
curl -X POST http://localhost:5000/api/v1/processing-jobs/<id>/upload-completion `
  -H "Content-Type: application/json" `
  -H "Authorization: Bearer <jwt-com-sub-name-email>" `
  -H "Idempotency-Key: demo-upload-001" `
  -d '{}'
```

Consulte o status:

```powershell
curl http://localhost:5000/api/v1/processing-jobs/<id> `
  -H "Authorization: Bearer <jwt-com-sub-name-email>"
```

Inspecione a mensagem publicada na fila local:

```powershell
aws --endpoint-url=http://localhost:4566 sqs receive-message `
  --region us-east-1 `
  --queue-url http://localhost:4566/000000000000/fiapx-dev-video-processing-requested `
  --attribute-names All `
  --message-attribute-names All
```

## Configuração AWS Student

No AWS Student/Lab, os recursos reais não são criados por scripts dentro do `fiapx-api`. O padrão do projeto é usar o repositório `fiapx-infra`, que provisiona S3, SQS, DynamoDB, ECR, EKS, secrets e gateway por Terraform.

```powershell
cd ..\fiapx-infra
aws configure
.\scripts\initialize-infrastructure.ps1 dev
```

Depois disso, publique a imagem da API no ECR e instale o chart em `k8s/`, informando `image.repository`, `image.tag`, `app.env` e `storage.bucketName`. O chart da API segue o padrão `Deployment + Service + Ingress + HPA`.

## Documentação complementar

Leia o guia do projeto em `docs/PROJECT_GUIDE.md`.

## Validação

```powershell
dotnet build FiapX.Api.sln
docker compose config
```

## Padrões alinhados aos projetos de referência

- CrossCutting dividido em `AddDataRepositories`, `AddStorage`, `AddMessaging`, `AddAppServices` e `AddRequestValidators`.
- Registros explícitos para app services e repositórios, evitando reflection desnecessária neste contexto.
- `MessagePublisher`, `MessageBase<T>` e `QueueUrlResolver` próximos ao `fiapx-notifier`.
- FluentValidation com `RequestValidationFilter`, como nos projetos Mechanics.
