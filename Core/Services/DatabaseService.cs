using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PokeIdle.Core.Models;

namespace PokeIdle.Core.Services
{
    public partial class DatabaseService : Node
    {
        public static DatabaseService Instance { get; private set; }

        public Dictionary<string, ItemData> Items { get; private set; } = new Dictionary<string, ItemData>();
        public Dictionary<string, ZoneData> Zones { get; private set; } = new Dictionary<string, ZoneData>();
        public Dictionary<string, GymData> Gyms { get; private set; } = new Dictionary<string, GymData>();
        public Dictionary<int, MoveData> Moves { get; private set; } = new Dictionary<int, MoveData>();
        public Dictionary<int, SpeciesData> Species { get; private set; } = new Dictionary<int, SpeciesData>();
        public Dictionary<int, PokemonData> Pokemon { get; private set; } = new Dictionary<int, PokemonData>();
        public List<EvolutionData> Evolutions { get; private set; } = new List<EvolutionData>();
        public Dictionary<string, MegaEvolutionData> MegaEvolutions { get; private set; } = new Dictionary<string, MegaEvolutionData>();
        public List<EliteFourData> EliteFour { get; private set; } = new List<EliteFourData>();
        public Dictionary<string, UserProfileData> UserProfiles { get; private set; } = new Dictionary<string, UserProfileData>();

        public override void _Ready()
        {
            Instance = this;
            LoadItems();
            LoadZones();
            LoadGyms();
            LoadMoves();
            LoadSpecies();
            LoadPokemon();
            LoadEvolutions();
            LoadMegaEvolutions();
            LoadEliteFour();
            LoadUserProfiles();
            
            GD.Print($"[DatabaseService] Loaded {Items.Count} Items, {Zones.Count} Zones, {Gyms.Count} Gyms, {Pokemon.Count} Pokemon.");
        }

        private void LoadItems()
        {
            using var file = FileAccess.Open("res://Assets/Data/items.csv", FileAccess.ModeFlags.Read);
            if (file == null) return;
            
            file.GetCsvLine(); // Skip header row
            while (!file.EofReached())
            {
                var row = file.GetCsvLine();
                if (row.Length < 10) continue;
                
                var item = new ItemData
                {
                    Id = row[0],
                    Name = row[1],
                    SpriteSlug = row[2],
                    Description = row[3],
                    Category = row[4],
                    Effect = row[5],
                    Buyable = row[6].ToLower() == "true" || row[6] == "1",
                    Lootable = row[7].ToLower() == "true" || row[7] == "1",
                    ShopPrice = int.TryParse(row[8], out int price) ? price : 0,
                    SortOrder = int.TryParse(row[9], out int sort) ? sort : 0
                };

                // Parse HealAmount from Effect JSON if applicable
                if (!string.IsNullOrWhiteSpace(item.Effect))
                {
                    try
                    {
                        var effectDoc = JsonDocument.Parse(item.Effect);
                        if (effectDoc.RootElement.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "heal_hp")
                        {
                            if (effectDoc.RootElement.TryGetProperty("amount", out var amountProp))
                            {
                                if (amountProp.ValueKind == JsonValueKind.Number)
                                {
                                    item.HealAmount = amountProp.GetInt32();
                                }
                                else if (amountProp.ValueKind == JsonValueKind.String && amountProp.GetString() == "full")
                                {
                                    item.HealAmount = 0; // InventorySystem treats <= 0 as full heal
                                }
                            }
                        }
                    }
                    catch { /* Ignore parse errors for malformed JSON */ }
                }

                Items[row[0]] = item;
            }
        }

        private void LoadZones()
        {
            using var file = FileAccess.Open("res://Assets/Data/zones.csv", FileAccess.ModeFlags.Read);
            if (file == null) return;
            
            file.GetCsvLine(); // Skip header row
            while (!file.EofReached())
            {
                var row = file.GetCsvLine();
                if (row.Length < 17) continue;
                
                var zone = new ZoneData
                {
                    Id = row[0],
                    Region = row[1],
                    ZoneIndex = int.TryParse(row[2], out int idx) ? idx : 0,
                    Name = row[3],
                    MinLevel = int.TryParse(row[4], out int min) ? min : 0,
                    MaxLevel = int.TryParse(row[5], out int max) ? max : 0,
                    Pokemon = ParseJson<List<ZonePokemonEntry>>(row[6]),
                    IsGym = row[7].ToLower() == "true" || row[7] == "1",
                    GymBadgeId = row[8],
                    GymId = row[10],
                    BattleBgId = row[11],
                    EncounterRate = float.TryParse(row[12], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float rate) ? rate : 1f,
                    TrainerCount = int.TryParse(row[13], out int tCount) ? tCount : 0,
                    ReferenceBst = int.TryParse(row[14], out int bst) ? bst : 0,
                    ItemDrops = ParseStringList(row[15]),
                    Description = row[16]
                };
                Zones[row[0]] = zone;
            }
        }
        
