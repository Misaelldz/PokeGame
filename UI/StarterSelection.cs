using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using PokeIdle.Core.Models;
using PokeIdle.Core.Services;
using PokeIdle.Core.Autoloads;
using Godot.Collections; // For JSON parsing

namespace PokeIdle.UI
{
	public partial class StarterSelection : Control
	{
		[Export] private NodePath GridContainerPath;
		[Export] private NodePath InfoPanelPath;
		[Export] private NodePath PokemonNameLabelPath;
		[Export] private NodePath PokemonTypeLabelPath;
		[Export] private NodePath PokemonStatsLabelPath;
		[Export] private NodePath PokemonSpritePath;
		[Export] private NodePath ConfirmButtonPath;
		[Export] private NodePath AbilityLabelPath;
		[Export] private NodePath PassiveLabelPath;
		[Export] private NodePath NatureLabelPath;
		[Export] private NodePath GrowthRateLabelPath;
		[Export] private NodePath GenderIconPath;
		[Export] private NodePath MovesContainerPath;
		[Export] private NodePath EggMovesContainerPath;
		[Export] private NodePath FormContainerPath;
		[Export] private NodePath ShinyTogglePath;

		private GridContainer _gridContainer;
		private Control _infoPanel;
		private Label _nameLabel;
		private Label _typeLabel;
		private Label _statsLabel;
		private TextureRect _spriteRect;
		private Button _confirmButton;
		private Label _idLabel;
		private Label _abilityLabel;
		private Label _passiveLabel;
		private Label _natureLabel;
		private RichTextLabel _growthLabel;
		private Label _genderLabel;
		private Control _movesContainer;
		private Control _eggMovesContainer;
		private Control _formContainer;
		private Button _shinyToggle;
		private Control _selectionCursor; // The [ ] bracket

		private bool _isShiny = false;
		private string _currentForm = ""; // "", "-mega", "-gigantamax"

		private Shader _silhouetteShader;
		private FontFile _pixelFont;
		
		private AtlasTexture _animatedSprite;
		private List<Rect2> _frames = new();
		private int _currentFrame = 0;
		private float _frameTimer = 0;
		private float _timePerFrame = 0.12f; 
		private bool _isHovering = false;
		private int _lastPokemonId = -1;
		
		public enum PokemonCategory
		{
			RegionalStarters,
			Everything,
			FirstEvolution,
			SingleStage,
			Fossils,
			PseudoLegendary,
			Legendary,
			Mythical,
			Paradox
		}

		private PokemonCategory _currentCategory = PokemonCategory.RegionalStarters;

		private static readonly HashSet<int> FossilIds = new() { 
			138, 139, 140, 141, 142, // Gen 1
			345, 346, 347, 348, // Gen 3
			408, 409, 410, 411, // Gen 4
			564, 565, 566, 567, // Gen 5
			696, 697, 698, 699, // Gen 6
			880, 881, 882, 883, // Gen 8
			950 // Gen 9 (Klawf isn't one but close enough? No, let's keep it strict)
		};

		private static readonly HashSet<int> PseudoLegendaryIds = new() {
			147, 148, 149, // Dragonite
			246, 247, 248, // Tyranitar
			371, 372, 373, // Salamence
			374, 375, 376, // Metagross
			443, 444, 445, // Garchomp
			633, 634, 635, // Hydreigon
			704, 705, 706, // Goodra
			782, 783, 784, // Kommo-o
			885, 886, 887, // Dragapult
			996, 997, 998  // Baxcalibur
		};

		private int _currentSelectionId = -1;
		private HashSet<int> _unlockedIds = new();

