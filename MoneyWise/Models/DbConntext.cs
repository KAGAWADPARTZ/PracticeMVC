using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace MoneyWise.Models
{
    public class AppDbContext : DbContext
    {
        
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<Users> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Savings> Savings { get; set; }
    }
}