        private void LoadGyms()
        {
            using var file = FileAccess.Open("res://Assets/Data/gyms.csv", FileAccess.ModeFlags.Read);
            if (file == null) return;
            
            file.GetCsvLine(); // Skip header row
            while (!file.EofReached())
            {
                var row = file.GetCsvLine();
                if (row.Length < 13) continue;
                
                var gym = new GymData
                {
                    Id = row[0],
                    Region = row[1],
                    LeaderName = row[2],
                    BadgeName = row[3],
                    Type = row[4],
                    UnlockLevel = int.TryParse(row[5], out int ulvl) ? ulvl : 0,
                    ReferenceBst = int.TryParse(row[6], out int bst) ? bst : 0,
                    Mechanic = row[7],
                    Pokemon = ParseStringList(row[8]),
                    RewardItems = ParseStringList(row[9]),
                    DialogIntro = row[10],
                    DialogVictory = row[11],
                    DialogDefeat = row[12]
                };
                Gyms[row[0]] = gym;
            }
        }

        // Helper string array parser for Lists inside CSV (like ["Pikachu", "Charmander"])
        private List<string> ParseStringList(string jsonStr)
        {
            if (string.IsNullOrWhiteSpace(jsonStr) || jsonStr == "[]" || jsonStr == "\"[]\"") return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(jsonStr) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private T ParseJson<T>(string jsonStr) where T : new()
        {
            if (string.IsNullOrWhiteSpace(jsonStr) || jsonStr == "{}" || jsonStr == "[]") return new T();
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<T>(jsonStr, options) ?? new T();
            }
            catch
            {
                return new T();
            }
        }

        private void LoadMoves()
        {
            using var file = FileAccess.Open("res://Assets/Data/move_cache.csv", FileAccess.ModeFlags.Read);
            if (file == null) return;
            file.GetCsvLine();
            while (!file.EofReached())
            {
                var row = file.GetCsvLine();
                if (row.Length < 11 || !int.TryParse(row[0], out int id)) continue;
                Moves[id] = new MoveData
                {
                    MoveId = id,
                    NameEs = row[1],
                    NameEn = row[2],
                    Type = row[3],
                    Category = row[4],
                    Power = int.TryParse(row[5], out int pwr) ? pwr : null,
                    Accuracy = int.TryParse(row[6], out int acc) ? acc : null,
                    Pp = int.TryParse(row[7], out int pp) ? pp : 0,
                    Priority = int.TryParse(row[8], out int prio) ? prio : 0,
                    Ailment = row[9],
                    AilmentChance = int.TryParse(row[10], out int ailc) ? ailc : null
                };
            }
        }

        private void LoadSpecies()
        {
            using var file = FileAccess.Open("res://Assets/Data/species_cache.csv", FileAccess.ModeFlags.Read);
            if (file == null) return;
            file.GetCsvLine();
            while (!file.EofReached())
            {
                var row = file.GetCsvLine();
                if (row.Length < 9 || !int.TryParse(row[0], out int id)) continue;
                Species[id] = new SpeciesData
                {
                    PokemonId = id,
                    Name = row[1],
                    IsLegendary = row[2].ToLower() == "true" || row[2] == "1",
                    IsMythical = row[3].ToLower() == "true" || row[3] == "1",
                    EvolvesFromId = int.TryParse(row[4], out int evFrom) ? evFrom : null,
                    EvolutionChainUrl = row[5],
                    GrowthRate = row[6],
                    GachaTier = int.TryParse(row[8], out int t) ? t : 1
                };
            }
        }

        private void LoadPokemon()
        {
            using var file = FileAccess.Open("res://Assets/Data/pokemon_cache.csv", FileAccess.ModeFlags.Read);
            if (file == null) return;
            file.GetCsvLine();
            while (!file.EofReached())
            {
                var row = file.GetCsvLine();
                if (row.Length < 10 || !int.TryParse(row[0], out int id)) continue;
                var stats = ParseJson<BaseStats>(row[3]);
                var pData = new PokemonData
                {
                    Id = id,
                    Name = row[1],
                    Types = ParseStringList(row[2]),
                    Stats = stats,
                    LevelUpMoves = ParseJson<List<LevelUpMove>>(row[4]),
                    SpriteUrl = row[5],
                    TmMoves = ParseJson<List<int>>(row[8]),
                    Ability = row[9],
                    
                    // Initialization of missing data
                    BaseExpYield = 60, // Default fallback
                    CatchRate = 45,    // Default fallback (medium-hard)
                    Level = 1,
                    CurrentHp = stats.Hp > 0 ? stats.Hp : 10, // Fallback if 0
                    CurrentXp = 0
                };

                // Merge GrowthRate from Species (loaded previously)
                if (Species.TryGetValue(id, out var speciesData))
                {
                    pData.GrowthRate = speciesData.GrowthRate;
                }
                else
                {
                    pData.GrowthRate = "medium fast"; // Fallback
                }

                Pokemon[id] = pData;
            }
        }

