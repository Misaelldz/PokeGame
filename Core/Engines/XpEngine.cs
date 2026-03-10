using System;

namespace PokeIdle.Core.Engines
{
    public static class XpEngine
    {
        /// <summary>
        /// Exp yielded by defeating a Pokemon
        /// Formula: (Base Yield * Level) / 7
        /// </summary>
        public static int CalculateBattleXp(int faintedLevel, int baseYield, bool isWild = true)
        {
            float xp = (baseYield * faintedLevel) / 7f;
            
            // Trainer pokemon give 1.5x more XP
            if (!isWild)
            {
                xp *= 1.5f;
            }

            return (int)Math.Max(1, xp);
        }

        /// <summary>
        /// Total XP required to reach the given Level for a specific Growth Rate curve
        /// Formulas based on Bulbapedia
        /// </summary>
        public static int GetExpForLevel(string growthRate, int level)
        {
            if (level <= 1) return 0;
            if (level > 100) level = 100;

            double n3 = Math.Pow(level, 3);
            int xp = 0;

            switch (growthRate.ToLower())
            {
                case "erratic":
                    if (level <= 50)
                        xp = (int)(n3 * (100 - level) / 50);
                    else if (level <= 68)
                        xp = (int)(n3 * (150 - level) / 100);
                    else if (level <= 98)
                        xp = (int)(n3 * Math.Floor((1911 - 10 * level) / 3.0) / 500);
                    else
                        xp = (int)(n3 * (160 - level) / 100);
                    break;
                case "fast":
                    xp = (int)(0.8 * n3);
                    break;
                case "medium fast":
                    xp = (int)n3;
                    break;
                case "medium slow":
                    xp = (int)(1.2 * n3 - 15 * level * level + 100 * level - 140);
                    break;
                case "slow":
                    xp = (int)(1.25 * n3);
                    break;
                case "fluctuating":
                    if (level <= 15)
                        xp = (int)(n3 * (Math.Floor((level + 1) / 3.0) + 24) / 50);
                    else if (level <= 36)
                        xp = (int)(n3 * (level + 14) / 50);
                    else
                        xp = (int)(n3 * (Math.Floor(level / 2.0) + 32) / 50);
                    break;
                default:
                    // Fallback to Medium Fast
                    xp = (int)n3; 
                    break;
            }

            return Math.Max(0, xp);
        }
    }
}
