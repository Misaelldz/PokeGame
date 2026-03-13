using Godot;
using System.Collections.Generic;
using PokeIdle.Core.Models;
using PokeIdle.Core.Services;

namespace PokeIdle.Core.Systems
{
    /// <summary>
    /// EvolutionSystem: Detecta si un Pokémon puede evolucionar y ejecuta la transición.
    ///
    /// Se llama desde BattleSystem (HandleVictory) después de repartir XP y level-ups.
    /// No es un Node de Godot porque no necesita estar en el árbol de escenas;
    /// es lógica pura que opera sobre datos.
    /// </summary>
    public static class EvolutionSystem
    {
        /// <summary>
        /// Verifica si el Pokémon dado cumple las condiciones para evolucionar.
        /// Devuelve el PokemonData de la evolución, o null si no aplica.
        /// </summary>
        /// <param name="pokemon">El Pokémon a verificar</param>
        public static PokemonData CheckEvolution(PokemonData pokemon)
        {
            if (pokemon == null) return null;

            // Obtener los datos de evolución desde la base de datos
            EvolutionData evo = DatabaseService.GetEvolutionFor(pokemon.Id);
            if (evo == null) return null;

            // Verificar el trigger de la evolución
            bool conditionMet = evo.Trigger?.ToLower() switch
            {
                "level-up" => pokemon.Level >= (evo.MinLevel ?? 0),
                // Aquí se pueden añadir más triggers en el futuro:
                // "use-item"    → requiere que el jugador use un ítem (no aplica en auto-combat)
                // "trade"       → no implementado en idle games
                // "friendship"  → requiere contador de amistad (expandible)
                _ => false
            };

            if (!conditionMet) return null;

            // Obtener los datos de la forma evolucionada
            return DatabaseService.GetPokemonById(evo.ToPokemonId);
        }

        /// <summary>
        /// Aplica la evolución: transfiere nivel, HP actual y XP a la nueva forma.
        /// Devuelve el nuevo PokemonData ya configurado.
        /// </summary>
        /// <param name="current">El Pokémon antes de evolucionar</param>
        /// <param name="evolvedForm">El PokemonData de la forma evolucionada</param>
        public static PokemonData ApplyEvolution(PokemonData current, PokemonData evolvedForm)
        {
            if (current == null || evolvedForm == null) return current;

            GD.Print($"[EvolutionSystem] ¡{current.Name} evoluciona a {evolvedForm.Name}!");

            // Transferir el progreso y cosméticos
            evolvedForm.Level = current.Level;
            evolvedForm.CurrentXp = current.CurrentXp;
            evolvedForm.GrowthRate = current.GrowthRate;
            evolvedForm.IsShiny = current.IsShiny;
            evolvedForm.FormSuffix = current.FormSuffix;

            // Calcular HP proporcional: mantener el mismo porcentaje de HP que tenía antes
            int oldMaxHp = CombatantState.CalculateMaxHp(current.Stats.Hp, current.Level);
            int newMaxHp = CombatantState.CalculateMaxHp(evolvedForm.Stats.Hp, evolvedForm.Level);

            float hpRatio = oldMaxHp > 0 ? (float)current.CurrentHp / oldMaxHp : 1f;
            evolvedForm.CurrentHp = (int)(newMaxHp * hpRatio);

            return evolvedForm;
        }

        /// <summary>
        /// Verifica y aplica evoluciones para todo el equipo.
        /// Devuelve una lista de índices de Pokémon que evolucionaron.
        /// BattleSystem puede usar esto para disparar animaciones.
        /// </summary>
        public static List<int> ProcessTeamEvolutions(List<PokemonData> team)
        {
            var evolved = new List<int>();
            if (team == null) return evolved;

            for (int i = 0; i < team.Count; i++)
            {
                var evolvedForm = CheckEvolution(team[i]);
                if (evolvedForm == null) continue;

                team[i] = ApplyEvolution(team[i], evolvedForm);
                evolved.Add(i);
            }

            return evolved;
        }
    }
}
