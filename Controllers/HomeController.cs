using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Trackly.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Trackly.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Login", "Account");
        }
    }
}
