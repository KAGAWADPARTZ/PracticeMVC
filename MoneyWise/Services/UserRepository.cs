using MoneyWise.Models;
using Microsoft.EntityFrameworkCore;

namespace MoneyWise.Services
{
    public class UserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<Users> GetAllUsers()
        {
            return _context.Users.ToList();
        }

        public Users? GetUserById(int id)
        {
            return _context.Users.Find(id);
        }

        public void CreateUser(Users user)
        {
            user.created_at = DateTime.UtcNow;
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        public void UpdateUser(Users user)
        {
            _context.Users.Update(user);
            _context.SaveChanges();
        }

        public void DeleteUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
        }
    }
}