using Dapper;
using MoneyWise.Models;
using System.Data;

namespace MoneyWise.Services
{
    public class TransactionRepository
    {
        private readonly DatabaseService _db;

        public TransactionRepository(DatabaseService db)
        {
            _db = db;
        }

        public List<Transaction> GetUserTransaction()
        {
            using var conn = _db.CreateConnection();
            var sql = @"SELECT ""TransactionID"", ""UserID"", ""created_at"" , ""created_at"" FROM ""Transactions""";
            return conn.Query<Transaction>(sql).ToList();
        }

        public Transaction? GetUserTransactionById(int id)
        {
            using var conn = _db.CreateConnection();
            var sql = @"SELECT ""TransactionID"", ""UserID"", ""created_at"" ""updated_at"" FROM ""Transaction"" WHERE ""UserID"" = @id";
            return conn.QuerySingleOrDefault<Transaction>(sql, new { id });
        }

        public void CreateTransaction(Transaction transaction)
        {
            using var conn = _db.CreateConnection();
            var sql = @"INSERT INTO ""Transations"" (""UserID"", ""Savings"", ""Wants"",""Needs"",""Investment"",  ""created_at"") 
                    VALUES (@Username, @Email, NOW())";
            conn.Execute(sql, transaction);
        }

        public void UpdateUser(Users user)
        {
            using var conn = _db.CreateConnection();
            var sql = @"UPDATE ""Transaction"" 
                    SET ""Username"" = @Username, 
                        ""Email"" = @Email
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