using Microsoft.Data.SqlClient;
using SneCafe.Models;


namespace SneCafe.Data
{
    public class ProductRepository
    {
        private readonly string _connStr;

        public ProductRepository(IConfiguration config)
        {
            _connStr = config.GetConnectionString("SneCafeDb")!;
        }

        public List<Product> GetAvailable()
        {
            var list = new List<Product>();

            const string sql = @"
                SELECT p.Id, p.Name, p.Price, c.Name AS Category,
                       p.Icon, p.ImageUrl, p.HasOptions
                FROM Products p
                JOIN Categories c ON c.Id = p.CategoryId
                WHERE p.IsAvailable = 1
                ORDER BY c.Id, p.Name";

            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, conn);
            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Product
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Price = reader.GetDecimal(2),
                    Category = reader.GetString(3),
                    Icon = reader.GetString(4),
                    ImageUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
                    HasOptions = reader.GetBoolean(6)
                });
            }

            return list;
        }
    }
}