namespace Casino.Models
{
    public class CascadeResult
    {
        public string[][] BoardBefore { get; set; } = [];

        public string[][] BoardAfter { get; set; } = [];

        public List<WaysWin> Wins { get; set; } = [];

        public long Win { get; set; }

        public List<int> PhoenixReels { get; set; } = new();

        public List<int> ExpiredPhoenixReels { get; set; } = new();
    }
}