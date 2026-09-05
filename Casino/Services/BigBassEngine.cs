using Casino.Models;
using System.Security.Cryptography;
using System.Text.Json;

namespace Casino.Services
{
    // A simplified Big Bass Bonanza style slot engine.
    public class BigBassEngine
    {
        private const int Rows = 3;
        private const int Reels = 5;

        private readonly string[] _symbols =
        {
            "Fish",
            "Boot",
            "Hat",
            "TackleBox",
            "Coin",
            "Fisherman" // acts as a special symbol (wild/scatter)
        };

        private static readonly (string Symbol, int Weight)[] _symbolWeights =
        {
            ("Fish",  20),
            ("Boot",   16),
            ("Hat",    14),
            ("TackleBox",    10),
            ("Coin",    8),
            ("Fisherman", 6)
        };

        public SlotSpinResult Spin(long bet)
        {
            var result = new SlotSpinResult();

            var board = CreateBoard();

            result.InitialBoard = CloneBoard(board);

            while (true)
            {
                var wins = FindPaylineWins(board, bet);

                if (wins.Count == 0)
                    break;

                var cascade = new CascadeResult
                {
                    BoardBefore = CloneBoard(board),
                    Wins = wins,
                    Win = wins.Sum(w => w.Win)
                };

                RemoveWinningSymbols(board, wins);

                cascade.BoardAfter = CloneBoard(board);

                // Drop and refill
                DropSymbols(board);
                FillEmptySpaces(board);

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

        private List<WaysWin> FindPaylineWins(string[][] board, long bet)
        {
            // For simplicity reuse a small set of paylines (horizontal only)
            var paylines = new[]
            {
                new[] {1,1,1,1,1},
                new[] {0,0,0,0,0},
                new[] {2,2,2,2,2}
            };

            var wins = new List<WaysWin>();

            foreach (var line in paylines)
            {
                // determine matching symbol starting from left
                string symbol = board[line[0]][0];

                int matched = 1;

                for (int r = 1; r < Reels; r++)
                {
                    var s = board[line[r]][r];

                    if (SymbolsMatch(symbol, s))
                    {
                        matched++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (matched >= 3)
                {
                    var positions = new List<WinningPosition>();

                    for (int r = 0; r < matched; r++)
                    {
                        positions.Add(new WinningPosition { Row = line[r], Reel = r });
                    }

                    var multiplier = GetMultiplier(symbol, matched);

                    wins.Add(new WaysWin
                    {
                        Symbol = symbol,
                        ReelsMatched = matched,
                        Ways = 1,
                        Win = bet * multiplier,
                        Positions = positions
                    });
                }
            }

            return wins;
        }

        private long GetMultiplier(string symbol, int matched)
        {
            return symbol switch
            {
                "Fish" => matched switch { 3 => 2, 4 => 6, 5 => 12, _ => 0 },
                "Boot" => matched switch { 3 => 1, 4 => 3, 5 => 6, _ => 0 },
                "Hat" => matched switch { 3 => 1, 4 => 4, 5 => 8, _ => 0 },
                "TackleBox" => matched switch { 3 => 3, 4 => 8, 5 => 15, _ => 0 },
                "Coin" => matched switch { 3 => 2, 4 => 5, 5 => 10, _ => 0 },
                "Fisherman" => matched switch { 3 => 5, 4 => 12, 5 => 25, _ => 0 },
                _ => 0
            };
        }

        private static bool SymbolsMatch(string target, string actual)
        {
            // Fisherman acts as a wild and matches anything
            return actual == target || actual == "Fisherman" || target == "Fisherman";
        }

        private string GetRandomSymbol()
        {
            int totalWeight = _symbolWeights.Sum(x => x.Weight);

            int roll = RandomNumberGenerator.GetInt32(totalWeight);

            int running = 0;

            foreach (var w in _symbolWeights)
            {
                running += w.Weight;

                if (roll < running)
                    return w.Symbol;
            }

            return _symbolWeights.Last().Symbol;
        }

        private void RemoveWinningSymbols(string[][] board, List<WaysWin> wins)
        {
            var positions = wins.SelectMany(w => w.Positions).DistinctBy(p => new { p.Row, p.Reel });

            foreach (var position in positions)
            {
                board[position.Row][position.Reel] = string.Empty;
            }
        }

        private void DropSymbols(string[][] board)
        {
            for (int reel = 0; reel < Reels; reel++)
            {
                var remaining = new List<string>();

                for (int row = Rows - 1; row >= 0; row--)
                {
                    if (!string.IsNullOrEmpty(board[row][reel]))
                        remaining.Add(board[row][reel]);
                }

                int targetRow = Rows - 1;

                foreach (var symbol in remaining)
                {
                    board[targetRow][reel] = symbol;
                    targetRow--;
                }

                while (targetRow >= 0)
                {
                    board[targetRow][reel] = string.Empty;
                    targetRow--;
                }
            }
        }

        private void FillEmptySpaces(string[][] board)
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int reel = 0; reel < Reels; reel++)
                {
                    if (string.IsNullOrEmpty(board[row][reel]))
                        board[row][reel] = GetRandomSymbol();
                }
            }
        }

        private static string[][] CloneBoard(string[][] board)
        {
            return board.Select(row => row.ToArray()).ToArray();
        }
    }
}
