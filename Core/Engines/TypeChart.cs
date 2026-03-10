using System.Collections.Generic;

namespace PokeIdle.Core.Engines
{
    public static class TypeChart
    {
        // Simple matrix dictionary: TypeAttack vs TypeDefense
        // Missing keys imply neutral (1.0f) interactions.
        private static readonly Dictionary<string, Dictionary<string, float>> chart = new Dictionary<string, Dictionary<string, float>>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Normal", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Rock", 0.5f }, { "Ghost", 0f }, { "Steel", 0.5f }
            }},
            { "Fire", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Fire", 0.5f }, { "Water", 0.5f }, { "Grass", 2f }, { "Ice", 2f }, { "Bug", 2f }, { "Rock", 0.5f }, { "Dragon", 0.5f }, { "Steel", 2f }
            }},
            { "Water", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Fire", 2f }, { "Water", 0.5f }, { "Grass", 0.5f }, { "Ground", 2f }, { "Rock", 2f }, { "Dragon", 0.5f }
            }},
            { "Electric", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Water", 2f }, { "Electric", 0.5f }, { "Grass", 0.5f }, { "Ground", 0f }, { "Flying", 2f }, { "Dragon", 0.5f }
            }},
            { "Grass", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Fire", 0.5f }, { "Water", 2f }, { "Grass", 0.5f }, { "Poison", 0.5f }, { "Ground", 2f }, { "Flying", 0.5f }, { "Bug", 0.5f }, { "Rock", 2f }, { "Dragon", 0.5f }, { "Steel", 0.5f }
            }},
            { "Ice", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Fire", 0.5f }, { "Water", 0.5f }, { "Grass", 2f }, { "Ice", 0.5f }, { "Ground", 2f }, { "Flying", 2f }, { "Dragon", 2f }, { "Steel", 0.5f }
            }},
            { "Fighting", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Normal", 2f }, { "Ice", 2f }, { "Poison", 0.5f }, { "Flying", 0.5f }, { "Psychic", 0.5f }, { "Bug", 0.5f }, { "Rock", 2f }, { "Ghost", 0f }, { "Dark", 2f }, { "Steel", 2f }, { "Fairy", 0.5f }
            }},
            { "Poison", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Grass", 2f }, { "Poison", 0.5f }, { "Ground", 0.5f }, { "Rock", 0.5f }, { "Ghost", 0.5f }, { "Steel", 0f }, { "Fairy", 2f }
            }},
            { "Ground", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Fire", 2f }, { "Electric", 2f }, { "Grass", 0.5f }, { "Poison", 2f }, { "Flying", 0f }, { "Bug", 0.5f }, { "Rock", 2f }, { "Steel", 2f }
            }},
            { "Flying", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Electric", 0.5f }, { "Grass", 2f }, { "Fighting", 2f }, { "Bug", 2f }, { "Rock", 0.5f }, { "Steel", 0.5f }
            }},
            { "Psychic", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Fighting", 2f }, { "Poison", 2f }, { "Psychic", 0.5f }, { "Dark", 0f }, { "Steel", 0.5f }
            }},
            { "Bug", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Fire", 0.5f }, { "Grass", 2f }, { "Fighting", 0.5f }, { "Poison", 0.5f }, { "Flying", 0.5f }, { "Psychic", 2f }, { "Ghost", 0.5f }, { "Dark", 2f }, { "Steel", 0.5f }, { "Fairy", 0.5f }
            }},
            { "Rock", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Fire", 2f }, { "Ice", 2f }, { "Fighting", 0.5f }, { "Ground", 0.5f }, { "Flying", 2f }, { "Bug", 2f }, { "Steel", 0.5f }
            }},
            { "Ghost", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Normal", 0f }, { "Psychic", 2f }, { "Ghost", 2f }, { "Dark", 0.5f }
            }},
            { "Dragon", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Dragon", 2f }, { "Steel", 0.5f }, { "Fairy", 0f }
            }},
            { "Dark", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Fighting", 0.5f }, { "Psychic", 2f }, { "Ghost", 2f }, { "Dark", 0.5f }, { "Fairy", 0.5f }
            }},
            { "Steel", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Fire", 0.5f }, { "Water", 0.5f }, { "Electric", 0.5f }, { "Ice", 2f }, { "Rock", 2f }, { "Steel", 0.5f }, { "Fairy", 2f }
            }},
            { "Fairy", new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase) {
                { "Fire", 0.5f }, { "Fighting", 2f }, { "Poison", 0.5f }, { "Dragon", 2f }, { "Dark", 2f }, { "Steel", 0.5f }
            }}
        };

        /// <summary>
        /// Gets the float multiplier of an attack against a list of defender types.
        /// </summary>
        public static float GetEffectiveness(string attackType, List<string> defenderTypes)
        {
            if (string.IsNullOrEmpty(attackType) || defenderTypes == null || defenderTypes.Count == 0) return 1f;
            
            float multiplier = 1f;
            if (chart.TryGetValue(attackType, out var defenseDict))
            {
                foreach (var defType in defenderTypes)
                {
                    if (defenseDict.TryGetValue(defType, out float val))
                    {
                        multiplier *= val;
                    }
                }
            }
            return multiplier;
        }
    }
}
