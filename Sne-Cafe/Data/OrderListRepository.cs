using Microsoft.Data.SqlClient;
using SneCafe.Models;

namespace SneCafe.Data
{
    public class OrderListRepository
    {
        private readonly string _connStr;

        public OrderListRepository(IConfiguration config)
        {
            _connStr = config.GetConnectionString("SneCafeDb")!;
        }

        // Newest first. all = false means only today's orders.
        public List<OrderSummary> GetOrders(bool all)
        {
            var list = new List<OrderSummary>();
            var map = new Dictionary<int, OrderSummary>();

            const string sql = @"
                SELECT o.Id, o.OrderNo, o.CreatedAt, o.Status, o.Total,
                       o.PaymentMethod, o.Note,
                       oi.ProductName, oi.Qty
                FROM Orders o
                LEFT JOIN OrderItems oi ON oi.OrderId = o.Id
                WHERE (@All = 1 OR o.CreatedAt >= @From)
                ORDER BY o.Id DESC, oi.Id";

            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@All", all ? 1 : 0);
            cmd.Parameters.AddWithValue("@From", DateTime.Today);
            conn.Open();

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                int id = r.GetInt32(0);

                // The query returns one row per item, so we group the rows by order
                if (!map.TryGetValue(id, out var order))
                {
                    order = new OrderSummary
                    {
                        Id = id,
                        OrderNo = r.GetString(1),
                        CreatedAt = r.GetDateTime(2),
                        Status = r.GetString(3),
                        Total = r.GetDecimal(4),
                        PaymentMethod = r.GetString(5),
                        Note = r.IsDBNull(6) ? null : r.GetString(6)
                    };
                    map[id] = order;
                    list.Add(order);
                }

                if (!r.IsDBNull(7))
                {
                    string item = r.GetString(7) + " x" + r.GetInt32(8);
                    order.ItemsText = order.ItemsText == "" ? item : order.ItemsText + ", " + item;
                }
            }

            return list;
        }

        // Only allows the legal moves. Returns false if the move is not allowed.
        public bool ChangeStatus(int id, string newStatus)
        {
            string? sql = newStatus switch
            {
                "Preparing" => "UPDATE Orders SET Status = 'Preparing' WHERE Id = @Id AND Status = 'Pending'",
                "Completed" => "UPDATE Orders SET Status = 'Completed' WHERE Id = @Id AND Status = 'Preparing'",
                "Cancelled" => "UPDATE Orders SET Status = 'Cancelled' WHERE Id = @Id AND Status IN ('Pending', 'Preparing')",
                _ => null
            };

            if (sql == null) return false;

            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            conn.Open();

            return cmd.ExecuteNonQuery() == 1;   // 1 row changed = success
        }
    }
}