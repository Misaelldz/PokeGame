using Godot;
using System;
using System.Collections.Generic;

public partial class MainMenu : Control
{
	// Rutas a los nodos en la escena, las asignaremos desde el Inspector
	[Export] private NodePath CursorPath;
	[Export] private NodePath OptionsContainerPath;

	private Control _cursor;
	private VBoxContainer _optionsContainer;
	private List<Button> _options = new List<Button>();
	private int _selectedIndex = 0;
	private AudioStreamPlayer _bgmPlayer;

	public override void _Ready()
	{
		// Configurar y reproducir BGM
		_bgmPlayer = new AudioStreamPlayer();
		AddChild(_bgmPlayer);
		_bgmPlayer.Stream = GD.Load<AudioStream>("res://Assets/Audio/BGM/MainMenu_1.mp3");
		_bgmPlayer.Bus = "BGM";
		_bgmPlayer.Play();

		if (CursorPath != null) _cursor = GetNode<Control>(CursorPath);
		if (OptionsContainerPath != null) _optionsContainer = GetNode<VBoxContainer>(OptionsContainerPath);

		if (_optionsContainer == null) 
		{
			GD.PrintErr("Falta asignar el contenedor de opciones (VBoxContainer) en MainMenu.");
			return;
		}

		// Leer todas las opciones (Buttons) que estén visibles
		foreach (Node child in _optionsContainer.GetChildren())
		{
			if (child is Button btn && btn.Visible)
			{
				_options.Add(btn);
				
				// Connect signals for mouse interaction
				btn.MouseEntered += () => {
					_selectedIndex = _options.IndexOf(btn);
					UpdateCursorPosition();
				};
			}
		}

		UpdateCursorPosition();
	}

	public override void _Process(double delta)
	{
		if (_options.Count == 0) return;

		if (Input.IsActionJustPressed("ui_up"))
		{
			_selectedIndex--;
			if (_selectedIndex < 0) _selectedIndex = _options.Count - 1;
			UpdateCursorPosition();
			// TODO: Reproducir sonido de 'bip' de navegación
		}
		else if (Input.IsActionJustPressed("ui_down"))
		{
			_selectedIndex++;
			if (_selectedIndex >= _options.Count) _selectedIndex = 0;
			UpdateCursorPosition();
			// TODO: Reproducir sonido de 'bip' de navegación
		}
		else if (Input.IsActionJustPressed("ui_accept")) // Tecla Z, Enter, Espacio
		{
			ExecuteOption(_options[_selectedIndex].Name);
			// TODO: Reproducir sonido de 'bip' de aceptación
		}
	}

	private void UpdateCursorPosition()
	{
		if (_options.Count == 0 || _cursor == null) return;

		Button targetBtn = _options[_selectedIndex];
		
		// Alineamos el cursor a la izquierda del botón actual.
		Vector2 labelPos = targetBtn.GlobalPosition;
		Vector2 cursorSize = _cursor.Size;
		
		_cursor.GlobalPosition = new Vector2(
			labelPos.X - cursorSize.X - 8, 
			labelPos.Y + (targetBtn.Size.Y / 2) - (cursorSize.Y / 2)
		);
	}

	// Signal handlers connected in editor or dynamically
	public void OnPlayButtonPressed() => ExecuteOption("PlayButton");
	public void OnContinueButtonPressed() => ExecuteOption("ContinueButton");
	public void OnGachaButtonPressed() => ExecuteOption("GachaButton");
	public void OnStatsButtonPressed() => ExecuteOption("StatsButton");
	public void OnQuitButtonPressed() => GetTree().Quit();

	private void ExecuteOption(string optionName)
	{
		GD.Print($"Opción seleccionada: {optionName}");
		
		switch (optionName)
		{
			case "PlayButton":
				GetTree().ChangeSceneToFile("res://UI/StarterSelection.tscn");
				break;
			case "ContinueButton":
				// Lógica de cargar la partida guardada
				break;
			case "GachaButton":
				break;
			case "StatsButton":
				break;
			default:
				GD.PrintErr("Opción de menú no reconocida: " + optionName);
				break;
		}
	}
}
