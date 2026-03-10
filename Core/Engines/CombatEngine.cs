using System;

namespace PokeIdle.Core.Engines
{
    public static class CombatEngine
    {
        // Random generator for damage variance (0.85 to 1.0)
        private static readonly Random rng = new Random();

        /// <summary>
        /// Calculates the final damage output of a move.
        /// </summary>
        /// <param name="level">Level of the attacking Pokemon</param>
        /// <param name="movePower">Base power of the move being used</param>
        /// <param name="attackStat">Effective Attack or Sp. Atk of the attacker</param>
        /// <param name="defenseStat">Effective Defense or Sp. Def of the defender</param>
        /// <param name="modifier">Combined modifier (crit * STAB * effectiveness * weather * random)</param>
        /// <returns>The calculated integer damage</returns>
        public static int CalculateDamage(int level, int movePower, int attackStat, int defenseStat, float modifier = 1f)
        {
            if (movePower <= 0) return 0;

            // Step 1: Base calculation (Level * 2 / 5) + 2
            float step1 = ((level * 2f) / 5f) + 2f;

            // Step 2: Multiply by Power and Attack/Defense ratio
            float step2 = (step1 * movePower * attackStat) / defenseStat;

            // Step 3: Divide by 50 and add 2
            float step3 = (step2 / 50f) + 2f;

            // Step 4: Add standard 85-100% variance if modifier doesn't include it. 
            // Usually we expect the `modifier` parameter to already contain the RNG roll, STAB, Crit, etc.
            // If modifier is precisely 1f, we can optionally roll RNG here, but standard is to pass it.

            // Final: Multiply by total modifiers
            int finalDamage = (int)(step3 * modifier);
            
            // At least 1 damage is always dealt unless immune
            return finalDamage < 1 ? 1 : finalDamage;
        }

        /// <summary>
        /// Roll for a critical hit (Gen VI+ standard is 1/24 or ~4.17% base)
        /// </summary>
        public static bool IsCritical(int critStage)
        {
            int roll = rng.Next(0, 10000);
            int chance = critStage switch
            {
                0 => 417,  // 1/24
                1 => 1250, // 1/8
                2 => 5000, // 1/2
                _ => 10000 // Guaranted
            };
            return roll < chance;
        }

        public static float GetRandomVariance()
        {
            return (float)(rng.NextDouble() * 0.15 + 0.85); // 0.85 to 1.00
        }
    }
}
