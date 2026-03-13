using Godot;
using System.Collections.Generic;
using PokeIdle.Core.Models;

namespace PokeIdle.Core.Services
{
    /// <summary>
    /// Servicio central de carga de sprites.
    /// Resuelve la ruta correcta de cada asset según:
    ///   1. Variante desbloqueada activa (cosméticos/eventos)
    ///   2. Sprite base del asset
    ///   3. Fallback a un placeholder si el archivo no existe
    ///
    /// Rutas base (res://):
    ///   Pokémon Front  → Assets/Sprites/Pokemon/Front/{id}.png
    ///   Pokémon Back   → Assets/Sprites/Pokemon/Back/{id}.png
    ///   Shiny Front    → Assets/Sprites/Pokemon/Shiny/Front/{id}.png
    ///   Shiny Back     → Assets/Sprites/Pokemon/Shiny/Back/{id}.png
    ///   Variantes      → Assets/Sprites/Pokemon/Variants/{id}_{variant}.png
    ///   Arenas         → Assets/Sprites/Arenas/{arenaId}.png
    ///   Trainers       → Assets/Sprites/Trainers/{trainerId}.png
    ///   Pokéballs      → Assets/Sprites/Pokeballs/{ballId}.png
    /// </summary>
    public static class SpriteLoader
    {
        // ── Rutas base ────────────────────────────────────────────────────────────
        private const string BasePokemonFront   = "res://Assets/Sprites/Pokemon/Front/";
        private const string BasePokemonBack    = "res://Assets/Sprites/Pokemon/Back/";
        private const string BaseShinyFront     = "res://Assets/Sprites/Pokemon/Shiny/Front/";
        private const string BaseShinyBack      = "res://Assets/Sprites/Pokemon/Shiny/Back/";
        private const string BaseVariants       = "res://Assets/Sprites/Pokemon/Variants/";
        private const string BaseArenas         = "res://Assets/Sprites/Arenas/";
        private const string BaseTrainers       = "res://Assets/Sprites/Trainers/";
        private const string BasePokeballs      = "res://Assets/Sprites/Pokeballs/";
        private const string BasePlaceholder    = "res://Assets/Sprites/placeholder.svg";
        private const string BaseIcons          = "res://Assets/Sprites/Icons/";

        // ── Caché en memoria para no recargar texturas repetidamente ─────────────
        private static readonly Dictionary<string, Texture2D> _cache = new();

        // ── Variantes activas del jugador: pokedexId → variantName ───────────────
        private static readonly Dictionary<int, string> _activeVariants = new();

        // ── Sistema de Iconos (PC Style) ─────────────────────────────────────────
        private static readonly Dictionary<string, (Texture2D sheet, Rect2 region)> _iconCache = new();
        private static bool _iconsInitialized = false;

        // ═════════════════════════════════════════════════════════════════════════
        //  POKÉMON SPRITES
        // ═════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Devuelve el sprite frontal del Pokémon.
        /// Prioridad: Variante cosmética activa → Shiny base → Sprite normal → Placeholder
        /// Formas: "" (base), "-mega", "-gigantamax"
        /// </summary>
        public static Texture2D GetPokemonSprite(int pokedexId, bool shiny = false, string form = "")
        {
            // 1. ¿Hay una variante cosmética activa para este Pokémon? (Solo aplica a forma base usualmente)
            if (string.IsNullOrEmpty(form) && _activeVariants.TryGetValue(pokedexId, out string variant))
            {
                string variantSuffix = shiny ? $"{pokedexId}_{variant}_shiny.png" : $"{pokedexId}_{variant}.png";
                var variantTex = LoadTexture(BaseVariants + variantSuffix);
                if (variantTex != null) return variantTex;
                
                if (shiny)
                {
                    var variantNormalTex = LoadTexture(BaseVariants + $"{pokedexId}_{variant}.png");
                    if (variantNormalTex != null) return variantNormalTex;
                }
            }

            // 2. Sprite shiny o normal con forma (mega/gigantamax)
            string filename = $"{pokedexId}{form}.png";
            string basePath = shiny
                ? BaseShinyFront + filename
                : BasePokemonFront + filename;

            return LoadTexture(basePath) ?? GetPlaceholder();
        }

        public static Texture2D GetPokemonSprite(PokemonData pokemon)
        {
            if (pokemon == null) return GetPlaceholder();
            return GetPokemonSprite(pokemon.Id, pokemon.IsShiny, pokemon.FormSuffix);
        }

