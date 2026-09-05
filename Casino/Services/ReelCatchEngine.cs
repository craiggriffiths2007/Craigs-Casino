using Casino.Models;
using System.Security.Cryptography;

namespace Casino.Services
{
    /// <summary>
    /// Original 5x3 fishing-themed slot. The server generates every result.
    /// Bet is the total stake for the spin, shared across ten paylines.
    /// </summary>
    public class ReelCatchEngine
    {
        private const int Rows = 3;
        private const int Reels = 5;
        private const int Paylines = 10;

        private static readonly int[][] Lines =
        [
            [0,0,0,0,0],
            [1,1,1,1,1],
            [2,2,2,2,2],
            [0,1,2,1,0],
            [2,1,0,1,2],
            [0,0,1,2,2],
            [2,2,1,0,0],
            [1,0,0,0,1],
            [1,2,2,2,1],
            [0,1,1,1,0]
        ];

        private static readonly (string Symbol, int Weight)[] BaseWeights =
        [
            ("J", 18), ("Q", 17), ("K", 16), ("A", 15),
            ("Tackle", 11), ("Boat", 9), ("Fish", 10),
            ("Fisherman", 3), ("Scatter", 4)
        ];

        private static readonly (string Symbol, int Weight)[] BonusWeights =
        [
            ("J", 16), ("Q", 15), ("K", 14), ("A", 13),
            ("Tackle", 10), ("Boat", 8), ("Fish", 15),
            ("Fisherman", 7), ("Scatter", 2)
        ];

        private static readonly int[] FishValueMultipliers = [1, 1, 2, 2, 3, 5, 5, 10, 20, 50];

        public ReelCatchSpinResult Spin(long bet, bool bonusSpin, int collectorMultiplier)
        {
            var board = CreateBoard(bonusSpin);
            var result = new ReelCatchSpinResult
            {
                Board = board,
                ScatterCount = CountSymbol(board, "Scatter")
            };

            result.Wins = FindPaylineWins(board, bet);
            result.LineWin = result.Wins.Sum(x => x.Win);
            result.BonusTriggered = !bonusSpin && result.ScatterCount >= 3;

            if (!bonusSpin)
                return result;

            result.FishPrizes = CreateFishPrizes(board, bet);
            result.FishermenLanded = CountSymbol(board, "Fisherman");
            result.FishermanLanded = result.FishermenLanded > 0;

            if (result.FishermanLanded && result.FishPrizes.Count > 0)
            {
                result.CollectorWin = result.FishPrizes.Sum(x => x.Value) * Math.Max(1, collectorMultiplier);
            }

            return result;
        }

        private static string[][] CreateBoard(bool bonusSpin)
        {
            var board = new string[Rows][];
            var weights = bonusSpin ? BonusWeights : BaseWeights;

            for (var row = 0; row < Rows; row++)
            {
                board[row] = new string[Reels];
                for (var reel = 0; reel < Reels; reel++)
                    board[row][reel] = RandomSymbol(weights);
            }

            return board;
        }

        private static string RandomSymbol((string Symbol, int Weight)[] weights)
        {
            var total = weights.Sum(x => x.Weight);
            var roll = RandomNumberGenerator.GetInt32(total);
            var running = 0;

            foreach (var item in weights)
            {
                running += item.Weight;
                if (roll < running)
                    return item.Symbol;
            }

            return weights[^1].Symbol;
        }

        private static List<WaysWin> FindPaylineWins(string[][] board, long totalBet)
        {
            var wins = new List<WaysWin>();
            var lineBet = Math.Max(1, totalBet / Paylines);

            for (var lineIndex = 0; lineIndex < Lines.Length; lineIndex++)
            {
                var line = Lines[lineIndex];
                var target = FindTargetSymbol(board, line);

                if (target == null || target == "Scatter")
                    continue;

                var matched = 0;
                var positions = new List<WinningPosition>();

                for (var reel = 0; reel < Reels; reel++)
                {
                    var symbol = board[line[reel]][reel];
                    if (symbol == target || symbol == "Fisherman")
                    {
                        matched++;
                        positions.Add(new WinningPosition { Row = line[reel], Reel = reel });
                    }
                    else
                    {
                        break;
                    }
                }

                if (matched < 3)
                    continue;

                var multiplier = GetPayMultiplier(target, matched);
                if (multiplier <= 0)
                    continue;

                wins.Add(new WaysWin
                {
                    Symbol = target,
                    ReelsMatched = matched,
                    Ways = 1,
                    PaylineIndex = lineIndex,
                    Win = lineBet * multiplier,
                    Positions = positions
                });
            }

            return wins;
        }

        private static string? FindTargetSymbol(string[][] board, int[] line)
        {
            for (var reel = 0; reel < Reels; reel++)
            {
                var symbol = board[line[reel]][reel];
                if (symbol != "Fisherman")
                    return symbol;
            }

            return "Fisherman";
        }

        private static int GetPayMultiplier(string symbol, int count) => symbol switch
        {
            "J" => count switch { 3 => 2, 4 => 4, 5 => 8, _ => 0 },
            "Q" => count switch { 3 => 2, 4 => 5, 5 => 10, _ => 0 },
            "K" => count switch { 3 => 3, 4 => 6, 5 => 12, _ => 0 },
            "A" => count switch { 3 => 3, 4 => 7, 5 => 15, _ => 0 },
            "Tackle" => count switch { 3 => 5, 4 => 12, 5 => 25, _ => 0 },
            "Boat" => count switch { 3 => 7, 4 => 18, 5 => 40, _ => 0 },
            "Fish" => count switch { 3 => 4, 4 => 10, 5 => 22, _ => 0 },
            "Fisherman" => count switch { 3 => 10, 4 => 25, 5 => 60, _ => 0 },
            _ => 0
        };

        private static List<FishPrize> CreateFishPrizes(string[][] board, long bet)
        {
            var values = new List<FishPrize>();

            for (var row = 0; row < Rows; row++)
            {
                for (var reel = 0; reel < Reels; reel++)
                {
                    if (board[row][reel] != "Fish")
                        continue;

                    var multiple = FishValueMultipliers[RandomNumberGenerator.GetInt32(FishValueMultipliers.Length)];
                    values.Add(new FishPrize
                    {
                        Row = row,
                        Reel = reel,
                        Value = bet * multiple
                    });
                }
            }

            return values;
        }

        private static int CountSymbol(string[][] board, string target)
            => board.Sum(row => row.Count(symbol => symbol == target));
    }
}
