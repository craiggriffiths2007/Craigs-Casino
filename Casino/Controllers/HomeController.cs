using Casino.Data;
using Casino.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Casino.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                var player = userId == null
                    ? null
                    : await _context.PlayerAccounts.FirstOrDefaultAsync(p => p.UserId == userId);

                ViewBag.Credits = player?.Credits ?? 10000;
            }

            return View();
        }

        [Authorize]
        public async Task<IActionResult> Lobby()
        {
            var userId = _userManager.GetUserId(User);
            var player = userId == null ? null : await _context.PlayerAccounts.FirstOrDefaultAsync(p => p.UserId == userId);
            ViewBag.Credits = player?.Credits ?? 10000;
            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
