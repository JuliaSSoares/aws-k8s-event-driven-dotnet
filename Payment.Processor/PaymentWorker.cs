using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.DynamoDBv2.DataModel;
using System.Text.Json;
using Payment.Domain;


namespace Payment.Processor
{
    public class PaymentWorker : BackgroundService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly IDynamoDBContext _dynamoContext;
        private readonly string _queueUrl = "http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/ProcessamentoPagamento";

        public PaymentWorker(IAmazonSQS sqsClient, IDynamoDBContext dynamoContext)
        {
            _sqsClient = sqsClient;
            _dynamoContext = dynamoContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    WaitTimeSeconds = 10, // Long Polling
                    MaxNumberOfMessages = 1
                };

                var response = await _sqsClient.ReceiveMessageAsync(request, stoppingToken);

                foreach (var message in response.Messages)
                {
                    await ProcessMessageAsync(message, stoppingToken);
                }
            }
        }

        private async Task ProcessMessageAsync(Message message, CancellationToken ct)
        {
            // O SNS envia um JSON que contém o campo "Message" com o nosso objeto
            var snsEnvelope = JsonDocument.Parse(message.Body);
            var content = snsEnvelope.RootElement.GetProperty("Message").GetString();
            var paymentData = JsonSerializer.Deserialize<PaymentRecord>(content!);

            if (paymentData == null)
            {
                // Log or handle the case where deserialization failed
                return;
            }

            // --- PULO DO GATO: IDEMPOTÊNCIA ---
            var payment = await _dynamoContext.LoadAsync<PaymentRecord>(paymentData.Id, ct);

            if (payment == null || payment.Status != "Pending")
            {
                // Já processado anteriormente ou não encontrado, apenas removemos da fila
                await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, ct);
                return;
            }

            // Simulação de Validação/Processamento
            payment.Status = "Approved";
            await _dynamoContext.SaveAsync(payment, ct);

            // Remove da fila após sucesso
            await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, ct);
        }

    }
}
