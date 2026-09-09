using Casino.Data;
using Casino.Models;
using Casino.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Casino.Controllers;

[ApiController]
[Authorize]
[Route("Game/PhoenixSlot2")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PhoenixCreditsController(
    ApplicationDbContext db,
    UserManager<IdentityUser> users,
    IAntiforgery antiforgery,
    SlotEngine engine) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("account")]
    public async Task<IActionResult> Account()
    {
        var userId = users.GetUserId(User);
        if (userId == null) return Unauthorized();

        // Also initialise new players who enter Phoenix Slot 2 before either old game.
        await using var transaction = await db.Database.BeginTransactionAsync();
        var player = await LockedPlayer(userId);
        if (player == null)
        {
            player = new PlayerAccount { UserId = userId, Credits = 10000, Created = DateTime.UtcNow };
            db.PlayerAccounts.Add(player);
            await db.SaveChangesAsync();
        }
        await transaction.CommitAsync();
        var token = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { userId, balance = player.Credits, token = token.RequestToken });
    }

    [HttpPost("spin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Spin([FromForm] long bet, [FromForm] Guid requestId,
        [FromForm] string accountId)
    {
        var userId = users.GetUserId(User);
        if (userId == null) return Unauthorized();
        if (accountId != userId)
            return Conflict(new { message = "The signed-in account changed. Reload the game." });
        if (requestId == Guid.Empty || bet is not (10 or 25 or 50 or 100))
            return BadRequest(new { message = "Choose a bet of 10, 25, 50 or 100 credits." });

        await using var transaction = await db.Database.BeginTransactionAsync();
        var player = await LockedPlayer(userId);
        if (player == null) return BadRequest(new { message = "Player account not found. Reload the game." });

        // The account lock serialises duplicate requests, including requests on other servers.
        // Store the receipt in the existing history JSON, so no schema migration is needed.
        string key = requestId.ToString("D");
        var previous = await db.Spins.FromSqlInterpolated($"""
            SELECT * FROM [Spins]
            WHERE [UserId] = {userId}
              AND JSON_VALUE(CASE WHEN ISJSON([Result]) = 1 THEN [Result] ELSE N'null' END, '$.game') = N'Phoenix Slot 2'
              AND JSON_VALUE(CASE WHEN ISJSON([Result]) = 1 THEN [Result] ELSE N'null' END, '$.requestId') = {key}
            """).AsNoTracking().FirstOrDefaultAsync();
        if (previous != null)
        {
            if (previous.Bet != bet)
                return Conflict(new { message = "This spin was already submitted with a different bet." });
            using var receipt = JsonDocument.Parse(previous.Result);
            var replay = receipt.RootElement.GetProperty("response").Deserialize<PhoenixCreditResult>(JsonOptions)!;
            // Replay the exact board and winnings, but show the latest shared wallet balance.
            replay.Balance = player.Credits;
            await transaction.CommitAsync();
            return Ok(replay);
        }

        if (player.Credits < bet)
            return BadRequest(new { message = "Not enough credits.", balance = player.Credits });

        var result = engine.Spin(bet);
        player.Credits = checked(player.Credits - bet + result.TotalWin);
        var response = PhoenixCreditResult.From(result, key, player.Credits);
        db.Spins.Add(new Spin
        {
            UserId = userId, Bet = bet, Win = result.TotalWin, Created = DateTime.UtcNow,
            Result = JsonSerializer.Serialize(new { game = "Phoenix Slot 2", requestId = key, response }, JsonOptions)
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(response);
    }

    private Task<PlayerAccount?> LockedPlayer(string userId) =>
        db.PlayerAccounts.FromSqlInterpolated($"SELECT * FROM [PlayerAccounts] WITH (UPDLOCK, HOLDLOCK) WHERE [UserId] = {userId}")
            .SingleOrDefaultAsync();
}

// Flat boards and ordinary arrays can be read by Unity JsonUtility on Web/IL2CPP.
public class PhoenixCreditResult
{
    public string RequestId { get; set; } = "";
    public string[] InitialBoard { get; set; } = [];
    public PhoenixCreditCascade[] Cascades { get; set; } = [];
    public long Win { get; set; }
    public long Balance { get; set; }

    public static PhoenixCreditResult From(SlotSpinResult result, string requestId, long balance) => new()
    {
        RequestId = requestId, Balance = balance, Win = result.TotalWin,
        InitialBoard = result.InitialBoard.SelectMany(row => row).ToArray(),
        Cascades = result.Cascades.Select(c => new PhoenixCreditCascade
        {
            BoardBefore = c.BoardBefore.SelectMany(row => row).ToArray(),
            BoardAfter = c.BoardAfter.SelectMany(row => row).ToArray(),
            Win = c.Win, Wins = c.Wins.ToArray(),
            PhoenixReels = c.PhoenixReels.ToArray(), ExpiredPhoenixReels = c.ExpiredPhoenixReels.ToArray()
        }).ToArray()
    };
}

public class PhoenixCreditCascade
{
    public string[] BoardBefore { get; set; } = [];
    public string[] BoardAfter { get; set; } = [];
    public WaysWin[] Wins { get; set; } = [];
    public long Win { get; set; }
    public int[] PhoenixReels { get; set; } = [];
    public int[] ExpiredPhoenixReels { get; set; } = [];
}
