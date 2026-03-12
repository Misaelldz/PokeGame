using Godot;
using System;
using PokeIdle.Core.Services;

namespace PokeIdle.UI
{
    public partial class MainMenu : Control
    {
        [Export] private Control _continueButton;
        [Export] private AudioStreamPlayer _bgmPlayer;
        [Export] private Label _subtitleLabel;
        [Export] private Label _versionLabel;

        public override void _Ready()
        {
            // Puedes cambiar los textos aquí mismo si quieres:
            // _subtitleLabel.Text = "Nuevo Subtítulo";
            // _versionLabel.Text = "v1.2.0";
            // Set up fonts for labels if needed programmatically, 
            // though usually this is done in the .tscn or via a Theme.
            
            // Check if there's a saved run to enable/disable the continue button
            bool hasSavedRun = CheckSavedRun();
            if (_continueButton != null)
            {
                _continueButton.Visible = hasSavedRun;
            }

            // Start BGM
            if (_bgmPlayer != null && !_bgmPlayer.Playing)
            {
                _bgmPlayer.Play();
            }

            GD.Print("[MainMenu] Ready. Save found: " + hasSavedRun);
        }

        private bool CheckSavedRun()
        {
            // Placeholder logic: check GameManager or DatabaseService for profile data
            // For now return false until save system is fully integrated
            return false; 
        }

        public void OnPlayButtonPressed()
        {
            GD.Print("[MainMenu] Play Pressed - Navigating to Starter Selection");
            // ChangeScene("res://Scenes/StarterSelection.tscn");
        }

        public void OnContinueButtonPressed()
        {
            GD.Print("[MainMenu] Continue Pressed - Loading Save");
            // ChangeScene("res://Scenes/GameplayScene.tscn");
        }

        public void OnGachaButtonPressed()
        {
            GD.Print("[MainMenu] Gacha Pressed");
            // ChangeScene("res://Scenes/GachaScene.tscn");
        }

        public void OnStatsButtonPressed()
        {
            GD.Print("[MainMenu] Stats Pressed");
            // ChangeScene("res://Scenes/GlobalStatsScene.tscn");
        }

        public void OnQuitButtonPressed()
        {
            GetTree().Quit();
        }

        private void ChangeScene(string path)
        {
            if (ResourceLoader.Exists(path))
            {
                GetTree().ChangeSceneToFile(path);
            }
            else
            {
                GD.PushError($"[MainMenu] Scene not found: {path}");
            }
        }
    }
}
