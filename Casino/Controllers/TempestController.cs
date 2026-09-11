using Casino.Data;
using Casino.Models;
using Casino.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Casino.Controllers;

[Authorize]
public class TempestController(ApplicationDbContext context, UserManager<IdentityUser> users) : Controller
{
    public async Task<IActionResult> Index()
    {
        var id = users.GetUserId(User);
        if (id is null) return Challenge();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var player = await context.PlayerAccounts.FromSqlInterpolated($"SELECT * FROM [PlayerAccounts] WITH (UPDLOCK, HOLDLOCK) WHERE [UserId] = {id}").SingleOrDefaultAsync();
        if (player is null)
        {
            player = new PlayerAccount { UserId = id, Credits = 10000 };
            context.PlayerAccounts.Add(player);
            await context.SaveChangesAsync();
        }
        await transaction.CommitAsync();
        return View(player);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Spin(long bet)
    {
        var id = users.GetUserId(User);
        if (id is null) return Unauthorized();
        if (bet < 10 || bet > 1000 || bet % 10 != 0)
            return BadRequest(new { message = "Choose 10–1000 credits in steps of 10." });
        await using var transaction = await context.Database.BeginTransactionAsync();
        var player = await context.PlayerAccounts.FromSqlInterpolated($"SELECT * FROM [PlayerAccounts] WITH (UPDLOCK, HOLDLOCK) WHERE [UserId] = {id}").SingleOrDefaultAsync();
        if (player is null || player.Credits < bet)
            return BadRequest(new { message = "Not enough credits. Refresh to check your balance." });
        var result = new TempestEngine().Play(bet);
        player.Credits = checked(player.Credits - bet + result.TotalWin);
        context.Spins.Add(new Spin { UserId = id, Bet = bet, Win = result.TotalWin,
            Result = JsonSerializer.Serialize(new { Game = "Tempest Temple", Result = result }) });
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return Json(new { result, balance = player.Credits });
    }
}
