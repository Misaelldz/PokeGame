using System;

namespace PokeIdle.Core.Engines
{
    public static class CaptureEngine
    {
        private static readonly Random rng = new Random();

        /// <summary>
        /// Tries to catch a Pokemon using the Gen VI mechanics
        /// </summary>
        /// <param name="hpMax">Maximum HP of the target</param>
        /// <param name="hpCurrent">Current HP of the target</param>
        /// <param name="catchRate">Base catch rate of the species (0-255)</param>
        /// <param name="ballBonus">Pokeball multiplier (1x Poke, 1.5x Great, 2x Ultra, etc)</param>
        /// <param name="statusBonus">1x None, 1.5x Paralysis/Burn/Poison, 2.5x Sleep/Freeze</param>
        /// <returns>True if caught successfully</returns>
        public static bool TryCatch(int hpMax, int hpCurrent, int catchRate, float ballBonus = 1f, float statusBonus = 1f)
        {
            // Master ball logic
            if (ballBonus >= 255f) return true;

            // 1. Calculate the modified catch rate 'a'
            float step1 = (3f * hpMax - 2f * hpCurrent) * catchRate * ballBonus;
            float step2 = step1 / (3f * hpMax);
            float a = step2 * statusBonus;

            // Instant catch safeguard
            if (a >= 255f) return true;

            // 2. Calculate shake probability 'b'
            // Formula: b = 65536 / (255 / a)^0.1875
            // An approximation for standard formula: 1048560 / Sqrt(Sqrt(16711680 / a))
            double denominator = Math.Pow(255d / Math.Max(1d, a), 0.1875d);
            int b = (int)(65536d / denominator);

            // 3. Roll 4 times
            for (int i = 0; i < 4; i++)
            {
                int roll = rng.Next(0, 65536);
                if (roll >= b)
                {
                    return false; // Escapes!
                }
            }

            return true; // Caught! All 4 shakes passed.
        }
    }
}
