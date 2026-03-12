using Godot;
using System.Collections.Generic;
using PokeIdle.Core.Models;
using PokeIdle.Core.Autoloads;

namespace PokeIdle.UI
{
    /// <summary>
    /// TeamUI: Panel visual del equipo del jugador.
    ///
    /// Escucha las señales del GameManager (TeamUpdated, LevelUp, ZoneChanged)
    /// y redibuja el panel del equipo cuando hay cambios.
    ///
    /// Estructura esperada en la escena:
    ///
    ///   TeamPanel (Control, este script)
    ///     ├── TeamGrid        (GridContainer — 2 columnas, 3 filas = 6 slots)
    ///     │     ├── Slot0     (PanelContainer)
    ///     │     ├── Slot1
    ///     │     └── ...
    ///     ├── ZoneLabel       (Label — nombre de la zona actual)
    ///     └── MoneyLabel      (Label — dinero de la run)
    /// </summary>
    public partial class TeamUI : Control
    {
        // ----------------------------------------------------------------
        // Node paths (asignados en el Inspector de Godot)
        // ----------------------------------------------------------------
        [Export] public NodePath TeamGridPath;
        [Export] public NodePath ZoneLabelPath;
        [Export] public NodePath MoneyLabelPath;

        // ----------------------------------------------------------------
        // Nodos internos
        // ----------------------------------------------------------------
        private GridContainer _teamGrid;
        private Label         _zoneLabel;
        private Label         _moneyLabel;

        // ----------------------------------------------------------------
        // Referencias
        // ----------------------------------------------------------------
        private GameManager _gm;

        // Caché de los 6 slots para actualizarlos sin recrearlos cada vez
        private readonly List<TeamSlot> _slots = new();

        // ----------------------------------------------------------------
        // Lifecycle
        // ----------------------------------------------------------------

        public override void _Ready()
        {
            _gm = GetNode<GameManager>("/root/GameManager");

            _teamGrid  = GetNodeOrNullByPath<GridContainer>(TeamGridPath);
            _zoneLabel = GetNodeOrNullByPath<Label>(ZoneLabelPath);
            _moneyLabel = GetNodeOrNullByPath<Label>(MoneyLabelPath);

            // Conectar señales del GameManager
            _gm.TeamUpdated    += OnTeamUpdated;
            _gm.LevelUp        += OnLevelUp;
            _gm.ZoneChanged    += OnZoneChanged;
            _gm.RunMoneyChanged += OnMoneyChanged;

            // Construir los 6 slots vacíos al inicio
            BuildSlots();

            // Renderizar el estado actual
            Refresh();

            GD.Print("[TeamUI] Listo y conectado al GameManager.");
        }

        // ----------------------------------------------------------------
        // Handlers de señales
        // ----------------------------------------------------------------

        private void OnTeamUpdated()
        {
            Refresh();
        }

        private void OnLevelUp(int teamIndex, int newLevel)
        {
            // Actualizar solo el slot afectado (más eficiente que Refresh completo)
            if (teamIndex >= 0 && teamIndex < _slots.Count)
            {
                var pokemon = teamIndex < _gm.Team.Count ? _gm.Team[teamIndex] : null;
                _slots[teamIndex].Update(pokemon, highlight: true);
            }
        }

        private void OnZoneChanged(string zoneId)
        {
            if (_zoneLabel != null)
                _zoneLabel.Text = $"🗺️ {zoneId}";
        }

        private void OnMoneyChanged(int newAmount)
        {
            if (_moneyLabel != null)
                _moneyLabel.Text = $"💰 {newAmount}";
        }

        // ----------------------------------------------------------------
        // Construcción de slots
        // ----------------------------------------------------------------

        /// <summary>
        /// Crea los 6 contenedores de slot una sola vez. Después solo se actualizan.
        /// </summary>
        private void BuildSlots()
        {
            if (_teamGrid == null) return;

            _teamGrid.Columns = 2;
            _slots.Clear();

            for (int i = 0; i < 6; i++)
            {
                var slot = new TeamSlot();
                slot.Build();
                _teamGrid.AddChild(slot.Root);
                _slots.Add(slot);
            }
        }

        /// <summary>
        /// Refresca todos los slots con el estado actual del equipo.
        /// </summary>
        private void Refresh()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var pokemon = i < _gm.Team.Count ? _gm.Team[i] : null;
                _slots[i].Update(pokemon);
            }

            // Actualizar dinero y zona también
            if (_moneyLabel != null)
                _moneyLabel.Text = $"💰 {_gm.CurrentRun?.Money ?? 0}";

