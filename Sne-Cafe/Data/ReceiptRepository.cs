using Microsoft.Data.SqlClient;
using SneCafe.Models;

namespace SneCafe.Data
{
    public class ReceiptRepository
    {
        private readonly string _connStr;

        public ReceiptRepository(IConfiguration config)
        {
            _connStr = config.GetConnectionString("SneCafeDb")!;
        }

        // Returns null if the order does not exist
        public ReceiptViewModel? GetReceipt(int id)
        {
            const string sql = @"
                SELECT o.Id, o.OrderNo, o.CreatedAt, o.Status,
                       o.Subtotal, o.Tax, o.Total, o.PaymentMethod, o.Note,
                       u.FullName,
                       oi.ProductName, oi.UnitPrice, oi.Qty,
                       oi.Size, oi.SugarLevel, oi.Toppings, oi.LineTotal
                FROM Orders o
                LEFT JOIN Users u ON u.Id = o.StaffId
                LEFT JOIN OrderItems oi ON oi.OrderId = o.Id
                WHERE o.Id = @Id
                ORDER BY oi.Id";

            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            conn.Open();

            ReceiptViewModel? receipt = null;

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                // First row: build the receipt header
                if (receipt == null)
                {
                    receipt = new ReceiptViewModel
                    {
                        Id = r.GetInt32(0),
                        OrderNo = r.GetString(1),
                        CreatedAt = r.GetDateTime(2),
                        Status = r.GetString(3),
                        Subtotal = r.GetDecimal(4),
                        Tax = r.GetDecimal(5),
                        Total = r.GetDecimal(6),
                        PaymentMethod = r.GetString(7),
                        Note = r.IsDBNull(8) ? null : r.GetString(8),
                        StaffName = r.IsDBNull(9) ? null : r.GetString(9)
                    };
                }

                // Every row: add one item (if the order has items)
                if (!r.IsDBNull(10))
                {
                    receipt.Items.Add(new ReceiptItem
                    {
                        ProductName = r.GetString(10),
                        UnitPrice = r.GetDecimal(11),
                        Qty = r.GetInt32(12),
                        Size = r.IsDBNull(13) ? null : r.GetString(13),
                        SugarLevel = r.IsDBNull(14) ? null : r.GetString(14),
                        Toppings = r.IsDBNull(15) ? null : r.GetString(15),
                        LineTotal = r.GetDecimal(16)
                    });
                }
            }

            return receipt;
        }
    }
}