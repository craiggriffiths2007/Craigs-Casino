using Casino.Data;
using Casino.Models;
using Casino.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Casino.Controllers
{
    [Authorize]
    public class GameController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SlotEngine _slotEngine;

        public GameController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            SlotEngine slotEngine)
        {
            _context = context;
            _userManager = userManager;
            _slotEngine = slotEngine;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var player = await _context.PlayerAccounts
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (player == null)
            {
                player = new PlayerAccount
                {
                    UserId = userId,
                    Credits = 10000,
                    Created = DateTime.UtcNow
                };

                _context.PlayerAccounts.Add(player);

                await _context.SaveChangesAsync();
            }

            return View(player);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Spin(long bet)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Unauthorized();

            if (bet <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid bet."
                });
            }

            var player = await _context.PlayerAccounts
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (player == null)
            {
                return BadRequest(new
                {
                    message = "Player account not found."
                });
            }

            if (player.Credits < bet)
            {
                return BadRequest(new
                {
                    message = "Not enough credits."
                });
            }

            // Deduct the bet first
            player.Credits -= bet;

            // Generate the result
            var result = _slotEngine.Spin(bet);

            // Add any winnings
            player.Credits += result.TotalWin;

            var spin = new Spin
            {
                UserId = userId,
                Bet = bet,
                Win = result.TotalWin,
                Result = JsonSerializer.Serialize(result),
                Created = DateTime.UtcNow
            };

            _context.Spins.Add(spin);

            await _context.SaveChangesAsync();

            return Json(new
            {
                initialBoard = result.InitialBoard,
                cascades = result.Cascades,
                win = result.TotalWin,
                balance = player.Credits
            });
        }

        private static string[][] ConvertSymbols(string[,] symbols)
        {
            var rows = new string[3][];

            for (int row = 0; row < 3; row++)
            {
                rows[row] = new string[5];

                for (int reel = 0; reel < 5; reel++)
                {
                    rows[row][reel] = symbols[row, reel];
                }
            }

            return rows;
        }
    }
}