using Godot;
using PokeIdle.Core.Autoloads;
using PokeIdle.Core.Models;
using PokeIdle.Core.Services;

namespace PokeIdle.Core.Systems
{
    /// <summary>
    /// InventorySystem: Capa de lógica sobre el inventario del GameManager.
    ///
    /// Traduce la acción de "usar un ítem" en efectos concretos sobre el juego.
    /// No duplica el estado — siempre opera sobre GameManager.CurrentRun.Items.
    ///
    /// Para usarlo: InventorySystem.UseItem(gameManager, "potion", targetPokemon);
    /// </summary>
    public static class InventorySystem
    {
        // ----------------------------------------------------------------
        // Consultas
        // ----------------------------------------------------------------

        /// <summary>
        /// Devuelve cuántas unidades tiene el jugador de un ítem dado.
        /// </summary>
        public static int GetCount(GameManager gm, string itemId)
        {
            if (gm?.CurrentRun?.Items == null) return 0;
            gm.CurrentRun.Items.TryGetValue(itemId, out int count);
            return count;
        }

        /// <summary>
        /// Devuelve true si el jugador tiene al menos una unidad del ítem.
        /// </summary>
        public static bool HasItem(GameManager gm, string itemId)
            => GetCount(gm, itemId) > 0;

        // ----------------------------------------------------------------
        // Uso de ítems
        // ----------------------------------------------------------------

        /// <summary>
        /// Intenta usar un ítem sobre un Pokémon objetivo.
        /// Devuelve true si el ítem se usó con éxito.
        /// </summary>
        public static bool UseItem(GameManager gm, string itemId, PokemonData target = null)
        {
            if (gm == null || !HasItem(gm, itemId)) return false;

            var itemData = DatabaseService.GetItemById(itemId);
            if (itemData == null)
            {
                GD.PrintErr($"[InventorySystem] Ítem desconocido: {itemId}");
                return false;
            }

            bool used = ApplyItemEffect(gm, itemData, target);

            if (used)
            {
                gm.ConsumeItem(itemId);
                GD.Print($"[InventorySystem] Usado: {itemData.Name} sobre {target?.Name ?? "equipo"}");
            }

            return used;
        }

        // ----------------------------------------------------------------
        // Efectos de ítems
        // ----------------------------------------------------------------

        private static bool ApplyItemEffect(GameManager gm, ItemData item, PokemonData target)
        {
            // La categoría del ítem determina su efecto.
            // Expandir aquí según los ítems del items.csv.
            switch (item.Category?.ToLower())
            {
                case "healing":
                    return ApplyHealingItem(item, target);

                case "pokeball":
                    // Las Pokéballs las gestiona BattleSystem directamente.
                    // InventorySystem solo verifica disponibilidad.
                    return true;

                case "battle":
                    return ApplyBattleItem(item, target);

                default:
                    GD.Print($"[InventorySystem] Categoría '{item.Category}' sin efecto implementado.");
                    return false;
            }
        }

        private static bool ApplyHealingItem(ItemData item, PokemonData target)
        {
            if (target == null) return false;

            int maxHp = CombatantState.CalculateMaxHp(target.Stats.Hp, target.Level);

            // Curar HP completo si el ítem no tiene cantidad definida (e.g. Max Potion)
            if (item.HealAmount <= 0)
            {
                target.CurrentHp = maxHp;
            }
            else
            {
                target.CurrentHp = System.Math.Min(target.CurrentHp + item.HealAmount, maxHp);
            }

            // No se puede usar una poción en un Pokémon desmayado
            if (target.CurrentHp <= 0) return false;

            return true;
        }

        private static bool ApplyBattleItem(ItemData item, PokemonData target)
        {
            if (target == null) return false;
            // Placeholder para ítems de batalla (X Attack, X Defense, etc.)
            // Se implementará cuando tengamos el sistema de etapas activo en BattleSystem
            GD.Print($"[InventorySystem] Ítem de batalla '{item.Name}' — pendiente de implementar etapas.");
            return true;
        }
    }
}
