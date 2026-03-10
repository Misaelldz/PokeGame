using System.Collections.Generic;

namespace PokeIdle.Core.Models
{
    public class EliteFourData
    {
        public string Region { get; set; }
        public int Slot { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public string Type { get; set; }
        public List<string> Pokemon { get; set; } = new List<string>();
    }
}