		public override void _Ready()
		{
			// Initialize UI references
			if (GridContainerPath != null) _gridContainer = GetNode<GridContainer>(GridContainerPath);
			if (InfoPanelPath != null) _infoPanel = GetNode<Control>(InfoPanelPath);
			if (PokemonNameLabelPath != null) _nameLabel = GetNode<Label>(PokemonNameLabelPath);
			if (PokemonTypeLabelPath != null) _typeLabel = GetNode<Label>(PokemonTypeLabelPath);
			if (PokemonStatsLabelPath != null) _statsLabel = GetNode<Label>(PokemonStatsLabelPath);
			if (PokemonSpritePath != null) _spriteRect = GetNode<TextureRect>(PokemonSpritePath);
			if (ConfirmButtonPath != null) _confirmButton = GetNode<Button>(ConfirmButtonPath);
			
			if (AbilityLabelPath != null) _abilityLabel = GetNode<Label>(AbilityLabelPath);
			if (PassiveLabelPath != null) _passiveLabel = GetNode<Label>(PassiveLabelPath);
			if (NatureLabelPath != null) _natureLabel = GetNode<Label>(NatureLabelPath);
			if (GrowthRateLabelPath != null) _growthLabel = GetNode<RichTextLabel>(GrowthRateLabelPath);
			if (GenderIconPath != null) _genderLabel = GetNode<Label>(GenderIconPath);
			if (MovesContainerPath != null) _movesContainer = GetNode<Control>(MovesContainerPath);
			if (EggMovesContainerPath != null) _eggMovesContainer = GetNode<Control>(EggMovesContainerPath);
			if (FormContainerPath != null) _formContainer = GetNode<Control>(FormContainerPath);
			if (ShinyTogglePath != null) 
			{
				_shinyToggle = GetNode<Button>(ShinyTogglePath);
				_shinyToggle.Pressed += ToggleShiny;
			}

			_idLabel = _infoPanel?.GetNode<Label>("VBox/HeaderBar/HeaderHBox/lblID");
			
			// Apply stripes shader
			var stripes = _infoPanel?.GetNode<TextureRect>("VBox/MiddleHBox/CenterSprite/StripedBG/Stripes");
			if (stripes != null)
			{
				var stripesShader = GD.Load<Shader>("res://Assets/Shaders/stripes.gdshader");
				var stripesMat = new ShaderMaterial { Shader = stripesShader };
				stripesMat.SetShaderParameter("stripe_color", new Color(0, 0, 0, 0.05f));
				stripesMat.SetShaderParameter("stripe_thickness", 2.0f);
				stripesMat.SetShaderParameter("stripe_spacing", 2.0f);
				stripes.Material = stripesMat;
			}

			if (_confirmButton != null) _confirmButton.Disabled = true;

			// Load silhouette shader and font
			_silhouetteShader = GD.Load<Shader>("res://Assets/Shaders/silhouette.gdshader");
			_pixelFont = GD.Load<FontFile>("res://Assets/Fonts/fonts/pokemon-emerald-pro.ttf");

			LoadUnlockedStarters();
			PopulateGrid();

			// Hook up category buttons
			var topBar = _infoPanel?.GetParent()?.GetNodeOrNull<Control>("CenterPanel/TopBar/HBox");
			if (topBar != null)
			{
				foreach (var btn in topBar.GetChildren().OfType<Button>())
				{
					switch (btn.Name)
					{
						case "BtnGen": btn.Pressed += () => SetCategory(PokemonCategory.Everything); break;
						case "BtnCaught": btn.Pressed += () => SetCategory(PokemonCategory.FirstEvolution); break;
						case "BtnUnlocks": btn.Pressed += () => SetCategory(PokemonCategory.SingleStage); break;
						case "BtnMisc": btn.Pressed += () => SetCategory(PokemonCategory.Fossils); break;
						case "BtnLegendary": btn.Pressed += () => SetCategory(PokemonCategory.Legendary); break;
						case "BtnMythical": btn.Pressed += () => SetCategory(PokemonCategory.Mythical); break;
						case "BtnParadox": btn.Pressed += () => SetCategory(PokemonCategory.Paradox); break;
					}
				}
			}

			// Setup animated sprite
			_animatedSprite = new AtlasTexture();
			if (_spriteRect != null) 
			{
				_spriteRect.Texture = _animatedSprite;
				_spriteRect.MouseEntered += () => { _isHovering = true; _frameTimer = 0; };
				_spriteRect.MouseExited += () => { _isHovering = false; _currentFrame = 0; UpdateSpriteRegion(); };
			}

			// Default selection
			OnPokemonHovered(1);

			// Setup Selection Cursor (Bracket)
			CreateSelectionCursor();
		}

