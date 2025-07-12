using Dapper;
using MoneyWise.Models;
using System.Data;

namespace MoneyWise.Services
{
    public class UserRepository
    {
        private readonly DatabaseService _db;

        public UserRepository(DatabaseService db)
        {
            _db = db;
        }
       
        public List<Users> GetAllUsers()
        {
            using var conn = _db.CreateConnection();
            var sql = @"SELECT ""UserID"", ""Username"", ""Email"", ""created_at"", ""ContactNumber"", ""Address"" FROM ""Users""";
            return conn.Query<Users>(sql).ToList();
        }

        public Users? GetUserById(int id)
        {
            using var conn = _db.CreateConnection();
            var sql = @"SELECT ""UserID"", ""Username"", ""Email"", ""created_at"", ""ContactNumber"", ""Address"" 
                        FROM ""Users"" WHERE ""UserID"" = @id";
            return conn.QuerySingleOrDefault<Users>(sql, new { id });
        }

        public void CreateUser(Users user)
        {
            using var conn = _db.CreateConnection();
            var sql = @"INSERT INTO ""Users"" (""Username"", ""Email"", ""ContactNumber"", ""Address"", ""created_at"") 
                        VALUES (@Username, @Email, @ContactNumber, @Address, NOW())";
            conn.Execute(sql, user);
        }

        public void UpdateUser(Users user)
        {
            using var conn = _db.CreateConnection();
            var sql = @"UPDATE ""Users"" 
                        SET ""Username"" = @Username, 
                            ""Password"" = @Email, 
                            ""ContactNumber"" = @ContactNumber, 
                            ""Address"" = @Address 
                        WHERE ""UserID"" = @UserID";
            conn.Execute(sql, user);
        }

        public void DeleteUser(int id)
        {
            using var conn = _db.CreateConnection();
            var sql = @"DELETE FROM ""Users"" WHERE ""UserID"" = @id";
            conn.Execute(sql, new { id });
        }
    }
}
