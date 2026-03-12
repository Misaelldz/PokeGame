using Godot;
using System.Collections.Generic;
using PokeIdle.Core.Models;
using PokeIdle.Core.Services;
using PokeIdle.Core.Engines;
using PokeIdle.Core.Autoloads;

namespace PokeIdle.Core.Systems
{
    /// <summary>
    /// BattleSystem: El orquestador central del loop de combate.
    ///
    /// Es un Node de Godot (no un autoload) que debe ser hijo de la BattleScene.
    /// Usa _Process(delta) como "motor de ticks", lo que reemplaza el setInterval
    /// de useEngineTick.ts. No contiene matemáticas: delega en los Engines estáticos.
    ///
    /// Flujo de estados:
    ///   Idle -> Spawning -> PlayerTurn <-> EnemyTurn -> Victory/Defeat -> Idle
    ///                                   -> CatchAttempt -> Victory/Idle
    /// </summary>
    public partial class BattleSystem : Node
    {
        // -------------------------------------------------------------------------
        // Señales (equivalentes a los eventos de React/zustand)
        // -------------------------------------------------------------------------

        [Signal] public delegate void BattleStartedEventHandler(string enemyName, bool isShiny);
        [Signal] public delegate void TurnCompletedEventHandler(int playerHp, int enemyHp, string log);
        [Signal] public delegate void PokemonCaughtEventHandler(int pokemonId, bool isShiny);
        [Signal] public delegate void BattleEndedEventHandler(bool playerWon, int xpGained, int goldGained);
        [Signal] public delegate void LevelUpEventHandler(int teamIndex, int newLevel);
        [Signal] public delegate void PhaseChangedEventHandler(string newPhase);

        // -------------------------------------------------------------------------
        // Estado interno
        // -------------------------------------------------------------------------

        private BattleState _state = new BattleState();

        // Referencia al GameManager (Autoload)
        private GameManager _gameManager;

        // -------------------------------------------------------------------------
        // Configuración (exportable desde el Inspector de Godot)
        // -------------------------------------------------------------------------

        [Export] public float ActionCooldownSeconds = 1.5f;  // Tiempo entre acciones (auto-combat)
        [Export] public float SpawnDelaySeconds = 0.5f;      // Tiempo antes de que aparezca el enemigo

        // -------------------------------------------------------------------------
        // Godot Lifecycle
        // -------------------------------------------------------------------------

        public override void _Ready()
        {
            // Obtener referencia al GameManager (Autoload registrado en project.godot)
            _gameManager = GetNode<GameManager>("/root/GameManager");
            _state.ActionCooldown = ActionCooldownSeconds;

            GD.Print("[BattleSystem] Listo. Iniciando primer spawn...");
            TriggerNextEncounter();
        }

        public override void _Process(double delta)
        {
            // No hacer nada si el juego está pausado o si no hay combate activo
            if (_state.Phase == BattlePhase.Paused || _state.Phase == BattlePhase.Idle)
                return;

            _state.TimeSinceLastAction += (float)delta;

            switch (_state.Phase)
            {
                case BattlePhase.Spawning:
                    HandleSpawning();
                    break;

                case BattlePhase.PlayerTurn:
                    if (_state.TimeSinceLastAction >= _state.ActionCooldown)
                        ExecutePlayerTurn();
                    break;

                case BattlePhase.EnemyTurn:
                    if (_state.TimeSinceLastAction >= _state.ActionCooldown * 0.7f) // El turno enemigo es más rápido
                        ExecuteEnemyTurn();
                    break;

                case BattlePhase.Victory:
                    HandleVictory();
                    break;

                case BattlePhase.Defeat:
                    HandleDefeat();
                    break;
            }
        }

        // -------------------------------------------------------------------------
        // API Pública (llamada desde la UI o desde inputs del jugador)
        // -------------------------------------------------------------------------

        /// <summary>
        /// El jugador lanza una Pokéball. Solo válido si estamos en PlayerTurn.
        /// </summary>
        public void AttemptCapture(string ballType)
        {
            if (_state.Phase != BattlePhase.PlayerTurn || _state.IsTrainerBattle)
            {
                GD.PrintErr("[BattleSystem] AttemptCapture: Condición inválida.");
                return;
            }

            float ballBonus = GetBallBonus(ballType);
            float statusBonus = GetStatusBonus(_state.EnemyCombatant.StatusCondition);

            bool caught = CaptureEngine.TryCatch(
                _state.EnemyCombatant.MaxHp,
                _state.EnemyCombatant.CurrentHp,
                _state.EnemyCombatant.Data.CatchRate,
                ballBonus,
                statusBonus
            );

            if (caught)
            {
                _state.LastCatchSuccess = true;
                _state.Phase = BattlePhase.Victory;
                EmitSignal(SignalName.PokemonCaught, _state.EnemyCombatant.Data.Id, _state.EnemyCombatant.IsShiny);
            }
            else
            {
                _state.LastCatchSuccess = false;
                _state.Phase = BattlePhase.EnemyTurn;
                _state.TimeSinceLastAction = 0f;
            }
        }

