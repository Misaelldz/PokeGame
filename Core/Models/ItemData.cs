namespace PokeIdle.Core.Models
{
    public class ItemData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SpriteSlug { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Effect { get; set; }
        public bool Buyable { get; set; }
        public bool Lootable { get; set; }
        public int ShopPrice { get; set; }
        public int SortOrder { get; set; }
        public int HealAmount { get; set; }
    }
}
