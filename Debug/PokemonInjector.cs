using Godot;
using System.Collections.Generic;
using System.Linq;
using PokeIdle.Core.Autoloads;
using PokeIdle.Core.Models;
using PokeIdle.Core.Services;

namespace PokeIdle.Debug
{
    /// <summary>
    /// PokemonInjector — equivalente al "Laboratorio de Clonación" de React.
    ///
    /// Permite buscar cualquier Pokémon de la base de datos y añadirlo al equipo
    /// con nivel, shiny e IVs configurables.
    ///
    /// Se abre desde DebugPanel (tab General → botón "Abrir Inyector").
    /// Se construye por código sin .tscn.
    /// </summary>
    public partial class PokemonInjector : CanvasLayer
    {
        // ----------------------------------------------------------------
        // Referencias
        // ----------------------------------------------------------------
        private GameManager _gm;

        // ----------------------------------------------------------------
        // UI Nodes
        // ----------------------------------------------------------------
        private Control _modal;
        private LineEdit _searchField;
        private VBoxContainer _resultsList;
        private Label _selectedLabel;
        private HSlider _levelSlider;
        private Label _levelValue;
        private CheckButton _shinyToggle;
        private CheckButton _perfectIvsToggle;

        // ----------------------------------------------------------------
        // Estado
        // ----------------------------------------------------------------
        private PokemonData _selectedPokemon = null;
        private List<PokemonData> _searchResults = new();
        private int _level = 50;
        private bool _isShiny = false;
        private bool _perfectIvs = true;

        // ----------------------------------------------------------------
        // Lifecycle
        // ----------------------------------------------------------------

        public override void _Ready()
        {
            Layer = 101; // Por encima del DebugPanel
            _gm = GetNode<GameManager>("/root/GameManager");
            BuildModal();
            _modal.Visible = false;
        }

        /// <summary>
        /// Abre el inyector. Llamado desde DebugPanel.
        /// </summary>
        public void Open()
        {
            _modal.Visible = true;
            _searchField?.GrabFocus();
            LoadAllPokemon();
        }

        // ----------------------------------------------------------------
        // Construcción de UI
        // ----------------------------------------------------------------

        private void BuildModal()
        {
            var viewport = GetViewport().GetVisibleRect().Size;

            var overlay = new ColorRect
            {
                Color = new Color(0, 0, 0, 0.85f),
                Size = viewport,
                Position = Vector2.Zero,
            };

            _modal = new PanelContainer
            {
                CustomMinimumSize = new Vector2(900, viewport.Y * 0.88f),
                Position = new Vector2(viewport.X * 0.05f, viewport.Y * 0.06f),
                Size = new Vector2(viewport.X * 0.9f, viewport.Y * 0.88f),
            };

            var vBox = new VBoxContainer();
            _modal.AddChild(vBox);

            // Header
            var header = new HBoxContainer();
            header.AddChild(new Label { Text = "⚡ LABORATORIO DE CLONACIÓN" });
            header.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }); // spacer
            var closeBtn = new Button { Text = "✕" };
            closeBtn.Pressed += () => _modal.Visible = false;
            header.AddChild(closeBtn);
            vBox.AddChild(header);

            // Body
            var hBox = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
            vBox.AddChild(hBox);

            // Columna izquierda: búsqueda + lista
            hBox.AddChild(BuildSearchPanel());

            // Columna derecha: configuración + acciones
            hBox.AddChild(BuildConfigPanel());

