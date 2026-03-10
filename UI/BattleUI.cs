using Godot;
using PokeIdle.Core.Models;
using PokeIdle.Core.Systems;
using PokeIdle.Core.Autoloads;

namespace PokeIdle.UI
{
    /// <summary>
    /// BattleUI: El nodo de interfaz visual de la batalla.
    ///
    /// Su único trabajo es ESCUCHAR las señales del BattleSystem y
    /// actualizar los elementos visuales en consecuencia. No contiene
    /// ninguna lógica de juego — esa vive en BattleSystem y los Engines.
    ///
    /// Estructura esperada en la escena:
    ///
    ///   BattleScene
    ///     ├── BattleSystem          (Node, lógica)
    ///     └── BattleUI              (Control, este script)
    ///           ├── EnemyPanel
    ///           │     ├── EnemyName       (Label)
    ///           │     ├── EnemyHpBar      (ProgressBar)
    ///           │     └── EnemySprite     (TextureRect)
    ///           ├── PlayerPanel
    ///           │     ├── PlayerName      (Label)
    ///           │     └── PlayerHpBar     (ProgressBar)
    ///           ├── BattleLog           (RichTextLabel)
    ///           ├── ActionButtons       (HBoxContainer)
    ///           │     ├── BtnAutoToggle  (CheckButton)
    ///           │     └── BtnBall        (Button)
    ///           └── RewardPopup         (Control, oculto por defecto)
    /// </summary>
    public partial class BattleUI : Control
    {
        // ----------------------------------------------------------------
        // Node references — asignados vía Inspector o buscados en _Ready
        // ----------------------------------------------------------------
        [Export] public NodePath BattleSystemPath;     // Ruta al BattleSystem hermano

        // Paneles de combatientes
        [Export] public NodePath EnemyNamePath;
        [Export] public NodePath EnemyHpBarPath;
        [Export] public NodePath EnemySpritePath;
        [Export] public NodePath EnemyLevelPath;
        [Export] public NodePath PlayerNamePath;
        [Export] public NodePath PlayerHpBarPath;

        // Log y fase
        [Export] public NodePath BattleLogPath;
        [Export] public NodePath PhaseIndicatorPath;

        // Botones de acción
        [Export] public NodePath BtnAutoTogglePath;
        [Export] public NodePath BtnBallPath;

        // Popup de recompensa
        [Export] public NodePath RewardPopupPath;
        [Export] public NodePath RewardTextPath;

        // ----------------------------------------------------------------
        // Nodos internos
        // ----------------------------------------------------------------
        private Label        _enemyName;
        private ProgressBar  _enemyHpBar;
        private TextureRect  _enemySprite;
        private Label        _enemyLevel;
        private Label        _playerName;
        private ProgressBar  _playerHpBar;
        private RichTextLabel _battleLog;
        private Label        _phaseIndicator;
        private CheckButton  _btnAutoToggle;
        private Button       _btnBall;
        private Control      _rewardPopup;
        private Label        _rewardText;

        // ----------------------------------------------------------------
        // Referencias
        // ----------------------------------------------------------------
        private BattleSystem _battleSystem;
        private GameManager  _gm;

        // ----------------------------------------------------------------
        // Estado de la UI
        // ----------------------------------------------------------------
        private int _playerMaxHp  = 1;
        private int _enemyMaxHp   = 1;

        // ----------------------------------------------------------------
        // Lifecycle
        // ----------------------------------------------------------------

