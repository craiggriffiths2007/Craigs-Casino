using Casino.Data;
using Casino.Models;
using Casino.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
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
        private readonly ReelCatchEngine _reelCatchEngine;

        public GameController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            SlotEngine slotEngine,
            ReelCatchEngine reelCatchEngine)
        {
            _context = context;
            _userManager = userManager;
            _slotEngine = slotEngine;
            _reelCatchEngine = reelCatchEngine;
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



        public async Task<IActionResult> ReelCatch()
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

            ViewBag.FreeSpinsRemaining = HttpContext.Session.GetInt32("ReelCatch.FreeSpins") ?? 0;
            ViewBag.CollectorCount = HttpContext.Session.GetInt32("ReelCatch.Collectors") ?? 0;
            ViewBag.CollectorMultiplier = HttpContext.Session.GetInt32("ReelCatch.Multiplier") ?? 1;
            ViewBag.BonusBet = HttpContext.Session.GetInt32("ReelCatch.Bet") ?? 0;

            return View(player);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReelCatchSpin(long bet, bool freeSpin = false)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Unauthorized();

            var player = await _context.PlayerAccounts
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (player == null)
                return BadRequest(new { message = "Player account not found." });

            var freeSpinsRemaining = HttpContext.Session.GetInt32("ReelCatch.FreeSpins") ?? 0;
            var collectorCount = HttpContext.Session.GetInt32("ReelCatch.Collectors") ?? 0;
            var collectorMultiplier = HttpContext.Session.GetInt32("ReelCatch.Multiplier") ?? 1;
            var bonusBet = HttpContext.Session.GetInt32("ReelCatch.Bet") ?? 0;

            if (freeSpin)
            {
                if (freeSpinsRemaining <= 0 || bonusBet <= 0)
                    return BadRequest(new { message = "No free spins are available." });

                bet = bonusBet;
                freeSpinsRemaining--;
            }
            else
            {
                if (freeSpinsRemaining > 0)
                    return BadRequest(new { message = "Finish the free spins before placing another bet." });

                if (bet < 10 || bet > 1000 || bet % 10 != 0)
                    return BadRequest(new { message = "Bet must be between 10 and 1000 credits in steps of 10." });

                if (player.Credits < bet)
                    return BadRequest(new { message = "Not enough credits." });

                player.Credits -= bet;
                collectorCount = 0;
                collectorMultiplier = 1;
            }

            var result = _reelCatchEngine.Spin(bet, freeSpin, collectorMultiplier);
            var extraFreeSpins = 0;
            var previousCollectorCount = collectorCount;

            if (freeSpin && result.FishermenLanded > 0)
            {
                collectorCount += result.FishermenLanded;

                if (previousCollectorCount < 4 && collectorCount >= 4)
                {
                    collectorMultiplier = 2;
                    extraFreeSpins += 5;
                }
                if (previousCollectorCount < 8 && collectorCount >= 8)
                {
                    collectorMultiplier = 3;
                    extraFreeSpins += 5;
                }
                if (previousCollectorCount < 12 && collectorCount >= 12)
                {
                    collectorMultiplier = 10;
                    extraFreeSpins += 5;
                }

                if (result.FishPrizes.Count > 0)
                    result.CollectorWin = result.FishPrizes.Sum(x => x.Value) * collectorMultiplier;
            }

            if (!freeSpin && result.BonusTriggered)
            {
                freeSpinsRemaining = 10;
                bonusBet = checked((int)bet);
                collectorCount = 0;
                collectorMultiplier = 1;
            }
            else if (freeSpin && extraFreeSpins > 0)
            {
                freeSpinsRemaining += extraFreeSpins;
            }

            player.Credits += result.TotalWin;

            var spin = new Spin
            {
                UserId = userId,
                Bet = freeSpin ? 0 : bet,
                Win = result.TotalWin,
                Result = JsonSerializer.Serialize(new
                {
                    Game = "Reel Catch",
                    IsFreeSpin = freeSpin,
                    Result = result,
                    FreeSpinsRemaining = freeSpinsRemaining,
                    CollectorCount = collectorCount,
                    CollectorMultiplier = collectorMultiplier
                }),
                Created = DateTime.UtcNow
            };

            _context.Spins.Add(spin);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("ReelCatch.FreeSpins", freeSpinsRemaining);
            HttpContext.Session.SetInt32("ReelCatch.Collectors", collectorCount);
            HttpContext.Session.SetInt32("ReelCatch.Multiplier", collectorMultiplier);
            HttpContext.Session.SetInt32("ReelCatch.Bet", bonusBet);

            var response = new
            {
                board = result.Board,
                wins = result.Wins,
                fishPrizes = result.FishPrizes,
                lineWin = result.LineWin,
                collectorWin = result.CollectorWin,
                win = result.TotalWin,
                balance = player.Credits,
                scatterCount = result.ScatterCount,
                bonusTriggered = result.BonusTriggered,
                isFreeSpin = freeSpin,
                freeSpinsRemaining,
                extraFreeSpins,
                collectorCount,
                collectorMultiplier,
                fishermanLanded = result.FishermanLanded,
                fishermenLanded = result.FishermenLanded
            };

            if (freeSpin && freeSpinsRemaining == 0)
            {
                HttpContext.Session.Remove("ReelCatch.Collectors");
                HttpContext.Session.Remove("ReelCatch.Multiplier");
                HttpContext.Session.Remove("ReelCatch.Bet");
            }

            return Json(response);
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