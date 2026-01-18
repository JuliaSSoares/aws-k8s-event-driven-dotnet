#!/bin/bash
# Criar Tabela DynamoDB
awslocal dynamodb create-table \
    --table-name Pagamentos \
    --key-schema AttributeName=payment_id,KeyType=HASH \
    --attribute-definitions AttributeName=payment_id,AttributeType=S \
    --provisioned-throughput ReadCapacityUnits=5,WriteCapacityUnits=5

# Criar Tópico SNS
awslocal sns create-topic --name PaymentEvents

# Criar Fila SQS
awslocal sqs create-queue --queue-name ProcessamentoPagamento

# Assinar a Fila no Tópico (Fan-out)
awslocal sns subscribe \
    --topic-arn arn:aws:sns:us-east-1:000000000000:PaymentEvents \
    --protocol sqs \
    --notification-endpoint arn:aws:sqs:us-east-1:000000000000:ProcessamentoPagamento