namespace PokeIdle.Core.Models
{
    public class MoveData
    {
        public int MoveId { get; set; }
        public string NameEs { get; set; }
        public string NameEn { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public int? Power { get; set; }
        public int? Accuracy { get; set; }
        public int Pp { get; set; }
        public int Priority { get; set; }
        public string Ailment { get; set; }
        public int? AilmentChance { get; set; }
    }
}
