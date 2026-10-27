namespace SneCafe.Models
{
    public class OrderRequest
    {
        public string? Note { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public List<OrderItemRequest> Items { get; set; } = new();
    }

    public class OrderItemRequest
    {
        public int ProductId { get; set; }
        public int Qty { get; set; }
        public string? Size { get; set; }
        public string? Sugar { get; set; }
        public List<string> Toppings { get; set; } = new();
    }
}