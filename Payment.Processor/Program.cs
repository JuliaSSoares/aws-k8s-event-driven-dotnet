using Amazon.Runtime;
using Amazon.SQS;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Payment.Processor;

var builder = Host.CreateApplicationBuilder(args);

var credentials = new BasicAWSCredentials("test", "test");
var config = new AmazonSQSConfig { ServiceURL = "http://localhost:4566" };
var ddbConfig = new AmazonDynamoDBConfig { ServiceURL = "http://localhost:4566" };

builder.Services.AddSingleton<IAmazonSQS>(new AmazonSQSClient(credentials, config));
builder.Services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient(credentials, ddbConfig));
builder.Services.AddScoped<IDynamoDBContext, DynamoDBContext>();

builder.Services.AddHostedService<PaymentWorker>();

var host = builder.Build();
host.Run();