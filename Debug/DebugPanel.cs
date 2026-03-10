using Godot;
using System.Collections.Generic;
using System.Linq;
using PokeIdle.Core.Autoloads;
using PokeIdle.Core.Models;
using PokeIdle.Core.Services;

namespace PokeIdle.Debug
{
    /// <summary>
    /// Panel de Debug — solo visible en builds de desarrollo (OS.IsDebugBuild()).
    /// Todo construido por código (sin .tscn) para ser autosuficiente.
    /// Se añade como CanvasLayer hijo de la escena principal.
    ///
    /// Tabs: GENERAL | EQUIPO | ÍTEMS | PROGRESO | MEGA | ESTADO
    /// </summary>
    public partial class DebugPanel : CanvasLayer
    {
        // ----------------------------------------------------------------
        // Señales
        // ----------------------------------------------------------------
        [Signal] public delegate void DebugNotifiedEventHandler(string message);

        // ----------------------------------------------------------------
        // Referencias
        // ----------------------------------------------------------------
        private GameManager _gm;

        // ----------------------------------------------------------------
        // UI Nodes (creados en _Ready)
        // ----------------------------------------------------------------
        private Button   _devButton;      // Botón flotante "DEV"
        private Control  _modal;          // Ventana modal principal
        private Control  _contentArea;    // Área de contenido de la tab activa
        private Label    _statusLabel;    // "RUN: ACTIVE / NULL"

        // ----------------------------------------------------------------
        // Estado local
        // ----------------------------------------------------------------
        private enum Tab { General, Equipo, Items, Progreso, Mega, Estado }
        private Tab _activeTab = Tab.General;

        // Items quicklist para acceso rápido
        private static readonly (string id, string label, int qty)[] QuickItems =
        {
            ("poke-ball",    "Poké Ball",    50),
            ("great-ball",   "Great Ball",   30),
            ("ultra-ball",   "Ultra Ball",   20),
            ("master-ball",  "Master Ball",  5),
            ("rare-candy",   "Rare Candy",   10),
            ("potion",       "Poción",       20),
            ("super-potion", "Súper Poción", 15),
            ("hyper-potion", "Hiper Poción", 10),
            ("full-restore", "Full Restore", 5),
            ("revive",       "Revive",       10),
            ("max-revive",   "Max Revive",   5),
        };

        // ----------------------------------------------------------------
        // Lifecycle
        // ----------------------------------------------------------------

        public override void _Ready()
        {
            // Solo en debug builds
            if (!OS.IsDebugBuild())
            {
                QueueFree();
                return;
            }

            Layer = 100; // Por encima de todo
            _gm = GetNode<GameManager>("/root/GameManager");

            BuildDevButton();
            BuildModal();

            _modal.Visible = false;
            GD.Print("[DebugPanel] Inicializado.");
        }

        // ----------------------------------------------------------------
        // Construcción de UI
        // ----------------------------------------------------------------

        private void BuildDevButton()
        {
            _devButton = new Button
            {
                Text = "⚡\nDEV",
                CustomMinimumSize = new Vector2(56, 56),
                Position = new Vector2(16, GetViewport().GetVisibleRect().Size.Y - 72),
            };
            _devButton.Pressed += () => _modal.Visible = true;
            AddChild(_devButton);
        }

        private void BuildModal()
        {
            var viewport = GetViewport().GetVisibleRect().Size;

            // Fondo oscuro
            var overlay = new ColorRect
            {
                Color = new Color(0, 0, 0, 0.8f),
                Size = viewport,
                Position = Vector2.Zero,
            };
            overlay.GuiInput += (e) => { }; // captura clicks

            // Ventana principal
            _modal = new PanelContainer
            {
                CustomMinimumSize = new Vector2(900, viewport.Y * 0.8f),
                Position = new Vector2(viewport.X * 0.05f, viewport.Y * 0.1f),
                Size = new Vector2(viewport.X * 0.9f, viewport.Y * 0.8f),
            };

            var mainVBox = new VBoxContainer();
            _modal.AddChild(mainVBox);

            // Header
            mainVBox.AddChild(BuildHeader());

            // Body (sidebar + content)
            var hBox = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
            mainVBox.AddChild(hBox);

            hBox.AddChild(BuildSidebar());

            _contentArea = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            hBox.AddChild(_contentArea);

            RefreshContent();

            overlay.AddChild(_modal);
            AddChild(overlay);
        }