        public override void _Ready()
        {
            _gm = GetNode<GameManager>("/root/GameManager");

            // Buscar BattleSystem
            _battleSystem = BattleSystemPath != null
                ? GetNode<BattleSystem>(BattleSystemPath)
                : GetParent().GetNodeOrNull<BattleSystem>("BattleSystem");

            if (_battleSystem == null)
            {
                GD.PrintErr("[BattleUI] No se encontró BattleSystem. Revisa el NodePath.");
                return;
            }

            // Conectar señales
            _battleSystem.BattleStarted    += OnBattleStarted;
            _battleSystem.TurnCompleted    += OnTurnCompleted;
            _battleSystem.BattleEnded      += OnBattleEnded;
            _battleSystem.PokemonCaught    += OnPokemonCaught;
            _battleSystem.PhaseChanged     += OnPhaseChanged;
            _battleSystem.LevelUp          += OnLevelUp;

            // Obtener nodos por NodePath (si están asignados)
            _enemyName      = GetNodeOrNullByPath<Label>(EnemyNamePath);
            _enemyHpBar     = GetNodeOrNullByPath<ProgressBar>(EnemyHpBarPath);
            _enemySprite    = GetNodeOrNullByPath<TextureRect>(EnemySpritePath);
            _enemyLevel     = GetNodeOrNullByPath<Label>(EnemyLevelPath);
            _playerName     = GetNodeOrNullByPath<Label>(PlayerNamePath);
            _playerHpBar    = GetNodeOrNullByPath<ProgressBar>(PlayerHpBarPath);
            _battleLog      = GetNodeOrNullByPath<RichTextLabel>(BattleLogPath);
            _phaseIndicator = GetNodeOrNullByPath<Label>(PhaseIndicatorPath);
            _btnAutoToggle  = GetNodeOrNullByPath<CheckButton>(BtnAutoTogglePath);
            _btnBall        = GetNodeOrNullByPath<Button>(BtnBallPath);
            _rewardPopup    = GetNodeOrNullByPath<Control>(RewardPopupPath);
            _rewardText     = GetNodeOrNullByPath<Label>(RewardTextPath);

            // Conectar botones
            if (_btnAutoToggle != null)
                _btnAutoToggle.Toggled += (on) => _battleSystem.SetAutoMode(on);

            if (_btnBall != null)
                _btnBall.Pressed += () => _battleSystem.AttemptCapture("poke-ball");

            HideRewardPopup();
            GD.Print("[BattleUI] Listo y conectado al BattleSystem.");
        }

        // ----------------------------------------------------------------
        // Handlers de señales del BattleSystem
        // ----------------------------------------------------------------

        /// <summary>
        /// Una nueva batalla comenzó: actualizar nombre y preparar barras.
        /// </summary>
        private void OnBattleStarted(string enemyName, bool isShiny)
        {
            // Nombre del enemigo
            if (_enemyName != null)
                _enemyName.Text = isShiny ? $"✨ {enemyName}" : enemyName;

            // Intentar mostrar el sprite del enemigo (se llenará en Fase 4 con el sistema de sprites)
            if (_enemySprite != null)
                _enemySprite.Texture = null; // placeholder hasta tener los sprites

            // Nombre del jugador
            var active = _gm.GetActivePokemon();
            if (_playerName != null && active != null)
                _playerName.Text = active.Name;

            AppendLog($"¡Apareció un {enemyName} salvaje{(isShiny ? " ✨" : "")}!");
            HideRewardPopup();
        }

        /// <summary>
        /// Un turno se completó: actualizar las barras de HP y el log.
        /// </summary>
        private void OnTurnCompleted(int playerHp, int enemyHp, string log)
        {
            // Actualizar HP del enemigo
            if (_enemyHpBar != null)
            {
                _enemyHpBar.MaxValue = _enemyMaxHp;
                _enemyHpBar.Value    = System.Math.Max(0, enemyHp);
                UpdateHpBarColor(_enemyHpBar, enemyHp, _enemyMaxHp);
            }

            // Actualizar HP del jugador
            if (_playerHpBar != null)
            {
                _playerHpBar.MaxValue = _playerMaxHp;
                _playerHpBar.Value    = System.Math.Max(0, playerHp);
                UpdateHpBarColor(_playerHpBar, playerHp, _playerMaxHp);
            }

            AppendLog(log);
        }