            if (_zoneLabel != null)
                _zoneLabel.Text = $"🗺️ {(string.IsNullOrEmpty(_gm.CurrentZoneId) ? "—" : _gm.CurrentZoneId)}";
        }

        // ----------------------------------------------------------------
        // Helper
        // ----------------------------------------------------------------

        private T GetNodeOrNullByPath<T>(NodePath path) where T : Node
        {
            if (path == null || path.IsEmpty) return null;
            return GetNodeOrNull<T>(path);
        }

        // ----------------------------------------------------------------
        // TeamSlot — representación de UN Pokémon en el panel
        // ----------------------------------------------------------------

        /// <summary>
        /// Encapsula todos los nodos visuales de un slot del equipo.
        /// Se construye una vez y se actualiza con Update().
        /// </summary>
        private class TeamSlot
        {
            public Control Root { get; private set; }

            private Label       _name;
            private Label       _level;
            private ProgressBar _hpBar;
            private Label       _hpText;
            private Label       _status;

            // Tween reutilizable para la animación de level-up
            private Tween _highlightTween;

            public void Build()
            {
                // Contenedor raíz del slot
                var panel = new PanelContainer { CustomMinimumSize = new Vector2(160, 60) };
                Root = panel;

                var vBox = new VBoxContainer();
                panel.AddChild(vBox);

                // Fila superior: nombre + nivel
                var topRow = new HBoxContainer();
                _name  = new Label { Text = "—", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                _level = new Label { Text = "" };
                topRow.AddChild(_name);
                topRow.AddChild(_level);
                vBox.AddChild(topRow);

                // Barra de HP
                _hpBar = new ProgressBar
                {
                    MinValue = 0,
                    MaxValue = 100,
                    Value    = 0,
                    ShowPercentage = false,
                    CustomMinimumSize = new Vector2(0, 8),
                };
                vBox.AddChild(_hpBar);

                // Texto de HP + status
                var bottomRow = new HBoxContainer();
                _hpText = new Label { Text = "", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                _status = new Label { Text = "" };
                bottomRow.AddChild(_hpText);
                bottomRow.AddChild(_status);
                vBox.AddChild(bottomRow);
            }

            /// <summary>
            /// Actualiza el slot con los datos del Pokémon dado (null = slot vacío).
            /// </summary>
            public void Update(PokemonData pokemon, bool highlight = false)
            {
                if (pokemon == null)
                {
                    // Slot vacío
                    _name.Text  = "—";
                    _level.Text = "";
                    _hpBar.Value = 0;
                    _hpText.Text = "";
                    _status.Text = "";
                    Root.Modulate = new Color(1, 1, 1, 0.3f); // Atenuado
                    return;
                }

                Root.Modulate = Colors.White;

                int maxHp = CombatantState.CalculateMaxHp(pokemon.Stats.Hp, pokemon.Level);
                float hpRatio = maxHp > 0 ? (float)pokemon.CurrentHp / maxHp : 0f;

                _name.Text  = pokemon.Name + (pokemon.IsShiny ? " ✨" : "");
                _level.Text = $"Nv.{pokemon.Level}";
                _hpBar.MaxValue = maxHp;
                _hpBar.Value    = pokemon.CurrentHp;
                _hpText.Text    = $"{pokemon.CurrentHp}/{maxHp}";
                _status.Text    = pokemon.CurrentHp <= 0 ? "PSN" : ""; // placeholder

                // Color de la barra de HP
                Color barColor = hpRatio switch
                {
                    > 0.5f => new Color(0.2f, 0.8f, 0.2f),
                    > 0.2f => new Color(0.9f, 0.8f, 0.1f),
                    _      => new Color(0.9f, 0.2f, 0.1f),
                };
                _hpBar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = barColor });

                // Animación de flash amarillo en level-up
                if (highlight)
                    AnimateLevelUp();
            }

            private void AnimateLevelUp()
            {
                // Necesita un SceneTree — solo si el nodo está en el árbol
                if (!Root.IsInsideTree()) return;

                _highlightTween?.Kill();
                _highlightTween = Root.CreateTween();
                _highlightTween
                    .TweenProperty(Root, "modulate", new Color(1f, 0.95f, 0.3f, 1f), 0.15f)
                    .SetTrans(Tween.TransitionType.Quad);
                _highlightTween
                    .TweenProperty(Root, "modulate", Colors.White, 0.4f)
                    .SetTrans(Tween.TransitionType.Quad);
            }
        }
    }
}