        private Control BuildHeader()
        {
            var hBox = new HBoxContainer();

            var title = new Label { Text = "⚡ DEBUGGER v2.5" };
            hBox.AddChild(title);

            hBox.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }); // spacer

            _statusLabel = new Label { Text = "RUN: NULL" };
            hBox.AddChild(_statusLabel);

            var closeBtn = new Button { Text = "✕" };
            closeBtn.Pressed += () => _modal.Visible = false;
            hBox.AddChild(closeBtn);

            return hBox;
        }

        private Control BuildSidebar()
        {
            var vBox = new VBoxContainer
            {
                CustomMinimumSize = new Vector2(160, 0),
            };

            var tabs = new (Tab tab, string icon, string label)[]
            {
                (Tab.General,  "⚡", "GENERAL"),
                (Tab.Equipo,   "⚔️", "EQUIPO"),
                (Tab.Items,    "📦", "ÍTEMS"),
                (Tab.Progreso, "🗺️", "PROGRESO"),
                (Tab.Mega,     "✨", "MEGA"),
                (Tab.Estado,   "💻", "ESTADO"),
            };

            foreach (var (tab, icon, label) in tabs)
            {
                var btn = new Button { Text = $"{icon} {label}", Flat = true };
                btn.Pressed += () =>
                {
                    _activeTab = tab;
                    RefreshContent();
                    UpdateStatus();
                };
                vBox.AddChild(btn);
            }

            return vBox;
        }

        // ----------------------------------------------------------------
        // Contenido dinámico de tabs
        // ----------------------------------------------------------------

        private void RefreshContent()
        {
            // Limpiar contenido anterior
            foreach (Node child in _contentArea.GetChildren())
                child.QueueFree();

            var content = _activeTab switch
            {
                Tab.General  => BuildTabGeneral(),
                Tab.Equipo   => BuildTabEquipo(),
                Tab.Items    => BuildTabItems(),
                Tab.Progreso => BuildTabProgreso(),
                Tab.Mega     => BuildTabMega(),
                Tab.Estado   => BuildTabEstado(),
                _            => new VBoxContainer(),
            };

            _contentArea.AddChild(content);
        }

        // ── Tab: GENERAL ──────────────────────────────────────────────────

        private Control BuildTabGeneral()
        {
            var vBox = new VBoxContainer();

            // Sección: Sesión
            vBox.AddChild(SectionLabel("⬛ SESIÓN"));
            vBox.AddChild(StatRow("Zona",      _gm.CurrentZoneId.IsEmpty() ? "—" : _gm.CurrentZoneId));
            vBox.AddChild(StatRow("Dinero",    $"${_gm.CurrentRun?.Money ?? 0}"));
            vBox.AddChild(StatRow("Equipo",    $"{_gm.Team.Count}/6"));

            // Sección: Economía
            vBox.AddChild(SectionLabel("💰 ECONOMÍA"));
            var goldGrid = new GridContainer { Columns = 4 };
            foreach (int amount in new[] { 100, 1000, 10000, 100000 })
            {
                var n = amount;
                var btn = new Button { Text = $"+{(n >= 1000 ? $"{n/1000}k" : $"{n}")} $" };
                btn.Pressed += () => { _gm.AddGold(n); RefreshContent(); };
                goldGrid.AddChild(btn);
            }
            vBox.AddChild(goldGrid);

            // Sección: Control maestro
            vBox.AddChild(SectionLabel("🎮 CONTROL MAESTRO"));

            var healBtn = new Button { Text = "❤️ Curar equipo" };
            healBtn.Pressed += () =>
            {
                foreach (var p in _gm.Team)
                    p.CurrentHp = CombatantState.CalculateMaxHp(p.Stats.Hp, p.Level);
                Notify("Equipo curado al 100%");
                RefreshContent();
            };
            vBox.AddChild(healBtn);

            var killBattleBtn = new Button { Text = "💥 Terminar batalla" };
            killBattleBtn.Pressed += () => Notify("Batalla terminada (TODO: señal a BattleSystem)");
            vBox.AddChild(killBattleBtn);

            var injectorBtn = new Button { Text = "🧬 Abrir Inyector" };
            injectorBtn.Pressed += OpenInjector;
            vBox.AddChild(injectorBtn);

            return vBox;
        }

        // ── Tab: EQUIPO ───────────────────────────────────────────────────

        private Control BuildTabEquipo()
        {
            var vBox = new VBoxContainer();

            if (_gm.Team.Count == 0)
            {
                vBox.AddChild(new Label { Text = "El equipo está vacío." });
                return vBox;
            }

            foreach (var p in _gm.Team)
            {
                var pokemon = p; // captura para el lambda
                var row = new HBoxContainer();

                var info = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                info.AddChild(new Label { Text = $"{pokemon.Name}  Nv.{pokemon.Level}" });
                info.AddChild(new Label { Text = $"{pokemon.CurrentHp}/{CombatantState.CalculateMaxHp(pokemon.Stats.Hp, pokemon.Level)} HP" });
                row.AddChild(info);

                var plus1 = new Button { Text = "+1 Nv" };
                plus1.Pressed += () =>
                {
                    if (pokemon.Level < 100) { pokemon.Level++; Notify($"{pokemon.Name} → Nv.{pokemon.Level}"); RefreshContent(); }
                };
                row.AddChild(plus1);

                var plus10 = new Button { Text = "+10 Nv" };
                plus10.Pressed += () =>
                {
                    pokemon.Level = System.Math.Min(100, pokemon.Level + 10);
                    Notify($"{pokemon.Name} → Nv.{pokemon.Level}"); RefreshContent();
                };
                row.AddChild(plus10);

                vBox.AddChild(row);
                vBox.AddChild(new HSeparator());
            }

            var candyBtn = new Button { Text = "🍬 Añadir 50 Rare Candies" };
            candyBtn.Pressed += () => { _gm.AddItem("rare-candy", 50); Notify("+50 Rare Candies"); };
            vBox.AddChild(candyBtn);

            return vBox;
        }

        // ── Tab: ÍTEMS ────────────────────────────────────────────────────

        private Control BuildTabItems()
        {
            var vBox = new VBoxContainer();

            vBox.AddChild(SectionLabel("⚡ ACCESO RÁPIDO"));
            var grid = new GridContainer { Columns = 2 };
            foreach (var (id, label, qty) in QuickItems)
            {
                var itemId = id; var itemQty = qty;
                var btn = new Button { Text = $"{label} +{qty}" };
                btn.Pressed += () => { _gm.AddItem(itemId, itemQty); Notify($"+{itemQty} {label}"); };
                grid.AddChild(btn);
            }
            vBox.AddChild(grid);

            vBox.AddChild(SectionLabel("🔍 TODOS LOS ÍTEMS (desde CSV)"));

            // Listar todos los ítems conocidos de la base de datos
            var allItems = DatabaseService.GetAllItems();
            foreach (var item in allItems.Take(80)) // Límite de 80 para rendimiento
            {
                var itemData = item;
                var row = new HBoxContainer();
                row.AddChild(new Label
                {
                    Text = itemData.Name,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                });

                var count = new Label { Text = $"× {_gm.CurrentRun?.Items.GetValueOrDefault(itemData.Id, 0) ?? 0}" };
                row.AddChild(count);

                var btn1 = new Button { Text = "+1" };
                btn1.Pressed += () => { _gm.AddItem(itemData.Id, 1); RefreshContent(); };
                row.AddChild(btn1);

                var btn10 = new Button { Text = "+10" };
                btn10.Pressed += () => { _gm.AddItem(itemData.Id, 10); RefreshContent(); };
                row.AddChild(btn10);

                vBox.AddChild(row);
            }

            return vBox;
        }

        // ── Tab: PROGRESO ─────────────────────────────────────────────────

        private Control BuildTabProgreso()
        {
            var vBox = new VBoxContainer();

            vBox.AddChild(SectionLabel("🏆 TELEPORT A ZONA"));

            var allZones = DatabaseService.GetAllZones();
            if (allZones == null || allZones.Count == 0)
            {
                vBox.AddChild(new Label { Text = "No hay zonas cargadas aún." });
                return vBox;
            }

            foreach (var zone in allZones)
            {
                var z = zone;
                var btn = new Button
                {
                    Text = z.Id == _gm.CurrentZoneId
                        ? $"▶ {z.Name} [ACTUAL]"
                        : z.Name,
                };
                btn.Pressed += () =>
                {
                    _gm.SetZone(z.Id);
                    Notify($"Teleportado a: {z.Name}");
                    RefreshContent();
                };
                vBox.AddChild(btn);
            }

            return vBox;
        }

        // ── Tab: MEGA ─────────────────────────────────────────────────────

        private Control BuildTabMega()
        {
            var vBox = new VBoxContainer();

            vBox.AddChild(SectionLabel("✨ MEGA EVOLUCIÓN"));

            var braceletBtn = new Button { Text = "💎 Toggle Mega Bracelet" };
            braceletBtn.Pressed += () =>
            {
                // Toggle en GameManager (campo a agregar cuando implementemos Mega)
                Notify("Mega Bracelet — pendiente de implementar en GameManager");
            };
            vBox.AddChild(braceletBtn);

            var allStonesBtn = new Button { Text = "💎 Dar todas las Mega Stones" };
            allStonesBtn.Pressed += () =>
            {
                string[] stones = {
                    "venusaurite","charizardite-x","charizardite-y","blastoisite",
                    "alakazite","gengarite","kangaskhanite","lucarionite","garchompite",
                    "gardevoirite","blazikenite","salamencite","metagrossite","beedrillite"
                };
                foreach (var s in stones) _gm.AddItem(s, 1);
                Notify("Todas las Mega Stones añadidas");
            };
            vBox.AddChild(allStonesBtn);

            var resetBtn = new Button { Text = "🔄 Reset MegaState" };
            resetBtn.Pressed += () => Notify("MegaState — pendiente de implementar");
            vBox.AddChild(resetBtn);

            return vBox;
        }

        // ── Tab: ESTADO ───────────────────────────────────────────────────

        private Control BuildTabEstado()
        {
            var vBox = new VBoxContainer();

            vBox.AddChild(SectionLabel("💻 SYSTEM INSPECTOR"));

            // Dump del GameManager como texto
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== GameManager ===");
            sb.AppendLine($"ZonaActiva:     {_gm.CurrentZoneId}");
            sb.AppendLine($"Dinero:         {_gm.CurrentRun?.Money ?? 0}");
            sb.AppendLine($"Equipo:         {_gm.Team.Count}/6");
            sb.AppendLine($"TotalRuns:      {_gm.TotalRunsPlayed}");
            sb.AppendLine();
            sb.AppendLine("=== Equipo ===");
            foreach (var p in _gm.Team)
                sb.AppendLine($"  {p.Name} Nv.{p.Level} — {p.CurrentHp} HP");
            sb.AppendLine();
            sb.AppendLine("=== Inventario ===");
            if (_gm.CurrentRun?.Items != null)
                foreach (var kvp in _gm.CurrentRun.Items)
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value}");

            var inspector = new TextEdit
            {
                Text = sb.ToString(),
                Editable = false,
                CustomMinimumSize = new Vector2(0, 300),
            };
            vBox.AddChild(inspector);

            var clearBtn = new Button { Text = "🧹 Refrescar" };
            clearBtn.Pressed += RefreshContent;
            vBox.AddChild(clearBtn);

            return vBox;
        }

        // ----------------------------------------------------------------
        // Inyector de Pokémon
        // ----------------------------------------------------------------

        private void OpenInjector()
        {
            var injector = GetNodeOrNull<PokemonInjector>("PokemonInjector");
            if (injector == null)
            {
                injector = new PokemonInjector();
                injector.Name = "PokemonInjector";
                AddChild(injector);
            }
            injector.Open();
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        private void UpdateStatus()
        {
            if (_statusLabel == null) return;
            _statusLabel.Text = _gm.CurrentRun != null ? "RUN: ACTIVE" : "RUN: NULL";
        }

        private void Notify(string msg)
        {
            GD.Print($"[DebugPanel] {msg}");
            EmitSignal(SignalName.DebugNotified, msg);
        }

        private static Label SectionLabel(string text) => new Label
        {
            Text = text,
        };

        private static HBoxContainer StatRow(string label, string value)
        {
            var row = new HBoxContainer();
            row.AddChild(new Label { Text = label, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
            row.AddChild(new Label { Text = value });
            return row;
        }
    }
}