        /// <summary>
        /// Activa o desactiva el modo automático de combate.
        /// </summary>
        public void SetAutoMode(bool enabled)
        {
            _state.IsAutoMode = enabled;
        }

        /// <summary>
        /// Pausa o reanuda el sistema de batalla.
        /// </summary>
        public void SetPaused(bool paused)
        {
            _state.Phase = paused ? BattlePhase.Paused : BattlePhase.PlayerTurn;
            EmitSignal(SignalName.PhaseChanged, _state.Phase.ToString());
        }

        // -------------------------------------------------------------------------
        // Lógica de fases (privada)
        // -------------------------------------------------------------------------

        private void HandleSpawning()
        {
            // Esperamos el delay de spawn antes de activar el turno del jugador
            if (_state.TimeSinceLastAction >= SpawnDelaySeconds)
            {
                _state.Phase = BattlePhase.PlayerTurn;
                _state.TimeSinceLastAction = 0f;
                EmitSignal(SignalName.PhaseChanged, _state.Phase.ToString());
            }
        }

        private void ExecutePlayerTurn()
        {
            if (_state.PlayerCombatant == null || _state.EnemyCombatant == null) return;

            _state.TimeSinceLastAction = 0f;

            // Obtener el movimiento a usar (el de mayor poder con PP disponibles)
            // TODO: cuando tengamos el sistema de PP, filtrar por PP > 0
            var move = GetBestMove(_state.PlayerCombatant.Data);

            // Calcular daño
            int atk = CombatantState.CalculateStat(_state.PlayerCombatant.Data.Stats.Atk, _state.PlayerCombatant.Data.Level);
            int def = CombatantState.CalculateStat(_state.EnemyCombatant.Data.Stats.Def, _state.EnemyCombatant.Data.Level);

            bool isCrit = CombatEngine.IsCritical(critStage: 0);
            float variance = CombatEngine.GetRandomVariance();
            float typeEff = TypeChart.GetEffectiveness(move?.Type ?? "Normal", _state.EnemyCombatant.Data.Types);
            float stab = _state.PlayerCombatant.Data.Types.Contains(move?.Type ?? "") ? 1.5f : 1f;

            float modifier = variance * typeEff * stab * (isCrit ? 1.5f : 1f);
            int damage = CombatEngine.CalculateDamage(
                _state.PlayerCombatant.Data.Level,
                move?.Power ?? 40,
                atk,
                def,
                modifier
            );

            _state.EnemyCombatant.CurrentHp -= damage;
            _state.LastDamageDealt = damage;
            _state.LastActionWasCritical = isCrit;
            _state.LastTypeEffectiveness = typeEff;
            _state.TurnCount++;

            string log = $"{_state.PlayerCombatant.Data.Name} usó {move?.Name ?? "Placaje"} — {damage} daño!";
            if (isCrit) log += " ¡Golpe Crítico!";
            if (typeEff >= 2f) log += " ¡Es muy efectivo!";
            if (typeEff <= 0.5f) log += " No es muy efectivo...";
            _state.LastActionLog = log;

            EmitSignal(SignalName.TurnCompleted,
                _state.PlayerCombatant.CurrentHp,
                _state.EnemyCombatant.CurrentHp,
                log);

            if (_state.EnemyCombatant.IsFainted)
            {
                _state.Phase = BattlePhase.Victory;
            }
            else
            {
                _state.Phase = BattlePhase.EnemyTurn;
            }

            EmitSignal(SignalName.PhaseChanged, _state.Phase.ToString());
        }

        private void ExecuteEnemyTurn()
        {
            if (_state.PlayerCombatant == null || _state.EnemyCombatant == null) return;

            _state.TimeSinceLastAction = 0f;

            // El enemigo usa su movimiento de mayor poder también (IA básica)
            var move = GetBestMove(_state.EnemyCombatant.Data);

            int atk = CombatantState.CalculateStat(_state.EnemyCombatant.Data.Stats.SpAtk, _state.EnemyCombatant.Data.Level);
            int def = CombatantState.CalculateStat(_state.PlayerCombatant.Data.Stats.SpDef, _state.PlayerCombatant.Data.Level);

            float typeEff = TypeChart.GetEffectiveness(move?.Type ?? "Normal", _state.PlayerCombatant.Data.Types);
            float variance = CombatEngine.GetRandomVariance();
            float modifier = variance * typeEff;

            int damage = CombatEngine.CalculateDamage(
                _state.EnemyCombatant.Data.Level,
                move?.Power ?? 40,
                atk,
                def,
                modifier
            );

            _state.PlayerCombatant.CurrentHp -= damage;
            _state.LastDamageReceived = damage;

            string log = $"{_state.EnemyCombatant.Data.Name} usó {move?.Name ?? "Placaje"} — {damage} daño al jugador!";
            _state.LastActionLog = log;

            EmitSignal(SignalName.TurnCompleted,
                _state.PlayerCombatant.CurrentHp,
                _state.EnemyCombatant.CurrentHp,
                log);

            if (_state.PlayerCombatant.IsFainted)
            {
                _state.Phase = BattlePhase.Defeat;
            }
            else
            {
                _state.Phase = BattlePhase.PlayerTurn;
            }

            EmitSignal(SignalName.PhaseChanged, _state.Phase.ToString());
        }