		private void CreateSelectionCursor()
		{
			_selectionCursor = new Control();
			_selectionCursor.Name = "SelectionCursor";
			_selectionCursor.MouseFilter = Control.MouseFilterEnum.Ignore;
			_selectionCursor.ZIndex = 1;

			// Create 4 corner brackets (simplified for now with small ColorRects)
			// In a real pro setup, this would be a TextureRect with a 9patch
			Color bracketColor = new Color(0, 0.8f, 1.0f); // Bright blue
			float size = 8;
			float thickness = 2;

			// Top Left
			AddBracketCorner(_selectionCursor, new Vector2(0, 0), new Vector2(size, thickness), bracketColor, Control.LayoutPreset.TopLeft);
			AddBracketCorner(_selectionCursor, new Vector2(0, 0), new Vector2(thickness, size), bracketColor, Control.LayoutPreset.TopLeft);
			// Top Right
			AddBracketCorner(_selectionCursor, new Vector2(-size, 0), new Vector2(size, thickness), bracketColor, Control.LayoutPreset.TopRight);
			AddBracketCorner(_selectionCursor, new Vector2(-thickness, 0), new Vector2(thickness, size), bracketColor, Control.LayoutPreset.TopRight);
			// Bottom Left
			AddBracketCorner(_selectionCursor, new Vector2(0, -thickness), new Vector2(size, thickness), bracketColor, Control.LayoutPreset.BottomLeft);
			AddBracketCorner(_selectionCursor, new Vector2(0, -size), new Vector2(thickness, size), bracketColor, Control.LayoutPreset.BottomLeft);
			// Bottom Right
			AddBracketCorner(_selectionCursor, new Vector2(-size, -thickness), new Vector2(size, thickness), bracketColor, Control.LayoutPreset.BottomRight);
			AddBracketCorner(_selectionCursor, new Vector2(-thickness, -size), new Vector2(thickness, size), bracketColor, Control.LayoutPreset.BottomRight);

			_gridContainer.AddChild(_selectionCursor);
			_selectionCursor.Visible = false;
		}

		private void AddBracketCorner(Control parent, Vector2 pos, Vector2 size, Color color, Control.LayoutPreset anchor)
		{
			var rect = new ColorRect { 
				Color = color, 
				CustomMinimumSize = size, 
				Size = size, 
				Position = pos, 
				MouseFilter = Control.MouseFilterEnum.Ignore,
				LayoutMode = 1 // Anchors
			};
			parent.AddChild(rect);
			rect.SetAnchorsAndOffsetsPreset(anchor);
			rect.Position += pos; // Apply offset relative to anchor
		}

		public override void _Process(double delta)
		{
			if (!_isHovering || _frames.Count <= 1) return;
 
			_frameTimer += (float)delta;
			if (_frameTimer >= _timePerFrame)
			{
				_frameTimer = 0;
				_currentFrame = (_currentFrame + 1) % _frames.Count;
				UpdateSpriteRegion();
			}
		}

		private void UpdateSpriteRegion()
		{
			if (_animatedSprite == null || _frames.Count == 0) return;
			
			if (_currentFrame < _frames.Count)
			{
				_animatedSprite.Region = _frames[_currentFrame];
			}
		}

		private void LoadJsonFrames(int pokemonId, string form = "")
		{
			_frames.Clear();
			string jsonPath = $"res://Assets/Sprites/Pokemon/Front/{pokemonId}{form}.json";
			
			if (!FileAccess.FileExists(jsonPath))
			{
				GD.PrintErr($"[StarterSelection] JSON not found: {jsonPath}");
				return;
			}

			using var file = FileAccess.Open(jsonPath, FileAccess.ModeFlags.Read);
			string jsonText = file.GetAsText();
			
			var json = new Json();
			var error = json.Parse(jsonText);
			if (error != Error.Ok)
			{
				GD.PrintErr($"[StarterSelection] Error parsing JSON {jsonPath}: {json.GetErrorMessage()}");
				return;
			}

			var data = json.Data.AsGodotDictionary();
			if (data.ContainsKey("textures"))
			{
				var textures = data["textures"].AsGodotArray();
				if (textures.Count > 0)
				{
					var framesArray = textures[0].AsGodotDictionary()["frames"].AsGodotArray();
					foreach (var frameVar in framesArray)
					{
						var frameDict = frameVar.AsGodotDictionary();
						var rectDict = frameDict["frame"].AsGodotDictionary();
						
						float x = (float)rectDict["x"];
						float y = (float)rectDict["y"];
						float w = (float)rectDict["w"];
						float h = (float)rectDict["h"];
						
						_frames.Add(new Rect2(x, y, w, h));
					}
				}
			}
			
			GD.Print($"[StarterSelection] Loaded {_frames.Count} frames for #{pokemonId}{form}");
		}


