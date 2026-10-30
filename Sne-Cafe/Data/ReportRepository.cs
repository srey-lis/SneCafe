using Microsoft.Data.SqlClient;
using SneCafe.Models;

namespace Sne_Cafe.Data
{
    public class ReportRepository
    {
        private readonly string _connStr;

        public ReportRepository(IConfiguration config)
        {
            _connStr = config.GetConnectionString("SneCafeDb")!;
        }

        // start and end are both INCLUDED (whole days)
        public ReportViewModel Build(DateTime start, DateTime end, string preset)
        {
            var vm = new ReportViewModel { From = start, To = end, Preset = preset };

            DateTime today = DateTime.Today;
            DateTime weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));   // Monday
            DateTime monthStart = new DateTime(today.Year, today.Month, 1);
            DateTime toExclusive = end.Date.AddDays(1);     // "before the day after the last day"

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            // ---------- 1) Today / this week / this month ----------
            const string cardsSql = @"
                SELECT
                    ISNULL(SUM(CASE WHEN CreatedAt >= @Today THEN Total END), 0),
                    COUNT(CASE WHEN CreatedAt >= @Today THEN 1 END),
                    ISNULL(SUM(CASE WHEN CreatedAt >= @Week THEN Total END), 0),
                    COUNT(CASE WHEN CreatedAt >= @Week THEN 1 END),
                    ISNULL(SUM(CASE WHEN CreatedAt >= @Month THEN Total END), 0),
                    COUNT(CASE WHEN CreatedAt >= @Month THEN 1 END)
                FROM Orders
                WHERE Status <> 'Cancelled' AND CreatedAt >= @Earliest";

            using (var cmd = new SqlCommand(cardsSql, conn))
            {
                cmd.Parameters.AddWithValue("@Today", today);
                cmd.Parameters.AddWithValue("@Week", weekStart);
                cmd.Parameters.AddWithValue("@Month", monthStart);
                cmd.Parameters.AddWithValue("@Earliest", weekStart < monthStart ? weekStart : monthStart);

                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    vm.TodayIncome = r.GetDecimal(0);
                    vm.TodayOrders = r.GetInt32(1);
                    vm.WeekIncome = r.GetDecimal(2);
                    vm.WeekOrders = r.GetInt32(3);
                    vm.MonthIncome = r.GetDecimal(4);
                    vm.MonthOrders = r.GetInt32(5);
                }
            }

            // ---------- 2) Totals for the selected period ----------
            const string rangeSql = @"
                SELECT
                    ISNULL(SUM(CASE WHEN Status <> 'Cancelled' THEN Total END), 0),
                    COUNT(CASE WHEN Status <> 'Cancelled' THEN 1 END),
                    COUNT(CASE WHEN Status = 'Cancelled' THEN 1 END)
                FROM Orders
                WHERE CreatedAt >= @From AND CreatedAt < @To";

            using (var cmd = new SqlCommand(rangeSql, conn))
            {
                cmd.Parameters.AddWithValue("@From", start.Date);
                cmd.Parameters.AddWithValue("@To", toExclusive);

                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    vm.RangeIncome = r.GetDecimal(0);
                    vm.RangeOrders = r.GetInt32(1);
                    vm.RangeCancelled = r.GetInt32(2);
                }
            }

            // ---------- 3) Income per day (for the chart) ----------
            const string dailySql = @"
                SELECT CAST(CreatedAt AS DATE), SUM(Total), COUNT(*)
                FROM Orders
                WHERE Status <> 'Cancelled' AND CreatedAt >= @From AND CreatedAt < @To
                GROUP BY CAST(CreatedAt AS DATE)";

            var byDay = new Dictionary<DateTime, (decimal Income, int Orders)>();

            using (var cmd = new SqlCommand(dailySql, conn))
            {
                cmd.Parameters.AddWithValue("@From", start.Date);
                cmd.Parameters.AddWithValue("@To", toExclusive);

                using var r = cmd.ExecuteReader();
                while (r.Read())
                    byDay[r.GetDateTime(0).Date] = (r.GetDecimal(1), r.GetInt32(2));
            }

            // Days with no sales are missing from the query, so we add them as 0
            string fmt = start.Year == end.Year ? "dd MMM" : "dd MMM yy";
            for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
            {
                byDay.TryGetValue(d, out var v);
                vm.Days.Add(new DayTotal
                {
                    Date = d,
                    Label = d.ToString(fmt),
                    Income = v.Income,
                    Orders = v.Orders
                });
            }

            // ---------- 4) Cash vs Card ----------
            const string paySql = @"
                SELECT PaymentMethod, SUM(Total), COUNT(*)
                FROM Orders
                WHERE Status <> 'Cancelled' AND CreatedAt >= @From AND CreatedAt < @To
                GROUP BY PaymentMethod
                ORDER BY SUM(Total) DESC";

            using (var cmd = new SqlCommand(paySql, conn))
            {
                cmd.Parameters.AddWithValue("@From", start.Date);
                cmd.Parameters.AddWithValue("@To", toExclusive);

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    vm.Payments.Add(new PaymentTotal
                    {
                        Method = r.GetString(0),
                        Income = r.GetDecimal(1),
                        Orders = r.GetInt32(2)
                    });
                }
            }

            // ---------- 5) Best sellers ----------
            const string topSql = @"
                SELECT TOP 8 oi.ProductName, SUM(oi.Qty), SUM(oi.LineTotal)
                FROM OrderItems oi
                JOIN Orders o ON o.Id = oi.OrderId
                WHERE o.Status <> 'Cancelled' AND o.CreatedAt >= @From AND o.CreatedAt < @To
                GROUP BY oi.ProductName
                ORDER BY SUM(oi.Qty) DESC, SUM(oi.LineTotal) DESC";

            using (var cmd = new SqlCommand(topSql, conn))
            {
                cmd.Parameters.AddWithValue("@From", start.Date);
                cmd.Parameters.AddWithValue("@To", toExclusive);

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    vm.TopItems.Add(new TopItem
                    {
                        Name = r.GetString(0),
                        Qty = r.GetInt32(1),
                        Revenue = r.GetDecimal(2)
                    });
                }
            }

            return vm;
        }
    }
}