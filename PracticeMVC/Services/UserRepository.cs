using Dapper;
using PracticeMVC.Models;
using System.Data;

namespace PracticeMVC.Services
{
    public class UserRepository
    {
        private readonly DatabaseService _db;

        public UserRepository(DatabaseService db)
        {
            _db = db;
        }
        public List<Users> IsUserRegistered(string username, string password)
        {
            string msg = "You are already register ";
            using var conn = _db.CreateConnection();
            var sql = @"SELECT  ""Username"", ""Password""
                        FROM ""Users"" 
                        WHERE ""Username"" = @username AND ""Password"" = @password";
            return conn.Query<Users>(sql, new { username, password }).ToList();
        }

        public List<Users> GetAllUsers()
        {
            using var conn = _db.CreateConnection();
            var sql = @"SELECT ""UserID"", ""Username"", ""Password"", ""created_at"", ""ContactNumber"", ""Address"" FROM ""Users""";
            return conn.Query<Users>(sql).ToList();
        }

        public Users? GetUserById(int id)
        {
            using var conn = _db.CreateConnection();
            var sql = @"SELECT ""UserID"", ""Username"", ""Password"", ""created_at"", ""ContactNumber"", ""Address"" 
                        FROM ""Users"" WHERE ""UserID"" = @id";
            return conn.QuerySingleOrDefault<Users>(sql, new { id });
        }

        public void CreateUser(Users user)
        {
            using var conn = _db.CreateConnection();
            var sql = @"INSERT INTO ""Users"" (""Username"", ""Password"", ""ContactNumber"", ""Address"", ""created_at"") 
                        VALUES (@Username, @Password, @ContactNumber, @Address, NOW())";
            conn.Execute(sql, user);
        }

        public void UpdateUser(Users user)
        {
            using var conn = _db.CreateConnection();
            var sql = @"UPDATE ""Users"" 
                        SET ""Username"" = @Username, 
                            ""Password"" = @Password, 
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
