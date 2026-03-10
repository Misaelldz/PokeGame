using System.Collections.Generic;

namespace PokeIdle.Core.Models
{
    public class GymData
    {
        public string Id { get; set; }
        public string Region { get; set; }
        public string LeaderName { get; set; }
        public string BadgeName { get; set; }
        public string Type { get; set; }
        public int UnlockLevel { get; set; }
        public int ReferenceBst { get; set; }
        public string Mechanic { get; set; }
        public List<string> Pokemon { get; set; } = new List<string>();
        public List<string> RewardItems { get; set; } = new List<string>();
        public string DialogIntro { get; set; }
        public string DialogVictory { get; set; }
        public string DialogDefeat { get; set; }
    }
}