        /// <summary>
        /// La batalla terminó: mostrar popup de recompensas o derrota.
        /// </summary>
        private void OnBattleEnded(bool playerWon, int xpGained, int goldGained)
        {
            if (playerWon)
            {
                ShowRewardPopup($"+{xpGained} XP   +{goldGained} 💰");
                AppendLog($"¡Victoria! +{xpGained} XP, +{goldGained} monedas.");
            }
            else
            {
                ShowRewardPopup("¡Derrota!");
                AppendLog("El equipo fue derrotado...");
            }
        }

        /// <summary>
        /// Pokémon capturado exitosamente.
        /// </summary>
        private void OnPokemonCaught(int pokemonId, bool isShiny)
        {
            AppendLog($"¡Pokémon #{pokemonId} capturado{(isShiny ? " ✨" : "")}!");
            ShowRewardPopup("¡Atrapad@!");

            // Pequeña animación de shake (usando Tween, disponible en Fase 4 completa)
            // TODO: Tween de la Pokéball
        }

        /// <summary>
        /// La fase del combate cambió (para feedback visual de estado).
        /// </summary>
        private void OnPhaseChanged(string phase)
        {
            if (_phaseIndicator != null)
                _phaseIndicator.Text = phase.ToUpper();

            // Habilitar/deshabilitar el botón de Pokéball según la fase
            if (_btnBall != null)
                _btnBall.Disabled = phase != "PlayerTurn";
        }

        /// <summary>
        /// Un Pokémon del equipo subió de nivel.
        /// </summary>
        private void OnLevelUp(int teamIndex, int newLevel)
        {
            var pokemon = teamIndex < _gm.Team.Count ? _gm.Team[teamIndex] : null;
            string name = pokemon?.Name ?? $"Pokémon [{teamIndex}]";
            AppendLog($"¡{name} subió al nivel {newLevel}!");
        }

        // ----------------------------------------------------------------
        // Helpers de UI
        // ----------------------------------------------------------------

        /// <summary>
        /// Registra HP máximos para calcular los porcentajes de barra.
        /// Llamado internamente cuando comienza el combate.
        /// En Fase 4, se usará cuando el BattleSystem exponga el estado completo.
        /// </summary>
        public void SetMaxHp(int playerMax, int enemyMax)
        {
            _playerMaxHp = System.Math.Max(1, playerMax);
            _enemyMaxHp  = System.Math.Max(1, enemyMax);
        }

        private void AppendLog(string text)
        {
            if (_battleLog == null) return;
            _battleLog.AppendText($"\n{text}");

            // Auto-scroll al final
            _battleLog.ScrollToLine(_battleLog.GetLineCount());
        }

        private void ShowRewardPopup(string text)
        {
            if (_rewardPopup == null) return;
            if (_rewardText != null) _rewardText.Text = text;
            _rewardPopup.Visible = true;

            // Auto-ocultar después de 2 segundos
            GetTree().CreateTimer(2.0f).Timeout += HideRewardPopup;
        }

        private void HideRewardPopup()
        {
            if (_rewardPopup != null)
                _rewardPopup.Visible = false;
        }

        /// <summary>
        /// Cambia el color de la barra de HP según el porcentaje restante:
        /// Verde → Amarillo → Rojo (como en los juegos oficiales).
        /// </summary>
        private void UpdateHpBarColor(ProgressBar bar, int current, int max)
        {
            float ratio = max > 0 ? (float)current / max : 0f;
            Color color = ratio switch
            {
                > 0.5f => new Color(0.2f, 0.8f, 0.2f),  // Verde
                > 0.2f => new Color(0.9f, 0.8f, 0.1f),  // Amarillo
                _      => new Color(0.9f, 0.2f, 0.1f),  // Rojo crítico
            };

            // Aplicar StyleBoxFlat al fill de la ProgressBar
            var styleBox = new StyleBoxFlat { BgColor = color };
            bar.AddThemeStyleboxOverride("fill", styleBox);
        }

        /// <summary>Helper genérico para obtener nodos por NodePath de forma null-safe.</summary>
        private T GetNodeOrNullByPath<T>(NodePath path) where T : Node
        {
            if (path == null || path.IsEmpty) return null;
            return GetNodeOrNull<T>(path);
        }
    }
}
