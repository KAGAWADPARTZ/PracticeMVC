using Microsoft.AspNetCore.Mvc;
using PracticeMVC.Models;
using PracticeMVC.Services;

namespace PracticeMVC.Controllers
{
    public class UsersController : Controller
    {
        private readonly UserRepository _userRepo;

        public UsersController(UserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public IActionResult Index()
        {
            var users = _userRepo.GetAllUsers();
            return View(users);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Users user)
        {
            _userRepo.CreateUser(user);
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var user = _userRepo.GetUserById(id);
            return View(user);
        }

        [HttpPost]
        public IActionResult Edit(Users user)
        {
            _userRepo.UpdateUser(user);
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            _userRepo.DeleteUser(id);
            return RedirectToAction("Index");
        }
    }
}
