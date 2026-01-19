# AWS Event-Driven Architecture with .NET 8 & Kubernetes (Kind)

Este projeto demonstra uma arquitetura robusta de microserviços orientada a eventos, utilizando **.NET 8**, simulando serviços da **AWS** localmente e orquestrando tudo dentro de um cluster **Kubernetes** local.

## 🚀 Arquitetura do Sistema

O fluxo de dados segue o padrão de desacoplamento para garantir alta disponibilidade e resiliência:

1.  **Payment.API**: Recebe uma requisição POST, salva o pagamento com status `Pending` no **DynamoDB** e publica um evento no **SNS**.
2.  **SNS -> SQS**: O SNS encaminha a mensagem para uma fila **SQS** (Fan-out pattern).
3.  **Payment.Processor**: Um Worker Service (BackgroundService) que escuta a fila SQS, processa o pagamento e atualiza o status no DynamoDB para `Approved`.

## 🛠 Tecnologias Utilizadas

* **Linguagem**: .NET 8 (C#)
* **Mensageria**: AWS SNS & SQS (via LocalStack)
* **Banco de Dados**: Amazon DynamoDB
* **Orquestração**: Kubernetes (Kind)
* **Containers**: Docker

## 📦 Como Rodar o Projeto

### 1. Pré-requisitos
* Docker Desktop
* Kind (Kubernetes in Docker)
* kubectl
* AWS CLI (configurado com credenciais 'test')

### 2. Subir o Ambiente AWS (LocalStack)
```powershell
# Inicie o LocalStack via Docker Compose
docker-compose up -d


#Criar o Cluster Kubernetes
kind create cluster --name payment-system


# Build das imagens
docker build -t payment-api:latest .
docker build -t payment-processor:latest .

# Carregar imagens para dentro do Kind
kind load docker-image payment-api:latest --name payment-system
kind load docker-image payment-processor:latest --name payment-system

# Aplicar manifestos
kubectl apply -f k8s/

