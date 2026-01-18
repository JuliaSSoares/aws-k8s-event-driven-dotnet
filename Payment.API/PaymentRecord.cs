using Amazon.DynamoDBv2.DataModel;

namespace Payment.API
{
    [DynamoDBTable("Pagamentos")]
    public class PaymentRecord
    {
        [DynamoDBHashKey("payment_id")] // Mapeia explicitamente para o nome no Dynamo
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
