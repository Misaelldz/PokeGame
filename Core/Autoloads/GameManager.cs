using Godot;
using System.Collections.Generic;
using PokeIdle.Core.Models;
using PokeIdle.Core.Engines;
using PokeIdle.Core.Services;

namespace PokeIdle.Core.Autoloads
{
    /// <summary>
    /// Estado de una partida (Run) concreta. Se resetea al perder o reiniciar.
    /// </summary>
    public partial class RunState : GodotObject
    {
        public int Money { get; set; } = 0;
        public Dictionary<string, int> Items { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// Autoload singleton. Mantiene el estado global del juego:
    /// equipo del jugador, zona activa, dinero y meta-progresión.
    /// BattleSystem y otros sistemas lo consultan para leer/escribir estado.
    /// </summary>
    public partial class GameManager : Node
    {
        // ----------------------------------------------------------------
        // Señales
        // ----------------------------------------------------------------
        [Signal] public delegate void RunMoneyChangedEventHandler(int newAmount);
        [Signal] public delegate void TeamUpdatedEventHandler();
        [Signal] public delegate void LevelUpEventHandler(int teamIndex, int newLevel);
        [Signal] public delegate void ZoneChangedEventHandler(string newZoneId);

        // ----------------------------------------------------------------
        // Meta-progresión (persiste entre runs)
        // ----------------------------------------------------------------
        public int GlobalEmeralds { get; set; } = 0;
        public int TotalRunsPlayed { get; set; } = 0;

        // ----------------------------------------------------------------
        // Estado de la Run actual (se resetea al morir/reiniciar)
        // ----------------------------------------------------------------
        public RunState CurrentRun { get; set; } = null;

        /// <summary>
        /// Equipo activo del jugador. Máximo 6 Pokémon (estándar Pokémon).
        /// PokemonData tiene Level y CurrentHp como campos mutables de instancia.
        /// </summary>
        public List<PokemonData> Team { get; private set; } = new List<PokemonData>();

        /// <summary>
        /// ID de la zona donde el jugador está combatiendo actualmente.
        /// EncounterService lo usa para generar los salvajes correctos.
        /// </summary>
        public string CurrentZoneId { get; private set; } = "";

        // ----------------------------------------------------------------
        // Lifecycle
        // ----------------------------------------------------------------
        public override void _Ready()
        {
            GD.Print("[GameManager] Inicializado.");
        }

        // ----------------------------------------------------------------
        // Control de Run
        // ----------------------------------------------------------------

        /// <summary>
        /// Inicia una nueva partida: resetea equipo, dinero e ítems.
        /// </summary>
        public void StartNewRun(string startingZoneId = "zone_1")
        {
            CurrentRun = new RunState { Money = 500 };
            Team.Clear();
            TotalRunsPlayed++;
            SetZone(startingZoneId);

            GD.Print($"[GameManager] === NUEVA RUN #{TotalRunsPlayed} iniciada ===");
            EmitSignal(SignalName.RunMoneyChanged, CurrentRun.Money);
            EmitSignal(SignalName.TeamUpdated);
        }

        // ----------------------------------------------------------------
        // Dinero
        // ----------------------------------------------------------------

        /// <summary>
        /// Añade oro al saldo de la run actual. Alias público limpio para BattleSystem.
        /// </summary>
        public void AddGold(int amount)
        {
            if (CurrentRun == null) return;
            CurrentRun.Money += amount;
            EmitSignal(SignalName.RunMoneyChanged, CurrentRun.Money);
            GD.Print($"[GameManager] +{amount} oro → Total: {CurrentRun.Money}");
        }

        // ----------------------------------------------------------------
        // Equipo
        // ----------------------------------------------------------------

        /// <summary>
        /// Devuelve el primer Pokémon del equipo que no esté desmayado.
        /// BattleSystem lo usa para saber quién pelea actualmente.
        /// </summary>
        public PokemonData GetActivePokemon()
        {
            foreach (var pokemon in Team)
            {
                if (pokemon.CurrentHp > 0)
                    return pokemon;
            }
            return null; // Todo el equipo desmayado → derrota
        }

        /// <summary>
        /// Añade un Pokémon al equipo (captura, starter, etc.).
        /// Respeta el límite de 6.
        /// </summary>
        public bool AddToTeam(PokemonData pokemon)
        {
            if (Team.Count >= 6)
            {
                GD.Print("[GameManager] Equipo lleno. No se puede añadir más Pokémon.");
                return false;
            }
            // Asegurar que el Pokémon entre con HP máximo
            pokemon.CurrentHp = CombatantState.CalculateMaxHp(pokemon.Stats.Hp, pokemon.Level);
            Team.Add(pokemon);
            EmitSignal(SignalName.TeamUpdated);
            GD.Print($"[GameManager] {pokemon.Name} añadido al equipo (slot {Team.Count}).");
            return true;
        }

        // ----------------------------------------------------------------
        // Experiencia y Level-Up
        // ----------------------------------------------------------------

        /// <summary>
        /// Entrega XP al Pokémon activo y gestiona subidas de nivel.
        /// Llamado por BattleSystem tras cada victoria.
        /// </summary>
        public void AddExperience(int xp)
        {
            var active = GetActivePokemon();
            if (active == null) return;

            active.CurrentXp += xp;
            GD.Print($"[GameManager] {active.Name} recibió {xp} XP. Total: {active.CurrentXp}");

            // Verificar si sube varios niveles (puede pasar con Pokémon de bajo nivel)
            CheckAndApplyLevelUps(active);
        }

        // ----------------------------------------------------------------
        // Zona
        // ----------------------------------------------------------------

        /// <summary>
        /// Cambia la zona activa del jugador.
        /// </summary>
        public void SetZone(string zoneId)
        {
            CurrentZoneId = zoneId;
            EmitSignal(SignalName.ZoneChanged, zoneId);
            GD.Print($"[GameManager] Zona activa: {zoneId}");
        }

        // ----------------------------------------------------------------
        // Inventario
        // ----------------------------------------------------------------

        /// <summary>
        /// Añade ítems al inventario de la run actual.
        /// </summary>
        public void AddItem(string itemId, int amount = 1)
        {
            if (CurrentRun == null) return;

            if (CurrentRun.Items.ContainsKey(itemId))
                CurrentRun.Items[itemId] += amount;
            else
                CurrentRun.Items[itemId] = amount;

            GD.Print($"[GameManager] +{amount}x {itemId}");
        }

        /// <summary>
        /// Consume un ítem del inventario. Devuelve false si no hay suficientes.
        /// </summary>
        public bool ConsumeItem(string itemId, int amount = 1)
        {
            if (CurrentRun == null) return false;
            if (!CurrentRun.Items.TryGetValue(itemId, out int count)) return false;
            if (count < amount) return false;

            CurrentRun.Items[itemId] -= amount;
            if (CurrentRun.Items[itemId] <= 0)
                CurrentRun.Items.Remove(itemId);

            return true;
        }

        // ----------------------------------------------------------------
        // Helpers privados
        // ----------------------------------------------------------------

        private void CheckAndApplyLevelUps(PokemonData pokemon)
        {
            int teamIndex = Team.IndexOf(pokemon);

            while (pokemon.Level < 100)
            {
                int xpNeeded = XpEngine.GetExpForLevel(pokemon.GrowthRate, pokemon.Level + 1);
                if (pokemon.CurrentXp < xpNeeded) break;

                pokemon.Level++;
                // Restaurar proporcional de HP al subir de nivel (como en los juegos originales)
                int newMaxHp = CombatantState.CalculateMaxHp(pokemon.Stats.Hp, pokemon.Level);
                int hpGained = newMaxHp - CombatantState.CalculateMaxHp(pokemon.Stats.Hp, pokemon.Level - 1);
                pokemon.CurrentHp = System.Math.Min(pokemon.CurrentHp + hpGained, newMaxHp);

                GD.Print($"[GameManager] ¡{pokemon.Name} subió al nivel {pokemon.Level}!");
                EmitSignal(SignalName.LevelUp, teamIndex, pokemon.Level);
                EmitSignal(SignalName.TeamUpdated);
            }
        }
    }
}
