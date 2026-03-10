using System;
using System.Collections.Generic;
using System.Linq;
using PokeIdle.Core.Models;

namespace PokeIdle.Core.Services
{
    /// <summary>
    /// Genera encuentros con Pokémon salvajes según la zona actual.
    /// Es una clase estática pura: solo necesita datos de entrada para producir un CombatantState.
    /// </summary>
    public static class EncounterService
    {
        private static readonly Random rng = new Random();

        // Probabilidad de encontrar un Pokémon shiny (1/4096 Gen VI+)
        private const int ShinyOdds = 4096;

        /// <summary>
        /// Genera un CombatantState para un Pokémon salvaje según la zona dada.
        /// Devuelve null si no hay Pokémon disponibles en esa zona.
        /// </summary>
        /// <param name="zone">Los datos de la zona actual</param>
        /// <param name="playerMaxLevel">Nivel máximo del equipo del jugador (para escalar enemigos)</param>
        public static CombatantState GenerateWildPokemon(ZoneData zone, int playerMaxLevel)
        {
            if (zone == null || zone.Pokemon == null || zone.Pokemon.Count == 0)
                return null;

            // 1. Seleccionar especie por peso (rarity/rate)
            PokemonData species = SelectPokemonByWeight(zone.Pokemon);
            if (species == null) return null;

            // 2. Calcular el nivel del encuentro
            int level = RollLevel(zone.MinLevel, zone.MaxLevel, playerMaxLevel);

            // 3. Calcular estadísticas reales (fórmulas oficiales)
            int maxHp = CombatantState.CalculateMaxHp(species.Stats.Hp, level);

            // 4. Determinar si es shiny
            bool isShiny = rng.Next(0, ShinyOdds) == 0;

            // 5. Seleccionar el movimiento que usará el salvaje
            // Por ahora, el enemigo usa el movimiento de mayor poder disponible a su nivel
            // (lógica de IA básica, expandible).
            // Se puede sofisticar luego con sets de movimientos por nivel.

            return new CombatantState
            {
                Data = species,
                MaxHp = maxHp,
                CurrentHp = maxHp,
                IsShiny = isShiny,
                AttackStage = 0,
                DefenseStage = 0,
                StatusCondition = "none",
            };
        }

        /// <summary>
        /// Genera un Pokémon de Entrenador/Gimnasio directamente desde datos predefinidos.
        /// Útil para batallas de Gym donde el Pokémon ya viene a un nivel fijo.
        /// </summary>
        public static CombatantState GenerateTrainerPokemon(PokemonData data, int level)
        {
            if (data == null) return null;

            int maxHp = CombatantState.CalculateMaxHp(data.Stats.Hp, level);

            return new CombatantState
            {
                Data = data,
                MaxHp = maxHp,
                CurrentHp = maxHp,
                IsShiny = false,
                AttackStage = 0,
                DefenseStage = 0,
                StatusCondition = "none",
            };
        }

        // -------------------------------------------------------------------------
        // Helpers privados
        // -------------------------------------------------------------------------

        /// <summary>
        /// Selecciona un Pokémon de la lista según sus pesos de aparición.
        /// Los Pokémon con mayor "SpawnRate" en ZoneData aparecen con mayor frecuencia.
        /// </summary>
        private static PokemonData SelectPokemonByWeight(List<ZonePokemonEntry> entries)
        {
            // Suma total de pesos
            int totalWeight = entries.Sum(e => e.SpawnRate);
            if (totalWeight <= 0) return null;

            int roll = rng.Next(0, totalWeight);
            int cumulative = 0;

            foreach (var entry in entries)
            {
                cumulative += entry.SpawnRate;
                if (roll < cumulative)
                {
                    // Buscar en la base de datos el PokemonData por ID
                    return DatabaseService.GetPokemonById(entry.PokemonId);
                }
            }

            // Fallback: devolver el primero
            return DatabaseService.GetPokemonById(entries[0].PokemonId);
        }

        /// <summary>
        /// Calcula el nivel del Pokémon salvaje. El nivel varía dentro del rango de la zona,
        /// pero se "empuja" levemente hacia el nivel del jugador para que los encuentros
        /// sean siempre relevantes y no triviales.
        /// </summary>
        private static int RollLevel(int zoneMin, int zoneMax, int playerMaxLevel)
        {
            // Rango base de la zona
            int level = rng.Next(zoneMin, zoneMax + 1);

            // Limitar al rango de la zona pase lo que pase
            return Math.Clamp(level, zoneMin, zoneMax);
        }
    }
}