        private void HandleVictory()
        {
            _state.Phase = BattlePhase.Idle;

            // Calcular recompensas
            int xp = XpEngine.CalculateBattleXp(
                _state.EnemyCombatant.Data.Level,
                _state.EnemyCombatant.Data.BaseExpYield,
                isWild: !_state.IsTrainerBattle
            );
            int gold = CalculateGold(_state.EnemyCombatant.Data.Level);

            _state.XpGainedThisBattle = xp;
            _state.GoldGainedThisBattle = gold;

            // Entregar XP al Pokémon activo del GameManager
            // TODO: cuando haya un sistema de PP y equipo completo, distribuir a todo el equipo
            _gameManager?.AddExperience(xp);
            _gameManager?.AddGold(gold);

            EmitSignal(SignalName.BattleEnded, true, xp, gold);

            // Breve pausa antes de generar el siguiente encuentro
            GetTree().CreateTimer(0.8f).Timeout += TriggerNextEncounter;
        }

        private void HandleDefeat()
        {
            _state.Phase = BattlePhase.Idle;
            GD.Print("[BattleSystem] El jugador fue derrotado.");
            EmitSignal(SignalName.BattleEnded, false, 0, 0);
            // TODO: lógica de game-over o de recuperación (pokémon se desmayan)
        }

        // -------------------------------------------------------------------------
        // Helpers de configuración de encuentro
        // -------------------------------------------------------------------------

        private void TriggerNextEncounter()
        {
            _state.Reset();

            // Obtener zona actual del GameManager
            var currentZone = _gameManager != null
                ? DatabaseService.GetZoneById(_gameManager.CurrentZoneId)
                : null;

            if (currentZone == null)
            {
                GD.PrintErr("[BattleSystem] Zona actual no encontrada. Abortando spawn.");
                return;
            }

            // Obtener el Pokémon activo del equipo
            var activePokemon = _gameManager?.GetActivePokemon();
            if (activePokemon == null)
            {
                GD.PrintErr("[BattleSystem] No hay Pokémon activo en el equipo.");
                return;
            }

            // Generar enemigo
            var enemy = EncounterService.GenerateWildPokemon(currentZone, activePokemon.Level);
            if (enemy == null)
            {
                GD.PrintErr("[BattleSystem] No se pudo generar un Pokémon salvaje para la zona.");
                return;
            }

            // Configurar el estado del combatiente jugador
            var playerState = new CombatantState
            {
                Data = activePokemon,
                MaxHp = CombatantState.CalculateMaxHp(activePokemon.Stats.Hp, activePokemon.Level),
                CurrentHp = activePokemon.CurrentHp, // HP real del Pokémon (puede venir herido)
            };

            _state.PlayerCombatant = playerState;
            _state.EnemyCombatant = enemy;
            _state.CurrentZoneId = currentZone.Id;
            _state.Phase = BattlePhase.Spawning;
            _state.TimeSinceLastAction = 0f;

            EmitSignal(SignalName.BattleStarted, enemy.Data.Name, enemy.IsShiny);
            EmitSignal(SignalName.PhaseChanged, _state.Phase.ToString());

            GD.Print($"[BattleSystem] Encuentro generado: {enemy.Data.Name} Nv.{enemy.Data.Level}");
        }

        // -------------------------------------------------------------------------
        // Helpers de cálculo
        // -------------------------------------------------------------------------

        private MoveData GetBestMove(PokemonData pokemon)
        {
            if (pokemon?.LevelUpMoves == null || pokemon.LevelUpMoves.Count == 0)
                return null;

            // Filtrar los movimientos disponibles a este nivel
            MoveData best = null;
            int bestPower = -1;

            foreach (var lum in pokemon.LevelUpMoves)
            {
                if (lum.Level > pokemon.Level) continue;

                var moveData = DatabaseService.GetMoveById(lum.MoveId);
                if (moveData == null) continue;

                if (moveData.Power.HasValue && moveData.Power.Value > bestPower)
                {
                    bestPower = moveData.Power.Value;
                    best = moveData;
                }
            }

            return best;
        }

        private float GetBallBonus(string ballType)
        {
            return ballType?.ToLower() switch
            {
                "greatball"  => 1.5f,
                "ultraball"  => 2.0f,
                "masterball" => 255f,
                _            => 1.0f  // pokeball
            };
        }

        private float GetStatusBonus(string status)
        {
            return status?.ToLower() switch
            {
                "sleep"    => 2.5f,
                "freeze"   => 2.5f,
                "paralyze" => 1.5f,
                "burn"     => 1.5f,
                "poison"   => 1.5f,
                _          => 1.0f
            };
        }

        private int CalculateGold(int enemyLevel)
        {
            // Fórmula simple: el jugador gana entre 3 y 10 veces el nivel del enemigo
            return enemyLevel * GD.RandRange(3, 10);
        }
    }
}
