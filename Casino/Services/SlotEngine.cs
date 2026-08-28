using Casino.Models;
using System.Security.Cryptography;

namespace Casino.Services
{
    public class SlotEngine
    {
        private const int Rows = 3;
        private const int Reels = 5;
        private bool _useAllWays = false;

        private readonly string[] _symbols =
        {
            "Cherry",
            "Lemon",
            "Plum",
            "Bell",
            "Seven",
            "Diamond",

            "BlueOrb",
            "GreenOrb",
            "YellowOrb",
            "RedOrb"
        };

        private static readonly (string Symbol, int Weight)[] _symbolWeights =
        {
            ("Cherry",  18),
            ("Lemon",   18),
            ("Plum",    16),
            ("Bell",    14),
            ("Seven",   12),
            ("Diamond", 10),

            // Special coloured symbols
            ("BlueOrb",   6),
            ("GreenOrb",  5),
            ("YellowOrb", 4),

            // Rare wild
            ("RedOrb",    5)
        };

        private readonly int[][] _paylines =
        {
            // 0 = top, 1 = middle, 2 = bottom

            new[] { 1, 1, 1, 1, 1 },
            new[] { 0, 0, 0, 0, 0 },
            new[] { 2, 2, 2, 2, 2 },

            new[] { 0, 1, 2, 1, 0 },
            new[] { 2, 1, 0, 1, 2 },

            new[] { 0, 0, 1, 2, 2 },
            new[] { 2, 2, 1, 0, 0 },

            new[] { 1, 0, 0, 0, 1 },
            new[] { 1, 2, 2, 2, 1 },

            new[] { 0, 1, 1, 1, 0 },
            new[] { 2, 1, 1, 1, 2 },

            new[] { 1, 0, 1, 2, 1 },
            new[] { 1, 2, 1, 0, 1 },

            new[] { 0, 1, 0, 1, 0 },
            new[] { 2, 1, 2, 1, 2 },

            new[] { 0, 0, 2, 0, 0 },
            new[] { 2, 2, 0, 2, 2 },

            new[] { 1, 1, 0, 1, 1 },
            new[] { 1, 1, 2, 1, 1 },

            new[] { 0, 2, 1, 2, 0 },
            new[] { 2, 0, 1, 0, 2 },

            new[] { 0, 2, 2, 2, 0 },
            new[] { 2, 0, 0, 0, 2 },

            new[] { 0, 2, 1, 0, 2 },
            new[] { 2, 0, 1, 2, 0 }
        };


        public SlotSpinResult Spin(long bet)
        {
            var result = new SlotSpinResult();

            var board = CreateBoard();

            result.InitialBoard = CloneBoard(board);

            // A Phoenix only survives for one win evaluation.
            var activePhoenixReels =
                        new HashSet<int>();

            while (true)
            {
                var wins = _useAllWays
                    ? FindWaysWinsAll(board, bet)
                    : FindPaylineWins(board, bet);

                // Phoenix appeared but didn't create another win.
                // Spin simply ends with the Phoenix still displayed.
                if (wins.Count == 0)
                    break;

                var cascade = new CascadeResult
                {
                    BoardBefore = CloneBoard(board),
                    Wins = wins,
                    Win = wins.Sum(w => w.Win)
                };

                // Work out whether THIS win has removed
                // all three positions from a reel.
                var newPhoenixReels =
                        FindCompletelyRemovedReels(wins)
                            .Where(reel =>
                                !activePhoenixReels.Contains(reel))
                            .ToList();

                // Remove all winning symbols.
                RemoveWinningSymbols(board, wins);

                // Existing Phoenixes expire after this win
                cascade.ExpiredPhoenixReels =
                    activePhoenixReels.ToList();
                // If there was already a Phoenix on this board,
                // it expires after this cascade.
                foreach (int reel in activePhoenixReels)
                {
                    for (int row = 0; row < Rows; row++)
                    {
                        board[row][reel] = string.Empty;
                    }
                }

                activePhoenixReels.Clear();

                // Drop remaining normal symbols.
                DropSymbols(board);

                // Fill all normal empty spaces.
                FillEmptySpaces(board);

                // If THIS win cleared a complete reel,
                // replace that reel with a Phoenix.
                foreach (int reel in newPhoenixReels)
                {
                    board[0][reel] = "Phoenix";
                    board[1][reel] = "Phoenix";
                    board[2][reel] = "Phoenix";

                    activePhoenixReels.Add(reel);
                    cascade.PhoenixReels.Add(reel);
                }

                cascade.BoardAfter =
                    CloneBoard(board);

                result.Cascades.Add(cascade);

                result.TotalWin += cascade.Win;
            }

            return result;
        }