		private void LoadUnlockedStarters()
		{
			// Unlock all Pokémon for now as requested
			_unlockedIds = new HashSet<int>(DatabaseService.Instance.Species.Keys);
			GD.Print($"[StarterSelection] Unlocked {_unlockedIds.Count} Pokémon.");
		}

		private void PopulateGrid()
		{
			if (_gridContainer == null) return;

			foreach (Node child in _gridContainer.GetChildren())
				child.QueueFree();

			var allSpecies = DatabaseService.Instance.Species.Values;
			var filtered = allSpecies.Where(s => IsInCategory(s)).OrderBy(s => s.PokemonId);

			foreach (var species in filtered)
			{
				int pokemonId = species.PokemonId;
				var pokemonData = DatabaseService.GetPokemonById(pokemonId);
				if (pokemonData == null) continue;

				var itemContainer = new Control();
				itemContainer.CustomMinimumSize = new Vector2(64, 64); // Slightly smaller to ensure 9 fit perfectly
				itemContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
				
				// Cost Label (Top Left)
				var costLabel = new Label();
				costLabel.Text = species.GachaTier.ToString();
				costLabel.AddThemeFontSizeOverride("font_size", 12);
				costLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.5f)); 
				costLabel.Position = new Vector2(4, 2);
				itemContainer.AddChild(costLabel);

				var btn = new Button();
				btn.Size = new Vector2(40, 40); // Smaller icon size as requested
				btn.Position = new Vector2(12, 16); // Centered with room for cost
				btn.Flat = true;
				btn.IconAlignment = HorizontalAlignment.Center;
				btn.VerticalIconAlignment = VerticalAlignment.Center;
				btn.ExpandIcon = true;
				
				// Use professional high-quality PC icon from SpriteLoader
				btn.Icon = SpriteLoader.GetPokemonIconAtlas(pokemonId);

				bool isLocked = !_unlockedIds.Contains(pokemonId);
				if (isLocked)
				{
					var mat = new ShaderMaterial { Shader = _silhouetteShader };
					mat.SetShaderParameter("is_locked", true);
					btn.Material = mat;
				}

				btn.MouseEntered += () => { 
					OnPokemonHovered(pokemonId);
					UpdateSelectionCursor(itemContainer);
				};
				btn.FocusEntered += () => { 
					OnPokemonHovered(pokemonId);
					UpdateSelectionCursor(itemContainer);
				};
				btn.Pressed += () => OnPokemonSelected(pokemonId);

