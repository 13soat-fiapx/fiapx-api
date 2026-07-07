# FIAP X API Helm chart

Chart Kubernetes da API REST `fiapx-api`.

Este serviço HTTP usa `Deployment`, `Service`, `Ingress` e `HPA`. Não use `ScaledJob` nesta pasta; `ScaledJob` é exclusivo dos workers que consomem SQS.

O secret `{{ .Values.app.name }}-aws` é replicado pelo Reflector a partir de `external-secrets/aws-credentials`, conforme a infraestrutura compartilhada do projeto.

`StorageOptions__PublicServiceUrl` não é usado no deploy Kubernetes/AWS. Essa configuração é exclusiva do ambiente local com LocalStack, onde a API assina URLs usando o endpoint interno do container e precisa devolver uma URL acessível pelo navegador.