            overlay.AddChild(_modal);
            AddChild(overlay);
        }

        private Control BuildSearchPanel()
        {
            var vBox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };

            // Buscador
            _searchField = new LineEdit
            {
                PlaceholderText = "Buscar por nombre o ID...",
                CustomMinimumSize = new Vector2(0, 36),
            };
            _searchField.TextChanged += OnSearchChanged;
            vBox.AddChild(_searchField);

            // Lista de resultados
            var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
            _resultsList = new VBoxContainer();
            scroll.AddChild(_resultsList);
            vBox.AddChild(scroll);

            return vBox;
        }

        private Control BuildConfigPanel()
        {
            var vBox = new VBoxContainer { CustomMinimumSize = new Vector2(280, 0) };

            // Pokémon seleccionado
            _selectedLabel = new Label { Text = "— Ninguno seleccionado —" };
            vBox.AddChild(_selectedLabel);

            vBox.AddChild(new HSeparator());

            // Nivel
            var levelRow = new HBoxContainer();
            levelRow.AddChild(new Label { Text = "Nivel:", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
            _levelValue = new Label { Text = "50" };
            levelRow.AddChild(_levelValue);
            vBox.AddChild(levelRow);

            _levelSlider = new HSlider { MinValue = 1, MaxValue = 100, Value = 50, Step = 1 };
            _levelSlider.ValueChanged += (val) =>
            {
                _level = (int)val;
                _levelValue.Text = _level.ToString();
            };
            vBox.AddChild(_levelSlider);

            // Shiny toggle
            var shinyRow = new HBoxContainer();
            shinyRow.AddChild(new Label { Text = "✨ Shiny:", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
            _shinyToggle = new CheckButton { ButtonPressed = false };
            _shinyToggle.Toggled += (on) => _isShiny = on;
            shinyRow.AddChild(_shinyToggle);
            vBox.AddChild(shinyRow);

            // IVs perfectos toggle
            var ivsRow = new HBoxContainer();
            ivsRow.AddChild(new Label { Text = "IVs perfectos:", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
            _perfectIvsToggle = new CheckButton { ButtonPressed = true };
            _perfectIvsToggle.Toggled += (on) => _perfectIvs = on;
            ivsRow.AddChild(_perfectIvsToggle);
            vBox.AddChild(ivsRow);

            vBox.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill }); // spacer

            vBox.AddChild(new HSeparator());

            // Botón inyectar
            var injectBtn = new Button { Text = "💉 INYECTAR AL EQUIPO" };
            injectBtn.Pressed += HandleInject;
            vBox.AddChild(injectBtn);

            // Botón desbloquear como inicial (placeholder)
            var starterBtn = new Button { Text = "⭐ CLONAR COMO INICIAL (próximamente)" };
            starterBtn.Disabled = true;
            vBox.AddChild(starterBtn);

            return vBox;
        }

        // ----------------------------------------------------------------
        // Lógica
        // ----------------------------------------------------------------

        private void LoadAllPokemon()
        {
            _searchResults = DatabaseService.GetAllPokemon();
            RenderResults();
        }

        private void OnSearchChanged(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _searchResults = DatabaseService.GetAllPokemon();
            }
            else
            {
                var q = query.ToLower();
                _searchResults = DatabaseService.GetAllPokemon()
                    .Where(p => p.Name.ToLower().Contains(q) || p.Id.ToString() == q)
                    .ToList();
            }
            RenderResults();
        }

        private void RenderResults()
        {
            // Limpiar lista anterior
            foreach (Node child in _resultsList.GetChildren())
                child.QueueFree();

            // Máximo 120 resultados para no sobrecargar
            foreach (var pokemon in _searchResults.Take(120))
            {
                var p = pokemon;
                var btn = new Button
                {
                    Text = $"#{p.Id:000}  {p.Name}",
                    Flat = _selectedPokemon?.Id != p.Id,
                };
                btn.Pressed += () =>
                {
                    _selectedPokemon = p;
                    _selectedLabel.Text = $"✅ {p.Name}  (#{ p.Id})";
                    RenderResults(); // re-render para marcar el seleccionado
                };
                _resultsList.AddChild(btn);
            }
        }

        private void HandleInject()
        {
            if (_selectedPokemon == null)
            {
                GD.Print("[PokemonInjector] No hay Pokémon seleccionado.");
                return;
            }

            if (_gm.Team.Count >= 6)
            {
                GD.Print("[PokemonInjector] Equipo lleno.");
                return;
            }

            // Clonar el PokemonData para no mutar el original de la base de datos
            var clone = new PokemonData
            {
                Id           = _selectedPokemon.Id,
                Name         = _selectedPokemon.Name,
                Types        = _selectedPokemon.Types,
                Stats        = _selectedPokemon.Stats,
                Ability      = _selectedPokemon.Ability,
                CatchRate    = _selectedPokemon.CatchRate,
                BaseExpYield = _selectedPokemon.BaseExpYield,
                GrowthRate   = _selectedPokemon.GrowthRate,
                LevelUpMoves = _selectedPokemon.LevelUpMoves,
                SpriteUrl    = _selectedPokemon.SpriteUrl,
                Level        = _level,
                IsShiny      = _isShiny,
            };
            clone.CurrentHp = CombatantState.CalculateMaxHp(clone.Stats.Hp, clone.Level);

            bool added = _gm.AddToTeam(clone);
            if (added)
            {
                GD.Print($"[PokemonInjector] {clone.Name} Nv.{clone.Level} inyectado al equipo.");
                _modal.Visible = false;
            }
        }
    }
}
