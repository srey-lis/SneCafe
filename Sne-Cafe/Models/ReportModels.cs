namespace SneCafe.Models
{
    public class DayTotal
    {
        public DateTime Date { get; set; }
        public string Label { get; set; } = "";
        public decimal Income { get; set; }
        public int Orders { get; set; }
    }

    public class TopItem
    {
        public string Name { get; set; } = "";
        public int Qty { get; set; }
        public decimal Revenue { get; set; }
    }

    public class PaymentTotal
    {
        public string Method { get; set; } = "";
        public decimal Income { get; set; }
        public int Orders { get; set; }
    }

    public class ReportViewModel
    {
        // The three fixed cards
        public decimal TodayIncome { get; set; }
        public int TodayOrders { get; set; }
        public decimal WeekIncome { get; set; }
        public int WeekOrders { get; set; }
        public decimal MonthIncome { get; set; }
        public int MonthOrders { get; set; }

        // The selected period (affects everything below the cards)
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string Preset { get; set; } = "7";    // "7", "30", "month" or "custom"

        public decimal RangeIncome { get; set; }
        public int RangeOrders { get; set; }
        public int RangeCancelled { get; set; }
        public decimal AverageOrder => RangeOrders > 0 ? RangeIncome / RangeOrders : 0;

        public List<DayTotal> Days { get; set; } = new();
        public List<TopItem> TopItems { get; set; } = new();
        public List<PaymentTotal> Payments { get; set; } = new();
    }
}