namespace Casino.Models
{
    public class ReelCatchSpinResult
    {
        public string[][] Board { get; set; } = [];
        public List<WaysWin> Wins { get; set; } = [];
        public List<FishPrize> FishPrizes { get; set; } = [];
        public long LineWin { get; set; }
        public long CollectorWin { get; set; }
        public long TotalWin => LineWin + CollectorWin;
        public int ScatterCount { get; set; }
        public bool BonusTriggered { get; set; }
        public bool FishermanLanded { get; set; }
        public int FishermenLanded { get; set; }
    }

    public class FishPrize
    {
        public int Row { get; set; }
        public int Reel { get; set; }
        public long Value { get; set; }
    }
}
