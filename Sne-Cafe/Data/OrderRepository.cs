using Microsoft.Data.SqlClient;
using SneCafe.Models;

namespace SneCafe.Data
{
    public class OrderRepository
    {
        private readonly string _connStr;

        public OrderRepository(IConfiguration config)
        {
            _connStr = config.GetConnectionString("SneCafeDb")!;
        }

        // One checked-and-priced line of the order
        private class Line
        {
            public int ProductId;
            public string Name = "";
            public decimal UnitPrice;
            public int Qty;
            public string? Size;
            public string? Sugar;
            public string? Toppings;
        }

        public (int Id, string OrderNo) Create(OrderRequest req, int? staffId = null)
        {
            if (req.Items.Count == 0)
                throw new Exception("The cart is empty.");

            using var conn = new SqlConnection(_connStr);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                // 1) Build every line using prices from the DATABASE
                var lines = new List<Line>();

                foreach (var item in req.Items)
                {
                    if (item.Qty < 1 || item.Qty > 99)
                        throw new Exception("Invalid quantity.");

                    using var pc = new SqlCommand(
                        "SELECT Name, Price, HasOptions FROM Products WHERE Id = @Id AND IsAvailable = 1",
                        conn, tx);
                    pc.Parameters.AddWithValue("@Id", item.ProductId);

                    using var r = pc.ExecuteReader();
                    if (!r.Read())
                        throw new Exception("A product in the cart is no longer available.");

                    string name = r.GetString(0);
                    decimal price = r.GetDecimal(1);
                    bool hasOptions = r.GetBoolean(2);

                    var line = new Line
                    {
                        ProductId = item.ProductId,
                        Name = name,
                        UnitPrice = price,
                        Qty = item.Qty
                    };

                    if (hasOptions)
                    {
                        var size = MenuOptions.Sizes.FirstOrDefault(s => s.Name == item.Size)
                                   ?? throw new Exception("Invalid size.");

                        if (!MenuOptions.SugarLevels.Contains(item.Sugar ?? ""))
                            throw new Exception("Invalid sugar level.");

                        var tops = new List<ToppingOption>();
                        foreach (var topName in item.Toppings.Distinct())
                        {
                            var t = MenuOptions.Toppings.FirstOrDefault(x => x.Name == topName)
                                    ?? throw new Exception("Invalid topping.");
                            tops.Add(t);
                        }

                        line.UnitPrice = price + size.Extra + tops.Sum(t => t.Price);
                        line.Size = size.Name;
                        line.Sugar = item.Sugar;
                        line.Toppings = tops.Count > 0 ? string.Join(", ", tops.Select(t => t.Name)) : null;
                    }

                    lines.Add(line);
                }

                // 2) Totals
                decimal subtotal = lines.Sum(l => l.UnitPrice * l.Qty);
                decimal tax = Math.Round(subtotal * MenuOptions.TaxRate, 2, MidpointRounding.AwayFromZero);
                decimal total = subtotal + tax;
                string payment = req.PaymentMethod == "Card" ? "Card" : "Cash";

                string? note = req.Note?.Trim();
                if (string.IsNullOrEmpty(note)) note = null;
                if (note != null && note.Length > 200) note = note.Substring(0, 200);

                // 3) Insert the order (temporary order number, fixed in step 4)
                const string insertOrder = @"
                    INSERT INTO Orders (OrderNo, StaffId, Subtotal, Tax, Total, PaymentMethod, Note)
                    VALUES (@OrderNo, @StaffId, @Subtotal, @Tax, @Total, @Payment, @Note);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int orderId;
                using (var cmd = new SqlCommand(insertOrder, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@OrderNo", "T" + Guid.NewGuid().ToString("N").Substring(0, 12));
                    cmd.Parameters.AddWithValue("@StaffId", (object?)staffId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Subtotal", subtotal);
                    cmd.Parameters.AddWithValue("@Tax", tax);
                    cmd.Parameters.AddWithValue("@Total", total);
                    cmd.Parameters.AddWithValue("@Payment", payment);
                    cmd.Parameters.AddWithValue("@Note", (object?)note ?? DBNull.Value);
                    orderId = (int)cmd.ExecuteScalar()!;
                }

                // 4) Real order number from the new Id: SNE-0003
                string orderNo = "SNE-" + orderId.ToString("D4");
                using (var cmd = new SqlCommand("UPDATE Orders SET OrderNo = @No WHERE Id = @Id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@No", orderNo);
                    cmd.Parameters.AddWithValue("@Id", orderId);
                    cmd.ExecuteNonQuery();
                }

                // 5) Insert each line
                const string insertItem = @"
                    INSERT INTO OrderItems (OrderId, ProductId, ProductName, UnitPrice, Qty, Size, SugarLevel, Toppings)
                    VALUES (@OrderId, @ProductId, @Name, @UnitPrice, @Qty, @Size, @Sugar, @Toppings);";

                foreach (var l in lines)
                {
                    using var cmd = new SqlCommand(insertItem, conn, tx);
                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                    cmd.Parameters.AddWithValue("@ProductId", l.ProductId);
                    cmd.Parameters.AddWithValue("@Name", l.Name);
                    cmd.Parameters.AddWithValue("@UnitPrice", l.UnitPrice);
                    cmd.Parameters.AddWithValue("@Qty", l.Qty);
                    cmd.Parameters.AddWithValue("@Size", (object?)l.Size ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Sugar", (object?)l.Sugar ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Toppings", (object?)l.Toppings ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                return (orderId, orderNo);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}