        private string[][] CreateBoard()
        {
            var board = new string[Rows][];

            for (int row = 0; row < Rows; row++)
            {
                board[row] = new string[Reels];

                for (int reel = 0; reel < Reels; reel++)
                {
                    board[row][reel] = GetRandomSymbol();
                }
            }

            return board;
        }

        private static List<int> FindCompletelyRemovedReels(
    List<WaysWin> wins)
        {
            var removedPositions = wins
                .SelectMany(w => w.Positions)
                .DistinctBy(p => new
                {
                    p.Reel,
                    p.Row
                })
                .ToList();

            var reels = new List<int>();

            for (int reel = 0; reel < Reels; reel++)
            {
                bool allThreeRemoved =
                    Enumerable.Range(0, Rows)
                        .All(row =>
                            removedPositions.Any(p =>
                                p.Reel == reel &&
                                p.Row == row));

                if (allThreeRemoved)
                    reels.Add(reel);
            }

            return reels;
        }

        private static bool IsWild(string symbol)
        {
            return symbol == "RedOrb" ||
                   symbol == "Phoenix";
        }

        private static bool IsOrb(string symbol)
        {
            return symbol is
                "BlueOrb" or
                "GreenOrb" or
                "YellowOrb";
        }

        private static bool SymbolsMatch(
            string targetSymbol,
            string actualSymbol)
        {
            return actualSymbol == targetSymbol ||
                   IsWild(actualSymbol);
        }

        private string GetRandomSymbol()
        {
            int totalWeight =
                _symbolWeights.Sum(x => x.Weight);

            int roll =
                RandomNumberGenerator.GetInt32(totalWeight);

            int runningTotal = 0;

            foreach (var item in _symbolWeights)
            {
                runningTotal += item.Weight;

                if (roll < runningTotal)
                    return item.Symbol;
            }

            // Should never get here
            return _symbolWeights[0].Symbol;
        }



        private List<WaysWin> FindPaylineWins(
                                    string[][] board,
                                    long bet)
        {
            var wins = new List<WaysWin>();

            for (int paylineIndex = 0;
                 paylineIndex < _paylines.Length;
                 paylineIndex++)
            {
                var payline = _paylines[paylineIndex];

                // Find the first non-wild symbol on this payline.
                string? symbol = null;

                for (int reel = 0; reel < Reels; reel++)
                {
                    string current =
                        board[payline[reel]][reel];

                    if (!IsWild(current))
                    {
                        symbol = current;
                        break;
                    }
                }

                // Entire line is wild - ignore for now.
                if (symbol == null)
                    continue;

                int reelsMatched = 0;

                for (int reel = 0; reel < Reels; reel++)
                {
                    int row = payline[reel];

                    string actualSymbol =
                        board[row][reel];

                    if (SymbolsMatch(
                        symbol,
                        actualSymbol))
                    {
                        reelsMatched++;
                    }
                    else
                    {
                        break;
                    }
                }

                int minimumMatch =
                    IsOrb(symbol)
                        ? 2
                        : 3;

                if (reelsMatched < minimumMatch)
                    continue;

                long multiplier =
                    GetMultiplier(
                        symbol,
                        reelsMatched);

                if (multiplier <= 0)
                    continue;

                var positions =
                    new List<WinningPosition>();

                for (int reel = 0;
                     reel < reelsMatched;
                     reel++)
                {
                    positions.Add(
                        new WinningPosition
                        {
                            Reel = reel,
                            Row = payline[reel]
                        });
                }

                wins.Add(
                    new WaysWin
                    {
                        Symbol = symbol,
                        ReelsMatched = reelsMatched,
                        Ways = 1,
                        Win = bet * multiplier,
                        PaylineIndex = paylineIndex,
                        Positions = positions
                    });
            }

            return wins;
        }

