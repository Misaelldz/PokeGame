using System.Collections.Generic;

namespace PokeIdle.Core.Models
{
    /// <summary>
    /// Enumeración de las fases posibles de un combate.
    /// El BattleSystem avanza de fase según las acciones y resultados.
    /// </summary>
    public enum BattlePhase
    {
        Idle,           // Sin combate activo, esperando el siguiente spawn
        Spawning,       // Generando el Pokémon salvaje (animación de entrada)
        PlayerTurn,     // Esperando/ejecutando la acción del jugador (o del auto-combat)
        EnemyTurn,      // El enemigo ejecuta su ataque
        CatchAttempt,   // Se lanzó una Pokéball, calculando resultado
        Victory,        // El enemigo fue derrotado, repartiendo XP y oro
        Defeat,         // El equipo del jugador fue derrotado
        Fleeing,        // El jugador intentó huir
        Paused          // El juego está en pausa, no hay ticks
    }

    /// <summary>
    /// Snapshot del estado de un Pokémon durante el combate.
    /// Usado tanto para el activo del jugador como para el enemigo.
    /// Separa el HP en combate del HP guardado en PokemonData para no mutar
    /// los datos estáticos de la base de datos.
    /// </summary>
    public class CombatantState
    {
        // --- Referencia al modelo base ---
        public PokemonData Data { get; set; }

        // --- Estado mutable en combate ---
        public int CurrentHp { get; set; }
        public int MaxHp { get; set; }

        // --- Modificadores de estadísticas por etapas (rango: -6 a +6) ---
        public int AttackStage { get; set; } = 0;
        public int DefenseStage { get; set; } = 0;
        public int SpAttackStage { get; set; } = 0;
        public int SpDefenseStage { get; set; } = 0;
        public int SpeedStage { get; set; } = 0;

        // --- Estado de condición (parálisis, sueño, etc.) ---
        public string StatusCondition { get; set; } = "none"; // "none", "burn", "poison", "sleep", "paralyze", "freeze"
        public int StatusTurnsRemaining { get; set; } = 0;

        // --- Flags de combate ---
        public bool IsFainted => CurrentHp <= 0;
        public bool IsShiny { get; set; } = false;

        /// <summary>
        /// Calcula el HP máximo según la fórmula oficial de Pokémon.
        /// </summary>
        public static int CalculateMaxHp(int baseHp, int level, int iv = 15)
        {
            // Formula Gen VI+: floor(((2 * Base + IV) * Level / 100) + Level + 10)
            return (int)(((2 * baseHp + iv) * level / 100.0) + level + 10);
        }

        /// <summary>
        /// Calcula un stat de combate (Atk, Def, etc.) según la fórmula oficial.
        /// </summary>
        public static int CalculateStat(int baseStat, int level, int iv = 15)
        {
            // Formula Gen VI+: floor(((2 * Base + IV) * Level / 100) + 5)
            return (int)(((2 * baseStat + iv) * level / 100.0) + 5);
        }
    }

    /// <summary>
    /// El estado completo de una batalla en un instante dado.
    /// BattleSystem lo lee y modifica; las UIs simplemente lo observan para renderizar.
    /// </summary>
    public class BattleState
    {
        // --- Fase actual del combate ---
        public BattlePhase Phase { get; set; } = BattlePhase.Idle;

        // --- Combatientes ---
        public CombatantState PlayerCombatant { get; set; }   // El Pokémon activo del jugador
        public CombatantState EnemyCombatant { get; set; }    // El Pokémon salvaje o del entrenador

        // --- Contexto del encuentro ---
        public string CurrentZoneId { get; set; }
        public bool IsTrainerBattle { get; set; } = false;
        public string TrainerId { get; set; } = null; // Null si es wild battle
        public bool IsAutoMode { get; set; } = true;  // ¿El jugador usa auto-combat?

        // --- Turno y tiempo ---
        public int TurnCount { get; set; } = 0;
        public float TimeSinceLastAction { get; set; } = 0f; // Segundos desde la última acción
        public float ActionCooldown { get; set; } = 1.5f;    // Segundos entre acciones (configurable)

        // --- Resultado de la última acción (para el log de batalla) ---
        public int LastDamageDealt { get; set; } = 0;
        public int LastDamageReceived { get; set; } = 0;
        public bool LastActionWasCritical { get; set; } = false;
        public float LastTypeEffectiveness { get; set; } = 1f;
        public string LastActionLog { get; set; } = "";

        // --- Resultado de captura ---
        public bool LastCatchSuccess { get; set; } = false;
        public int CatchShakeCount { get; set; } = 0; // Para animaciones (0-4 sacudidas)

        // --- Acumuladores de la sesión ---
        public int XpGainedThisBattle { get; set; } = 0;
        public int GoldGainedThisBattle { get; set; } = 0;

        // --- Pokémon que subieron de nivel en esta batalla ---
        public List<int> LevelUpsThisBattle { get; set; } = new List<int>(); // Índices del equipo

        /// <summary>
        /// Reinicia el estado para preparar un nuevo encuentro.
        /// </summary>
        public void Reset()
        {
            Phase = BattlePhase.Idle;
            PlayerCombatant = null;
            EnemyCombatant = null;
            TurnCount = 0;
            TimeSinceLastAction = 0f;
            LastDamageDealt = 0;
            LastDamageReceived = 0;
            LastActionWasCritical = false;
            LastTypeEffectiveness = 1f;
            LastActionLog = "";
            LastCatchSuccess = false;
            CatchShakeCount = 0;
            XpGainedThisBattle = 0;
            GoldGainedThisBattle = 0;
            LevelUpsThisBattle.Clear();
        }
    }
}
