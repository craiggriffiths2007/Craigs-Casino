using Casino.Models;
using System.Security.Cryptography;

namespace Casino.Services
{
    public class SlotEngine
    {
        private const int Rows = 3;
        private const int Reels = 5;

        private readonly string[] _symbols =
        {
            "Cherry",
            "Lemon",
            "Plum",
            "Bell",
            "Seven",
            "Diamond"
        };

        public SlotSpinResult Spin(long bet)
        {
            var result = new SlotSpinResult();

            var board = CreateBoard();

            result.InitialBoard = CloneBoard(board);

            while (true)
            {
                var wins = FindWaysWins(board, bet);

                if (wins.Count == 0)
                    break;

                var cascade = new CascadeResult
                {
                    BoardBefore = CloneBoard(board),
                    Wins = wins,
                    Win = wins.Sum(w => w.Win)
                };

                RemoveWinningSymbols(board, wins);

                DropSymbols(board);

                FillEmptySpaces(board);

                cascade.BoardAfter = CloneBoard(board);

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

        private string GetRandomSymbol()
        {
            int index =
                RandomNumberGenerator.GetInt32(_symbols.Length);

            return _symbols[index];
        }

        private List<WaysWin> FindWaysWins(
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