        /// <summary>
        /// Devuelve el sprite de espalda del Pokémon (lado del jugador en batalla).
        /// </summary>
        public static Texture2D GetPokemonBackSprite(int pokedexId, bool shiny = false, string form = "")
        {
            string filename = $"{pokedexId}{form}.png";
            string path = shiny
                ? BaseShinyBack + filename
                : BasePokemonBack + filename;

            return LoadTexture(path) ?? GetPlaceholder();
        }

        public static Texture2D GetPokemonBackSprite(PokemonData pokemon)
        {
            if (pokemon == null) return GetPlaceholder();
            return GetPokemonBackSprite(pokemon.Id, pokemon.IsShiny, pokemon.FormSuffix);
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  SISTEMA DE ICONOS (PC STYLE)
        // ═════════════════════════════════════════════════════════════════════════

        public static void InitializeIconCache()
        {
            if (_iconsInitialized) return;

            GD.Print("[SpriteLoader] Initializing PC Icon Cache...");
            using var dir = DirAccess.Open(BaseIcons);
            if (dir == null) 
            {
                GD.PrintErr($"[SpriteLoader] Could not open icons directory: {BaseIcons}");
                return;
            }

            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (fileName != "")
            {
                if (!dir.CurrentIsDir() && fileName.EndsWith(".json") && fileName.StartsWith("pokemon_icons_"))
                {
                    ParseIconJson(BaseIcons + fileName);
                }
                fileName = dir.GetNext();
            }

            _iconsInitialized = true;
            GD.Print($"[SpriteLoader] PC Icon Cache initialized with {_iconCache.Count} entries.");
        }

        private static void ParseIconJson(string path)
        {
            if (!FileAccess.FileExists(path)) return;

            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            string jsonText = file.GetAsText();
            var json = new Json();
            if (json.Parse(jsonText) != Error.Ok) return;

            var data = json.Data.AsGodotDictionary();
            if (!data.ContainsKey("textures")) return;

            var textures = data["textures"].AsGodotArray();
            foreach (var texVar in textures)
            {
                var texDict = texVar.AsGodotDictionary();
                string imageName = texDict["image"].AsString();
                var sheet = LoadTexture(BaseIcons + imageName);
                if (sheet == null) continue;

                var frames = texDict["frames"].AsGodotArray();
                foreach (var frameVar in frames)
                {
                    var frameDict = frameVar.AsGodotDictionary();
                    string id = frameDict["filename"].AsString();
                    var rectDict = frameDict["frame"].AsGodotDictionary();
                    
                    Rect2 region = new Rect2(
                        (float)rectDict["x"],
                        (float)rectDict["y"],
                        (float)rectDict["w"],
                        (float)rectDict["h"]
                    );

                    _iconCache[id] = (sheet, region);
                }
            }
        }

        /// <summary>
        /// Devuelve un AtlasTexture con el icono estilo PC (Pokerogue).
        /// Clave esperada: "{id}" o "{id}s" (shiny) o "{id}-mega", etc.
        /// </summary>
        public static AtlasTexture GetPokemonIconAtlas(int pokedexId, bool shiny = false, string formSuffix = "")
        {
            InitializeIconCache();

            // Construct Key: {id}{s?}{form?}
            // Examples: "6", "6s", "6-mega", "6s-mega"
            string shinyPart = shiny ? "s" : "";
            string key = $"{pokedexId}{shinyPart}{formSuffix}";

            if (_iconCache.TryGetValue(key, out var data))
            {
                return new AtlasTexture { Atlas = data.sheet, Region = data.region };
            }

            // Fallback to base form if specific form icon fails
            if (!string.IsNullOrEmpty(formSuffix))
            {
                key = $"{pokedexId}{shinyPart}";
                if (_iconCache.TryGetValue(key, out data))
                    return new AtlasTexture { Atlas = data.sheet, Region = data.region };
            }

            // Ultimate Fallback: Placeholder if nothing matches
            return new AtlasTexture { Atlas = GetPlaceholder(), Region = new Rect2(0, 0, 32, 32) };
        }

        public static AtlasTexture GetPokemonIconAtlas(PokemonData pokemon)
        {
            if (pokemon == null) return null;
            return GetPokemonIconAtlas(pokemon.Id, pokemon.IsShiny, pokemon.FormSuffix);
        }

        public static AtlasTexture GetStatusIconAtlas(string statusName)
        {
            // Simple indexing for statuses.json
            string path = BaseIcons + "statuses.json";
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null) return null;

            string jsonText = file.GetAsText();
            var json = new Json();
            if (json.Parse(jsonText) != Error.Ok) return null;

            var data = json.Data.AsGodotDictionary();
            var textures = data["textures"].AsGodotArray();
            var texDict = textures[0].AsGodotDictionary();
            var sheet = LoadTexture(BaseIcons + "statuses.png");
            var frames = texDict["frames"].AsGodotArray();

            foreach (var frameVar in frames)
            {
                var frameDict = frameVar.AsGodotDictionary();
                if (frameDict["filename"].AsString() == statusName)
                {
                    var rectDict = frameDict["frame"].AsGodotDictionary();
                    Rect2 region = new Rect2((float)rectDict["x"], (float)rectDict["y"], (float)rectDict["w"], (float)rectDict["h"]);
                    return new AtlasTexture { Atlas = sheet, Region = region };
                }
            }
            return null;
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  ARENAS / FONDOS DE BATALLA
        // ═════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Devuelve el fondo de batalla para el bioma/arena indicado.
        /// El arenaId viene del campo BattleBgId de ZoneData (ej. "plains", "cave", "forest").
        /// Fallback: "default" → placeholder.
        /// </summary>
        public static Texture2D GetArena(string arenaId)
        {
            if (string.IsNullOrWhiteSpace(arenaId)) return GetPlaceholder();

            // Intentar con el id exacto
            var tex = LoadTexture(BaseArenas + $"{arenaId}.png");
            if (tex != null) return tex;

            // Fallback a "default"
            return LoadTexture(BaseArenas + "default.png") ?? GetPlaceholder();
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  TRAINERS / LÍDERES DE GIMNASIO
        // ═════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Devuelve el sprite del entrenador/líder de gimnasio.
        /// El trainerId viene del campo Id de GymData (ej. "brock", "misty").
        /// </summary>
        public static Texture2D GetTrainerSprite(string trainerId)
        {
            if (string.IsNullOrWhiteSpace(trainerId)) return GetPlaceholder();
            return LoadTexture(BaseTrainers + $"{trainerId}.png") ?? GetPlaceholder();
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  POKÉBALLS
        // ═════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Devuelve el sprite de una Pokéball por su id (ej. "poke_ball", "great_ball").
        /// </summary>
        public static Texture2D GetPokeballSprite(string ballId)
        {
            if (string.IsNullOrWhiteSpace(ballId)) return GetPlaceholder();
            return LoadTexture(BasePokeballs + $"{ballId}.png") ?? GetPlaceholder();
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  SISTEMA DE VARIANTES (Eventos / Cosméticos)
        // ═════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Activa una variante de sprite para un Pokémon.
        /// Llamar desde GameManager al completar una misión de evento.
        /// Ejemplo: SpriteLoader.SetActiveVariant(25, "gen1") → Pikachu retro Gen 1.
        /// </summary>
        public static void SetActiveVariant(int pokedexId, string variantName)
        {
            _activeVariants[pokedexId] = variantName;
            GD.Print($"[SpriteLoader] Variante activa para #{pokedexId}: '{variantName}'");
        }

        /// <summary>
        /// Elimina la variante activa de un Pokémon (vuelve al sprite base).
        /// </summary>
        public static void ClearVariant(int pokedexId)
        {
            _activeVariants.Remove(pokedexId);
        }

        /// <summary>
        /// Carga todas las variantes activas desde el save del jugador.
        /// Llamar desde GameManager._Ready() después de cargar el save.
        /// </summary>
        public static void LoadVariantsFromSave(Dictionary<int, string> savedVariants)
        {
            _activeVariants.Clear();
            if (savedVariants == null) return;
            foreach (var kv in savedVariants)
                _activeVariants[kv.Key] = kv.Value;
            GD.Print($"[SpriteLoader] {_activeVariants.Count} variantes cargadas desde el save.");
        }

        /// <summary>
        /// Devuelve todas las variantes activas (para guardar en el save file).
        /// </summary>
        public static Dictionary<int, string> GetActiveVariants()
        {
            return new Dictionary<int, string>(_activeVariants);
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  UTILIDADES INTERNAS
        // ═════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Limpia el caché de texturas (útil al cambiar de escena para liberar memoria).
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
        }

        private static Texture2D LoadTexture(string path)
        {
            if (_cache.TryGetValue(path, out var cached))
                return cached;

            if (!ResourceLoader.Exists(path))
            {
                _cache[path] = null; // Guardar null para no buscar de nuevo
                return null;
            }

            var tex = ResourceLoader.Load<Texture2D>(path);
            _cache[path] = tex;
            return tex;
        }

        private static Texture2D GetPlaceholder()
        {
            return LoadTexture(BasePlaceholder);
        }
    }
}
