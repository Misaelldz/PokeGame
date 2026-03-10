namespace PokeIdle.Core.Models
{
    public class SpeciesData
    {
        public int PokemonId { get; set; }
        public string Name { get; set; }
        public bool IsLegendary { get; set; }
        public bool IsMythical { get; set; }
        public int? EvolvesFromId { get; set; }
        public string EvolutionChainUrl { get; set; }
        public string GrowthRate { get; set; }
        public int GachaTier { get; set; }
    }
}
