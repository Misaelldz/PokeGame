namespace PokeIdle.Core.Models
{
    public class MegaEvolutionData
    {
        public string Id { get; set; }
        public int BasePokemonId { get; set; }
        public int MegaPokemonId { get; set; }
        public string MegaName { get; set; }
        public string BaseName { get; set; }
        public string RequiredItem { get; set; }
        public string TypeOverride { get; set; }
    }
}
