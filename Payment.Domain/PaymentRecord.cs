using Amazon.DynamoDBv2.DataModel;

namespace Payment.Domain
{
    [DynamoDBTable("Pagamentos")]
    public class PaymentRecord
    {
        [DynamoDBHashKey("payment_id")] 
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
