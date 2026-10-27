using Microsoft.Data.SqlClient;
using SneCafe.Models;

namespace SneCafe.Data
{
    public class UserRepository
    {
        private readonly string _connStr;

        public UserRepository(IConfiguration config)
        {
            _connStr = config.GetConnectionString("SneCafeDb")!;
        }

        private const string SelectUser =
            "SELECT Id, FullName, Username, PasswordHash, Role, IsActive FROM Users";

        private static AppUser Read(SqlDataReader r) => new AppUser
        {
            Id = r.GetInt32(0),
            FullName = r.GetString(1),
            Username = r.GetString(2),
            PasswordHash = r.GetString(3),
            Role = r.GetString(4),
            IsActive = r.GetBoolean(5)
        };

        public int Count()
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand("SELECT COUNT(*) FROM Users", conn);
            conn.Open();
            return (int)cmd.ExecuteScalar()!;
        }

        public AppUser? GetByUsername(string username)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(SelectUser + " WHERE Username = @U", conn);
            cmd.Parameters.AddWithValue("@U", username);
            conn.Open();

            using var r = cmd.ExecuteReader();
            return r.Read() ? Read(r) : null;
        }

        public bool UsernameExists(string username)
        {
            return GetByUsername(username) != null;
        }

        public List<AppUser> GetAll()
        {
            var list = new List<AppUser>();

            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(SelectUser + " ORDER BY Role, FullName", conn);
            conn.Open();

            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(Read(r));
            return list;
        }

        public void Create(string fullName, string username, string passwordHash, string role)
        {
            const string sql = @"
                INSERT INTO Users (FullName, Username, PasswordHash, Role)
                VALUES (@Name, @User, @Hash, @Role)";

            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Name", fullName);
            cmd.Parameters.AddWithValue("@User", username);
            cmd.Parameters.AddWithValue("@Hash", passwordHash);
            cmd.Parameters.AddWithValue("@Role", role);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void ToggleActive(int id)
        {
            const string sql = "UPDATE Users SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END WHERE Id = @Id";

            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}