        private List<WaysWin> FindWaysWinsAll(
            string[][] board,
            long bet)
        {
            var wins = new List<WaysWin>();

            foreach (var symbol in _symbols)
            {
                var positionsPerReel =
                    new List<List<WinningPosition>>();

                for (int reel = 0; reel < Reels; reel++)
                {
                    var positions =
                        new List<WinningPosition>();

                    for (int row = 0; row < Rows; row++)
                    {
                        if (board[row][reel] == symbol)
                        {
                            positions.Add(
                                new WinningPosition
                                {
                                    Row = row,
                                    Reel = reel
                                });
                        }
                    }

                    positionsPerReel.Add(positions);
                }

                // A ways win must start on reel 1
                // and continue consecutively to the right.

                int reelsMatched = 0;

                for (int reel = 0; reel < Reels; reel++)
                {
                    if (positionsPerReel[reel].Count == 0)
                        break;

                    reelsMatched++;
                }

                if (reelsMatched < 3)
                    continue;

                int ways = 1;

                var winningPositions =
                    new List<WinningPosition>();

                for (int reel = 0; reel < reelsMatched; reel++)
                {
                    ways *= positionsPerReel[reel].Count;

                    winningPositions.AddRange(
                        positionsPerReel[reel]);
                }

                long multiplier =
                    GetMultiplier(
                        symbol,
                        reelsMatched);

                wins.Add(new WaysWin
                {
                    Symbol = symbol,
                    ReelsMatched = reelsMatched,
                    Ways = ways,
                    Win = bet * multiplier * ways,
                    Positions = winningPositions
                });
            }

            return wins;
        }

        private long GetMultiplier(
            string symbol,
            int reelsMatched)
        {
            return symbol switch
            {
                "Cherry" => reelsMatched switch
                {
                    3 => 1,
                    4 => 2,
                    5 => 5,
                    _ => 0
                },

                "Lemon" => reelsMatched switch
                {
                    3 => 1,
                    4 => 3,
                    5 => 6,
                    _ => 0
                },

                "Plum" => reelsMatched switch
                {
                    3 => 2,
                    4 => 4,
                    5 => 8,
                    _ => 0
                },

                "Bell" => reelsMatched switch
                {
                    3 => 2,
                    4 => 5,
                    5 => 10,
                    _ => 0
                },

                "Seven" => reelsMatched switch
                {
                    3 => 3,
                    4 => 8,
                    5 => 15,
                    _ => 0
                },

                "Diamond" => reelsMatched switch
                {
                    3 => 5,
                    4 => 12,
                    5 => 25,
                    _ => 0
                },

                "BlueOrb" => reelsMatched switch
                {
                    2 => 1,
                    3 => 3,
                    4 => 8,
                    5 => 20,
                    _ => 0
                },

                "GreenOrb" => reelsMatched switch
                {
                    2 => 2,
                    3 => 5,
                    4 => 12,
                    5 => 30,
                    _ => 0
                },

                "YellowOrb" => reelsMatched switch
                {
                    2 => 3,
                    3 => 8,
                    4 => 20,
                    5 => 50,
                    _ => 0
                },

                _ => 0
            };
        }

        private void RemoveWinningSymbols(
            string[][] board,
            List<WaysWin> wins)
        {
            var positions = wins
                .SelectMany(w => w.Positions)
                .DistinctBy(p => new { p.Row, p.Reel });

            foreach (var position in positions)
            {
                board[position.Row][position.Reel] =
                    string.Empty;
            }
        }

        private void DropSymbols(
            string[][] board)
        {
            for (int reel = 0; reel < Reels; reel++)
            {
                var remaining =
                    new List<string>();

                for (int row = Rows - 1; row >= 0; row--)
                {
                    if (!string.IsNullOrEmpty(
                        board[row][reel]))
                    {
                        remaining.Add(
                            board[row][reel]);
                    }
                }

                int targetRow = Rows - 1;

                foreach (var symbol in remaining)
                {
                    board[targetRow][reel] = symbol;
                    targetRow--;
                }

                while (targetRow >= 0)
                {
                    board[targetRow][reel] =
                        string.Empty;

                    targetRow--;
                }
            }
        }

        private void FillEmptySpaces(
            string[][] board)
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int reel = 0; reel < Reels; reel++)
                {
                    if (string.IsNullOrEmpty(
                        board[row][reel]))
                    {
                        board[row][reel] =
                            GetRandomSymbol();
                    }
                }
            }
        }

        private static string[][] CloneBoard(
            string[][] board)
        {
            return board
                .Select(row => row.ToArray())
                .ToArray();
        }
    }
}