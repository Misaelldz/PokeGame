using System.Collections.Generic;

namespace PokeIdle.Core.Models
{
    // Clase C# pura para almacenar los datos estáticos de un Pokémon que leemos del CSV.
    // No hereda de Godot.Node porque esto es solo "Información", no algo que se dibuja en pantalla.
    public class PokemonData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        // Los tipos vienen como un array JSON en el CSV, los guardaremos como una lista.
        public List<string> Types { get; set; } = new List<string>();
        
        public BaseStats Stats { get; set; }
        
        public string SpriteUrl { get; set; }
        
        public string Ability { get; set; }
        
        // --- Static Data (Missing in current CSV, but required by systems) ---
        public string GrowthRate { get; set; }
        public int BaseExpYield { get; set; }
        public int CatchRate { get; set; }
        
        // --- Instance Data (Used for current state in team/battle) ---
        public int Level { get; set; } = 1;
        public int CurrentHp { get; set; }
        public int CurrentXp { get; set; }
        public bool IsShiny { get; set; }

        // Para los movimientos por nivel (level_up_moves), podemos mapearlos a una estructura
        public List<LevelUpMove> LevelUpMoves { get; set; } = new List<LevelUpMove>();
        
        public List<int> TmMoves { get; set; } = new List<int>();
    }

    // Estructura para contener las estadísticas base
    public struct BaseStats
    {
        public int Hp { get; set; }
        public int Atk { get; set; }
        public int Def { get; set; }
        public int SpAtk { get; set; }
        public int SpDef { get; set; }
        public int Speed { get; set; }
    }

    // Estructura para los movimientos que aprende por nivel
    public struct LevelUpMove
    {
        public int Level { get; set; }
        public int MoveId { get; set; }
    }
}