        private void LoadEvolutions()
        {
            using var file = FileAccess.Open("res://Assets/Data/evolution_cache.csv", FileAccess.ModeFlags.Read);
            if (file == null) return;
            file.GetCsvLine();
            while (!file.EofReached())
            {
                var row = file.GetCsvLine();
                if (row.Length < 5 || !int.TryParse(row[0], out int fromId)) continue;
                Evolutions.Add(new EvolutionData
                {
                    FromPokemonId = fromId,
                    ToPokemonId = int.TryParse(row[1], out int toId) ? toId : 0,
                    Trigger = row[2],
                    MinLevel = int.TryParse(row[3], out int minLvl) ? minLvl : null,
                    ItemId = row[4]
                });
            }
        }

        private void LoadMegaEvolutions()
        {
            using var file = FileAccess.Open("res://Assets/Data/mega_evolutions.csv", FileAccess.ModeFlags.Read);
            if (file == null) return;
            file.GetCsvLine();
            while (!file.EofReached())
            {
                var row = file.GetCsvLine();
                if (row.Length < 7) continue;
                MegaEvolutions[row[0]] = new MegaEvolutionData
                {
                    Id = row[0],
                    BasePokemonId = int.TryParse(row[1], out int bId) ? bId : 0,
                    MegaPokemonId = int.TryParse(row[2], out int mId) ? mId : 0,
                    MegaName = row[3],
                    BaseName = row[4],
                    RequiredItem = row[5],
                    TypeOverride = row[6]
                };
            }
        }

        private void LoadEliteFour()
        {
            using var file = FileAccess.Open("res://Assets/Data/elite_four.csv", FileAccess.ModeFlags.Read);
            if (file == null) return;
            file.GetCsvLine();
            while (!file.EofReached())
            {
                var row = file.GetCsvLine();
                if (row.Length < 6) continue;
                EliteFour.Add(new EliteFourData
                {
                    Region = row[0],
                    Slot = int.TryParse(row[1], out int s) ? s : 0,
                    Name = row[2],
                    Role = row[3],
                    Type = row[4],
                    Pokemon = ParseStringList(row[5])
                });
            }
        }

        private void LoadUserProfiles()
        {
            using var file = FileAccess.Open("res://Assets/Data/user_profiles.csv", FileAccess.ModeFlags.Read);
            if (file == null) return;
            file.GetCsvLine();
            while (!file.EofReached())
            {
                var row = file.GetCsvLine();
                if (row.Length < 7) continue;
                UserProfiles[row[0]] = new UserProfileData
                {
                    Id = row[0],
                    RunState = row[1],
                    MetaState = row[2],
                    TrainingState = row[3],
                    Email = row[5],
                    Username = row[6]
                };
            }
        }

        // -------------------------------------------------------------------------
        // API de Acceso (Estática para facilitar llamadas desde cualquier sitio)
        // -------------------------------------------------------------------------

        public static ZoneData GetZoneById(string id) => Instance?.Zones.GetValueOrDefault(id);
        public static PokemonData GetPokemonById(int id) => Instance?.Pokemon.GetValueOrDefault(id);
        public static MoveData GetMoveById(int id) => Instance?.Moves.GetValueOrDefault(id);
        public static ItemData GetItemById(string id) => Instance?.Items.GetValueOrDefault(id);
        
        public static List<PokemonData> GetAllPokemon() => Instance != null ? new List<PokemonData>(Instance.Pokemon.Values) : new List<PokemonData>();
        public static List<ItemData> GetAllItems() => Instance != null ? new List<ItemData>(Instance.Items.Values) : new List<ItemData>();
        public static List<ZoneData> GetAllZones() => Instance != null ? new List<ZoneData>(Instance.Zones.Values) : new List<ZoneData>();

        public static EvolutionData GetEvolutionFor(int pokemonId) => Instance?.Evolutions.FirstOrDefault(e => e.FromPokemonId == pokemonId);
    }
}