				itemContainer.AddChild(btn);
				_gridContainer.AddChild(itemContainer);
			}
		}

		private void UpdateSelectionCursor(Control target)
		{
			if (_selectionCursor == null) return;
			_selectionCursor.Visible = true;
			_selectionCursor.Position = target.GlobalPosition - _gridContainer.GlobalPosition;
			_selectionCursor.Size = target.Size;
		}

		private bool IsInCategory(SpeciesData s)
		{
			return _currentCategory switch
			{
				PokemonCategory.Everything => true,
				PokemonCategory.RegionalStarters => (new[] { 
					1, 4, 7, // Gen 1
					152, 155, 158, // Gen 2
					252, 255, 258, // Gen 3
					387, 390, 393, // Gen 4
					495, 498, 501, // Gen 5
					650, 653, 656, // Gen 6
					722, 725, 728, // Gen 7
					810, 813, 816, // Gen 8
					906, 909, 912  // Gen 9
				}).Contains(s.PokemonId),
				PokemonCategory.FirstEvolution => s.EvolvesFromId == null,
				PokemonCategory.SingleStage => s.EvolvesFromId == null && !DatabaseService.Instance.Evolutions.Any(e => e.FromPokemonId == s.PokemonId),
				PokemonCategory.Fossils => FossilIds.Contains(s.PokemonId),
				PokemonCategory.PseudoLegendary => PseudoLegendaryIds.Contains(s.PokemonId),
				PokemonCategory.Legendary => s.IsLegendary,
				PokemonCategory.Mythical => s.IsMythical,
				PokemonCategory.Paradox => (s.PokemonId >= 984 && s.PokemonId <= 1010) || (s.PokemonId >= 1020 && s.PokemonId <= 1025),
				_ => false
			};
		}

		private void OpenCategoryMenu()
		{
			var menu = new PopupMenu();
			foreach (var cat in Enum.GetValues<PokemonCategory>())
			{
				menu.AddItem(cat.ToString());
			}
			
			menu.IndexPressed += (index) => {
				_currentCategory = (PokemonCategory)index;
				PopulateGrid();
			};
			
			AddChild(menu);
			menu.Position = (Vector2I)GetGlobalMousePosition();
			menu.Show();
		}

		private void OnPokemonHovered(int pokemonId)
		{
			if (_lastPokemonId != pokemonId)
			{
				_currentForm = ""; // Reset form on hover new pokemon
				UpdateDetailPanel(pokemonId);
			}
		}

		private void ToggleShiny()
		{
			_isShiny = !_isShiny;
			if (_shinyToggle != null) 
				_shinyToggle.Text = _isShiny ? "Shiny: ON" : "Shiny: OFF";
			UpdateDetailPanel(_lastPokemonId);
		}

		private void SetForm(string form)
		{
			_currentForm = form;
			UpdateDetailPanel(_lastPokemonId);
		}

		private void OnPokemonSelected(int pokemonId)
		{
			if (!_unlockedIds.Contains(pokemonId)) return;

			_currentSelectionId = pokemonId;
			UpdateDetailPanel(pokemonId);
			
			if (_confirmButton != null)
			{
				_confirmButton.Disabled = false;
				_confirmButton.Text = $"1/10\nStart"; 
			}
		}

		private void UpdateDetailPanel(int pokemonId)
		{
			var data = DatabaseService.GetPokemonById(pokemonId);
			if (data == null) return;

			bool isLocked = !_unlockedIds.Contains(pokemonId);

			if (_idLabel != null) _idLabel.Text = $"No. {pokemonId:D4}";
			if (_nameLabel != null) _nameLabel.Text = isLocked ? "???" : data.Name;
			
			if (_typeLabel != null) _typeLabel.Text = isLocked ? "???" : string.Join(" / ", data.Types).ToUpper();
			
			if (_growthLabel != null) 
			{
				string color = data.GrowthRate switch {
					"Fast" => "#8db74a",
					"Medium Fast" => "#4ab78d",
					"Medium Slow" => "#4a8db7",
					"Slow" => "#b74a4a",
					_ => "#ffffff"
				};
				_growthLabel.BbcodeEnabled = true;
				_growthLabel.Text = isLocked ? "Growth Rate: [color=#888]???[/color]" : $"Growth Rate: [color={color}]{data.GrowthRate}[/color]";
			}
			if (_abilityLabel != null) _abilityLabel.Text = $"Ability: {(isLocked ? "???" : data.Ability)}";
			if (_passiveLabel != null) _passiveLabel.Text = $"Passive: {(isLocked ? "???" : "Grassy Surge")}"; // Static for now
			if (_natureLabel != null) _natureLabel.Text = $"Nature: Docile (-)";
			if (_genderLabel != null) _genderLabel.Text = isLocked ? "" : "♂"; // Default male for now
			
			// Populate Moves (simplified)
			if (_movesContainer != null)
			{
				foreach (Node child in _movesContainer.GetChildren()) child.QueueFree();
				if (!isLocked)
				{
					// Mock moves for now
					AddMoveSlot(_movesContainer, "Tackle", new Color(0.7f, 0.7f, 0.6f)); // Normal
					AddMoveSlot(_movesContainer, "Growl", new Color(0.7f, 0.7f, 0.6f));  // Normal
					AddMoveSlot(_movesContainer, "Vine Whip", new Color(0.5f, 0.8f, 0.4f)); // Grass
				}
			}

			if (_eggMovesContainer != null)
			{
				foreach (Node child in _eggMovesContainer.GetChildren()) child.QueueFree();
				if (!isLocked)
				{
					AddMoveSlot(_eggMovesContainer, "???", new Color(0.5f, 0.8f, 0.4f)); 
					AddMoveSlot(_eggMovesContainer, "???", new Color(0.6f, 0.4f, 0.8f)); 
					AddMoveSlot(_eggMovesContainer, "???", new Color(0.8f, 0.6f, 0.4f)); 
					AddMoveSlot(_eggMovesContainer, "???", new Color(0.5f, 0.8f, 0.4f)); 
				}
			}

			if (_spriteRect != null)
			{
				if (_lastPokemonId != pokemonId || true) // Force update for forms/shiny
				{
					var tex = SpriteLoader.GetPokemonSprite(pokemonId, _isShiny, _currentForm);
					if (tex != null)
					{
						_animatedSprite.Atlas = tex;
						LoadJsonFrames(pokemonId, _currentForm);
						_currentFrame = 0;
						_frameTimer = 0;
						_lastPokemonId = pokemonId;
						UpdateSpriteRegion();
					}
				}

				UpdateFormButtons(pokemonId);

				if (isLocked)
				{
					var mat = new ShaderMaterial();
					mat.Shader = _silhouetteShader;
					mat.SetShaderParameter("is_locked", true);
					_spriteRect.Material = mat;
				}
				else
				{
					_spriteRect.Material = null;
				}
			}
		}

		public void OnConfirmButtonPressed()
		{
			if (_currentSelectionId == -1) return;
			
			var selectedData = DatabaseService.GetPokemonById(_currentSelectionId);
			if (selectedData == null) return;

			var gameManager = GetNode<GameManager>("/root/GameManager"); 
			if (gameManager != null)
			{
				gameManager.StartNewRun();
				
				var clone = selectedData.Clone();
				clone.IsShiny = _isShiny;
				clone.FormSuffix = _currentForm;
				
				gameManager.AddToTeam(clone);
				GetTree().ChangeSceneToFile("res://UI/Gameplay.tscn");
			}
		}

		private void UpdateFormButtons(int pokemonId)
		{
			if (_formContainer == null) return;
			
			foreach (Node child in _formContainer.GetChildren()) child.QueueFree();

			var baseBtn = new Button { Text = "Base", Flat = (_currentForm != "") };
			baseBtn.AddThemeFontSizeOverride("font_size", 14);
			baseBtn.Pressed += () => SetForm("");
			_formContainer.AddChild(baseBtn);
			
			// Mega / G-Max
			CheckAndAddForm(pokemonId, "Mega", "-mega");
			CheckAndAddForm(pokemonId, "G-Max", "-gigantamax");

			// Regionals
			CheckAndAddForm(pokemonId, "Alola", "-alola");
			CheckAndAddForm(pokemonId, "Galar", "-galar");
			CheckAndAddForm(pokemonId, "Hisui", "-hisui");
			CheckAndAddForm(pokemonId, "Paldea", "-paldea");
		}

		private void CheckAndAddForm(int pokemonId, string label, string suffix)
		{
			if (FileAccess.FileExists($"res://Assets/Sprites/Pokemon/Front/{pokemonId}{suffix}.png"))
			{
				var btn = new Button { Text = label, Flat = (_currentForm != suffix) };
				btn.AddThemeFontSizeOverride("font_size", 14);
				btn.Pressed += () => SetForm(suffix);
				_formContainer.AddChild(btn);
			}
		}

		private void AddMoveSlot(Control container, string moveName, Color bgColor)
		{
			var panel = new PanelContainer();
			var style = new StyleBoxFlat {
				BgColor = bgColor,
				CornerRadiusTopLeft = 4,
				CornerRadiusTopRight = 4,
				CornerRadiusBottomLeft = 4,
				CornerRadiusBottomRight = 4,
				ContentMarginBottom = 1,
				ContentMarginTop = 1
			};
			panel.AddThemeStyleboxOverride("panel", style);
			
			var lbl = new Label { 
				Text = moveName, 
				HorizontalAlignment = HorizontalAlignment.Center,
				SizeFlagsHorizontal = SizeFlags.ExpandFill 
			};
			lbl.AddThemeColorOverride("font_color", Colors.White);
			lbl.AddThemeFontSizeOverride("font_size", 14);
			lbl.AddThemeConstantOverride("outline_size", 4);
			lbl.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.3f));
			
			panel.AddChild(lbl);
			container.AddChild(panel);
		}
		private void SetCategory(PokemonCategory category)
		{
			_currentCategory = category;
			PopulateGrid();
		}
	}
}
