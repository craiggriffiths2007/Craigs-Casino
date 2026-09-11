using System.Security.Cryptography;

namespace Casino.Services;

public record TempestFrame(int[] Board, int[] Winning, long Win);
public record TempestTurn(bool FreeSpin, List<TempestFrame> Frames, int Multiplier, long Win, int Awarded);
public record TempestRound(List<TempestTurn> Turns, long TotalWin, bool Capped);

public class TempestEngine
{
    // Nine regular symbols, scatter (9), and multiplier (10).
    private static int Draw()
    {
        var n = RandomNumberGenerator.GetInt32(1000);
        return n < 20 ? 9 : n < 35 ? 10 : (n - 35) * 9 / 965;
    }

    public TempestRound Play(long bet)
    {
        if (bet < 10 || bet > 1000 || bet % 10 != 0) throw new ArgumentOutOfRangeException(nameof(bet));
        var turns = new List<TempestTurn>();
        long total = 0;
        int remaining = 0, bank = 0;
        const int maxTurns = 101;
        var cap = bet * 5000;
        do
        {
            bool free = turns.Count > 0;
            if (free) remaining--;
            var board = Enumerable.Range(0, 30).Select(_ => Draw()).ToArray();
            var frames = new List<TempestFrame>();
            long baseWin = 0;
            int lightning = 0;
            // Multipliers are collected once per turn, on the final board.
            for (int tumble = 0; tumble < 50; tumble++)
            {
                var winning = new List<int>();
                long win = 0;
                for (int symbol = 0; symbol < 9; symbol++)
                {
                    var positions = Enumerable.Range(0, 30).Where(i => board[i] == symbol).ToArray();
                    if (positions.Length < 8) continue;
                    winning.AddRange(positions);
                    win += bet * (symbol + 2) * (positions.Length >= 12 ? 5 : positions.Length >= 10 ? 2 : 1) / 10;
                }
                frames.Add(new((int[])board.Clone(), winning.ToArray(), win));
                baseWin += win;
                if (win == 0 || tumble == 49) break;
                for (int col = 0; col < 6; col++)
                {
                    var kept = Enumerable.Range(0, 5).Select(row => row * 6 + col)
                        .Where(i => !winning.Contains(i)).Select(i => board[i]).ToList();
                    while (kept.Count < 5) kept.Insert(0, Draw());
                    for (int row = 0; row < 5; row++) board[row * 6 + col] = kept[row];
                }
            }
            foreach (var _ in board.Where(s => s == 10))
            {
                int roll = RandomNumberGenerator.GetInt32(100);
                lightning += roll < 65 ? 2 : roll < 90 ? 5 : roll < 98 ? 10 : 50;
            }
            if (free && baseWin > 0) bank = Math.Min(500, bank + lightning);
            int multiplier = Math.Max(1, free ? bank : lightning);
            int scatters = board.Count(s => s == 9);
            int awarded = free ? (scatters >= 3 ? 5 : 0) : (scatters >= 4 ? 15 : 0);
            awarded = Math.Min(awarded, maxTurns - turns.Count - 1 - remaining);
            remaining += awarded;
            long scatterWin = !free && scatters >= 4 ? bet * (scatters >= 6 ? 100 : scatters == 5 ? 5 : 3) : 0;
            long payout = Math.Min(cap - total, baseWin * multiplier + scatterWin);
            total += payout;
            turns.Add(new(free, frames, multiplier, payout, awarded));
        } while (remaining > 0 && total < cap && turns.Count < maxTurns);
        return new(turns, total, total == cap);
    }
}
