namespace PokeIdle.Core.Models
{
    public class EvolutionData
    {
        public int FromPokemonId { get; set; }
        public int ToPokemonId { get; set; }
        public string Trigger { get; set; }
        public int? MinLevel { get; set; }
        public string ItemId { get; set; }
    }
}
