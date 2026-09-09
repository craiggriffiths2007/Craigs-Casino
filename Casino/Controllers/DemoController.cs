using Casino.Models;
using Casino.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Casino.Controllers;

[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class DemoController(SlotEngine slotEngine, ReelCatchEngine reelCatchEngine,
    IAntiforgery antiforgery, IWebHostEnvironment environment) : Controller
{
    private readonly ReelCatchEngine _reelCatchEngine = reelCatchEngine;
    private long Balance => long.TryParse(HttpContext.Session.GetString("Demo.Balance"), out var value) ? value : 10000;
    private void SaveBalance(long value) => HttpContext.Session.SetString("Demo.Balance", value.ToString());
    private PlayerAccount Player() => new() { Credits = Balance };

    public IActionResult Index() => DemoView("Index");
    public IActionResult ReelCatch()
    {
        ViewBag.FreeSpinsRemaining = HttpContext.Session.GetInt32("Demo.ReelCatch.FreeSpins") ?? 0;
        ViewBag.CollectorCount = HttpContext.Session.GetInt32("Demo.ReelCatch.Collectors") ?? 0;
        ViewBag.CollectorMultiplier = HttpContext.Session.GetInt32("Demo.ReelCatch.Multiplier") ?? 1;
        ViewBag.BonusBet = HttpContext.Session.GetInt32("Demo.ReelCatch.Bet") ?? 0;
        return DemoView("ReelCatch");
    }
    public IActionResult PhoenixSlot2() => DemoView("PhoenixSlot2");
    private IActionResult DemoView(string name)
    {
        ViewBag.IsDemo = true;
        ViewBag.DemoBalance = Balance;
        return View("~/Views/Game/" + name + ".cshtml", Player());
    }

    // Keep the player's URL under /Demo so the existing Unity relative API URLs
    // resolve to practice endpoints. Assets still come from the shared build.
    [HttpGet("Demo/games/phoenix-slot-2/index.html")]
    public IActionResult UnityPlayer()
    {
        var html = System.IO.File.ReadAllText(Path.Combine(environment.WebRootPath, "Games", "phoenix-slot-2", "index.html"));
        html = html.Replace("<head>", "<head><base href=\"" + Url.Content("~/games/phoenix-slot-2/") + "\">");
        return Content(html, "text/html");
    }

    [HttpGet("Demo/Game/PhoenixSlot2/account")]
    public IActionResult Account()
    {
        var id = HttpContext.Session.GetString("Demo.Id");
        if (id == null) { id = "demo-" + Guid.NewGuid(); HttpContext.Session.SetString("Demo.Id", id); }
        return Json(new { userId = id, balance = Balance, token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }

    [HttpPost("Demo/Game/PhoenixSlot2/spin")]
    [ValidateAntiForgeryToken]
    public IActionResult UnitySpin(long bet, Guid requestId, string accountId)
    {
        if (accountId != HttpContext.Session.GetString("Demo.Id") || requestId == Guid.Empty)
            return Conflict(new { message = "Demo session changed. Reload the demo." });
        if (bet is not (10 or 25 or 50 or 100)) return BadRequest(new { message = "Invalid bet." });
        var key = "Demo.Receipt." + requestId.ToString("D");
        var saved = HttpContext.Session.GetString(key);
        if (saved != null) return Json(JsonSerializer.Deserialize<PhoenixCreditResult>(saved));
        if (Balance < bet) return BadRequest(new { message = "Not enough demo credits.", balance = Balance });
        var result = slotEngine.Spin(bet);
        SaveBalance(checked(Balance - bet + result.TotalWin));
        var reply = PhoenixCreditResult.From(result, requestId.ToString("D"), Balance);
        var previous = HttpContext.Session.GetString("Demo.LastReceipt");
        if (previous != null) HttpContext.Session.Remove(previous);
        HttpContext.Session.SetString(key, JsonSerializer.Serialize(reply));
        HttpContext.Session.SetString("Demo.LastReceipt", key);
        return Json(reply);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Spin(long bet)
    {
        if (bet is not (10 or 25 or 50 or 100)) return BadRequest(new { message = "Invalid bet." });
        if (Balance < bet) return BadRequest(new { message = "Not enough demo credits." });
        var result = slotEngine.Spin(bet);
        SaveBalance(checked(Balance - bet + result.TotalWin));
        return Json(new { initialBoard = result.InitialBoard, cascades = result.Cascades, win = result.TotalWin, balance = Balance });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ReelCatchSpin(long bet, bool freeSpin = false)
    {
        var player = Player();
            var freeSpinsRemaining = HttpContext.Session.GetInt32("Demo.ReelCatch.FreeSpins") ?? 0;
            var collectorCount = HttpContext.Session.GetInt32("Demo.ReelCatch.Collectors") ?? 0;
            var collectorMultiplier = HttpContext.Session.GetInt32("Demo.ReelCatch.Multiplier") ?? 1;
            var bonusBet = HttpContext.Session.GetInt32("Demo.ReelCatch.Bet") ?? 0;

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

            SaveBalance(player.Credits);

            HttpContext.Session.SetInt32("Demo.ReelCatch.FreeSpins", freeSpinsRemaining);
            HttpContext.Session.SetInt32("Demo.ReelCatch.Collectors", collectorCount);
            HttpContext.Session.SetInt32("Demo.ReelCatch.Multiplier", collectorMultiplier);
            HttpContext.Session.SetInt32("Demo.ReelCatch.Bet", bonusBet);

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
                HttpContext.Session.Remove("Demo.ReelCatch.Collectors");
                HttpContext.Session.Remove("Demo.ReelCatch.Multiplier");
                HttpContext.Session.Remove("Demo.ReelCatch.Bet");
            }

            return Json(response);
        }

}
