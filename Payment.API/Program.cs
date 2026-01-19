using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.AspNetCore.Mvc;
using Amazon.Runtime;
using Payment.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var awsServiceUrl = builder.Configuration["AWS:ServiceURL"] ?? "http://localhost:4566";
var topicArn = builder.Configuration["AWS:TopicArn"] ?? "arn:aws:sns:us-east-1:000000000000:PaymentEvents";

var credentials = new BasicAWSCredentials("test", "test");

var dynamoConfig = new AmazonDynamoDBConfig
{
    ServiceURL = awsServiceUrl, 
    UseHttp = true,
    AuthenticationRegion = "us-east-1"
};

var snsConfig = new AmazonSimpleNotificationServiceConfig
{
    ServiceURL = awsServiceUrl, 
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

app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseAuthorization();

app.MapPost("/payments", async (
    [FromBody] PaymentRequest request,
    IDynamoDBContext context,
    IAmazonSimpleNotificationService snsClient) =>
{
    var payment = new PaymentRecord
    {
        Id = Guid.NewGuid().ToString(),
        Amount = request.Amount,
        Status = "Pending"
    };

    await context.SaveAsync(payment);
    var message = System.Text.Json.JsonSerializer.Serialize(payment);

    await snsClient.PublishAsync(new PublishRequest(topicArn, message));

    return Results.Ok(new { message = "Pagamento processado", id = payment.Id });
});


app.MapControllers();
app.Run();

public record PaymentRequest(decimal Amount);
