using Microsoft.Data.SqlClient;
using SneCafe.Models;

namespace SneCafe.Data
{
    public class MenuRepository
    {
        private readonly string _connStr;

        public MenuRepository(IConfiguration config)
        {
            _connStr = config.GetConnectionString("SneCafeDb")!;
        }

        // Every item, including hidden ones
        public List<MenuRow> GetAll()
        {
            var list = new List<MenuRow>();

            const string sql = @"
                SELECT p.Id, p.Name, p.Price, c.Name, p.Icon, p.ImageUrl, p.HasOptions, p.IsAvailable,
                       (SELECT COUNT(*) FROM OrderItems oi WHERE oi.ProductId = p.Id) AS TimesOrdered
                FROM Products p
                JOIN Categories c ON c.Id = p.CategoryId
                ORDER BY c.Id, p.Name";

            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, conn);
            conn.Open();

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new MenuRow
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    Price = r.GetDecimal(2),
                    Category = r.GetString(3),
                    Icon = r.GetString(4),
                    ImageUrl = r.IsDBNull(5) ? null : r.GetString(5),
                    HasOptions = r.GetBoolean(6),
                    IsAvailable = r.GetBoolean(7),
                    TimesOrdered = r.GetInt32(8)
                });
            }

            return list;
        }

        public ProductEditModel? GetById(int id)
        {
            const string sql = @"
                SELECT Id, Name, CategoryId, Price, Icon, ImageUrl, HasOptions, IsAvailable
                FROM Products WHERE Id = @Id";

            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            conn.Open();

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            return new ProductEditModel
            {
                Id = r.GetInt32(0),
                Name = r.GetString(1),
                CategoryId = r.GetInt32(2),
                Price = r.GetDecimal(3),
                Icon = r.GetString(4),
                ImageUrl = r.IsDBNull(5) ? null : r.GetString(5),
                HasOptions = r.GetBoolean(6),
                IsAvailable = r.GetBoolean(7)
            };
        }

        public List<CategoryItem> GetCategories()
        {
            var list = new List<CategoryItem>();

            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand("SELECT Id, Name FROM Categories ORDER BY Id", conn);
            conn.Open();

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new CategoryItem { Id = r.GetInt32(0), Name = r.GetString(1) });

            return list;
        }

        // Is there another item with this name? (excludeId = the item being edited, or 0)
        public bool NameExists(string name, int excludeId)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Products WHERE Name = @Name AND Id <> @Id", conn);
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Id", excludeId);
            conn.Open();

            return (int)cmd.ExecuteScalar()! > 0;
        }

        // Does another item use this same picture?
        public bool IsImageUsed(string url, int exceptId)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Products WHERE ImageUrl = @Url AND Id <> @Id", conn);
            cmd.Parameters.AddWithValue("@Url", url);
            cmd.Parameters.AddWithValue("@Id", exceptId);
            conn.Open();

            return (int)cmd.ExecuteScalar()! > 0;
        }

        public void Create(ProductEditModel p)
        {
            const string sql = @"
                INSERT INTO Products (Name, Price, CategoryId, Icon, ImageUrl, HasOptions, IsAvailable)
                VALUES (@Name, @Price, @Cat, @Icon, @Img, @Opt, @Avail)";

            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, conn);
            AddParameters(cmd, p);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // Returns false if the item does not exist
        public bool Update(ProductEditModel p)
        {
            const string sql = @"
                UPDATE Products
                SET Name = @Name, Price = @Price, CategoryId = @Cat, Icon = @Icon,
                    ImageUrl = @Img, HasOptions = @Opt, IsAvailable = @Avail
                WHERE Id = @Id";

            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, conn);
            AddParameters(cmd, p);
            cmd.Parameters.AddWithValue("@Id", p.Id);
            conn.Open();

            return cmd.ExecuteNonQuery() == 1;
        }

        // Deletes the item ONLY if no order ever used it.
        // Returns false if it was not deleted (it has order history, or it does not exist).
        public bool Delete(int id)
        {
            const string sql = @"
                DELETE FROM Products
                WHERE Id = @Id
                  AND NOT EXISTS (SELECT 1 FROM OrderItems WHERE ProductId = @Id)";

            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            conn.Open();

            return cmd.ExecuteNonQuery() == 1;
        }

        public void ToggleAvailable(int id)
        {
            const string sql = @"
                UPDATE Products
                SET IsAvailable = CASE WHEN IsAvailable = 1 THEN 0 ELSE 1 END
                WHERE Id = @Id";

            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // Returns false if the category already exists
        public bool AddCategory(string name)
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();

            using (var check = new SqlCommand("SELECT COUNT(*) FROM Categories WHERE Name = @N", conn))
            {
                check.Parameters.AddWithValue("@N", name);
                if ((int)check.ExecuteScalar()! > 0) return false;
            }

            using var insert = new SqlCommand("INSERT INTO Categories (Name) VALUES (@N)", conn);
            insert.Parameters.AddWithValue("@N", name);
            insert.ExecuteNonQuery();
            return true;
        }

        private static void AddParameters(SqlCommand cmd, ProductEditModel p)
        {
            cmd.Parameters.AddWithValue("@Name", p.Name);
            cmd.Parameters.AddWithValue("@Price", p.Price);
            cmd.Parameters.AddWithValue("@Cat", p.CategoryId);
            cmd.Parameters.AddWithValue("@Icon", p.Icon);
            cmd.Parameters.AddWithValue("@Img", (object?)p.ImageUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Opt", p.HasOptions);
            cmd.Parameters.AddWithValue("@Avail", p.IsAvailable);
        }
    }
}