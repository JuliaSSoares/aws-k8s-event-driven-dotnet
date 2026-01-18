using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.AspNetCore.Mvc;
using Amazon.Runtime;
using Payment.Domain;

var builder = WebApplication.CreateBuilder(args);

// --- 1. REGISTRO DE SERVIÇOS ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var credentials = new BasicAWSCredentials("test", "test");

var dynamoConfig = new AmazonDynamoDBConfig
{
    ServiceURL = "http://localhost:4566",
    UseHttp = true,
    AuthenticationRegion = "us-east-1"
};

var snsConfig = new AmazonSimpleNotificationServiceConfig
{
    ServiceURL = "http://localhost:4566",
    UseHttp = true,
    AuthenticationRegion = "us-east-1"
};

builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
    new AmazonDynamoDBClient(credentials, dynamoConfig)
);

builder.Services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
    new AmazonSimpleNotificationServiceClient(credentials, snsConfig)
);

builder.Services.AddScoped<IDynamoDBContext, DynamoDBContext>();

var app = builder.Build();

// --- 2. MIDDLEWARES ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// --- 3. ENDPOINT POST /PAYMENTS ---
app.MapPost("/payments", async (
    [FromBody] PaymentRequest request,
    IDynamoDBContext context,
    IAmazonSimpleNotificationService snsClient) =>
{
    // Criar objeto baseado no seu script (payment_id)
    var payment = new PaymentRecord
    {
        Id = Guid.NewGuid().ToString(),
        Amount = request.Amount,
        Status = "Pending"
    };

    // Passo 1: Salvar no DynamoDB
    await context.SaveAsync(payment);

    // Passo 2: Publicar no SNS (ARN conforme seu script)
    var topicArn = "arn:aws:sns:us-east-1:000000000000:PaymentEvents";
    var message = System.Text.Json.JsonSerializer.Serialize(payment);

    await snsClient.PublishAsync(new PublishRequest(topicArn, message));

    return Results.Ok(new { message = "Pagamento processado", id = payment.Id });
});

app.MapControllers();
app.Run();

public record PaymentRequest(decimal Amount);