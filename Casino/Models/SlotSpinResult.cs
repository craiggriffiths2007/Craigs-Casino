namespace Casino.Models
{
    public class SlotSpinResult
    {
        public string[][] InitialBoard { get; set; } = [];

        public List<CascadeResult> Cascades { get; set; } = [];

        public long TotalWin { get; set; }
    }
}