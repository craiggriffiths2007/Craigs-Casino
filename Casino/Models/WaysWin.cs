namespace Casino.Models
{
    public class WaysWin
    {
        public string Symbol { get; set; } = string.Empty;

        public int ReelsMatched { get; set; }

        public int Ways { get; set; }

        public long Win { get; set; }

        public List<WinningPosition> Positions { get; set; } = [];
    }
}