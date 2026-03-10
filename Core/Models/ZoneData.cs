using System.Collections.Generic;

namespace PokeIdle.Core.Models
{
    public class ZoneData
    {
        public string Id { get; set; }
        public string Region { get; set; }
        public int ZoneIndex { get; set; }
        public string Name { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public List<string> WildPokemon { get; set; } = new List<string>();
        public bool IsGym { get; set; }
        public string GymBadgeId { get; set; }
        public string GymId { get; set; }
        public string BattleBgId { get; set; }
        public float EncounterRate { get; set; }
        public int TrainerCount { get; set; }
        public int ReferenceBst { get; set; }
        public List<string> ItemDrops { get; set; } = new List<string>();
        public string Description { get; set; }
    }
}
