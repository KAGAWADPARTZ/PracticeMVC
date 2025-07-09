using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace MoneyWise.Services
{
    public class DatabaseService(IConfiguration configuration)
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "NoConnection" ;

        public IDbConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}