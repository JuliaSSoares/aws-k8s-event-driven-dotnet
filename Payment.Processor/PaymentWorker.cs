using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.DynamoDBv2.DataModel;
using System.Text.Json;
using Payment.Domain;


namespace Payment.Processor
{
    public class PaymentWorker(IAmazonSQS sqsClient, IDynamoDBContext dynamoContext, IConfiguration configuration) : BackgroundService
    {
        private readonly IAmazonSQS _sqsClient = sqsClient;
        private readonly IDynamoDBContext _dynamoContext = dynamoContext;
        private readonly string _queueUrl = configuration.GetValue<string>("AWS:SQS:QueueUrl")
                        ?? "http://host.docker.internal:4566/000000000000/ProcessamentoPagamento";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"Worker iniciado. Escutando a fila: {_queueUrl}");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var request = new ReceiveMessageRequest
                    {
                        QueueUrl = _queueUrl,
                        WaitTimeSeconds = 10,
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
            var snsEnvelope = JsonDocument.Parse(message.Body);
            var content = snsEnvelope.RootElement.GetProperty("Message").GetString();
            var paymentData = JsonSerializer.Deserialize<PaymentRecord>(content!);

            if (paymentData == null)
            {
                return;
            }

            var payment = await _dynamoContext.LoadAsync<PaymentRecord>(paymentData.Id, ct);

            if (payment == null || payment.Status != "Pending")
            {
                await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, ct);
                return;
            }

                payment.Status = "Approved";
                await _dynamoContext.SaveAsync(payment, ct);

                Console.WriteLine("NOTIFICA��O: E-mail de confirma��o enviado para o pagamento {Id}", payment.Id);
                
                await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, ct);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar mensagem {ex.Message}");
            }
        }
    }
}
