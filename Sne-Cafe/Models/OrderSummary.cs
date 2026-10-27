namespace SneCafe.Models
{
    // One row in the Orders list
    public class OrderSummary
    {
        public int Id { get; set; }
        public string OrderNo { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "";
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = "";
        public string? Note { get; set; }
        public string ItemsText { get; set; } = "";   // "Latte x1, Bagel x1"
    }

    // Everything the Orders page needs
    public class OrdersViewModel
    {
        public List<OrderSummary> Orders { get; set; } = new();
        public string Range { get; set; } = "today";   // "today" or "all"

        public int InProgress => Orders.Count(o => o.Status == "Pending" || o.Status == "Preparing");
        public int Completed => Orders.Count(o => o.Status == "Completed");
        public decimal Sales => Orders.Where(o => o.Status != "Cancelled").Sum(o => o.Total);
    }
}