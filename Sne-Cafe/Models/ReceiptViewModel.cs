namespace SneCafe.Models
{
    public class ReceiptItem
    {
        public string ProductName { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Qty { get; set; }
        public string? Size { get; set; }
        public string? SugarLevel { get; set; }
        public string? Toppings { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class ReceiptViewModel
    {
        public int Id { get; set; }
        public string OrderNo { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "";
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = "";
        public string? Note { get; set; }
        public string? StaffName { get; set; }
        public List<ReceiptItem> Items { get; set; } = new();
    }
}