using System.Linq;
using System.Collections.Generic;
using System.Numerics;

public class CraftingTableComponent : Component
{
    public ResultSlot? resultSlot = null;
    public CraftingTier craftingTier;
    public List<CraftingRecipe> availableRecipes = new List<CraftingRecipe>();

    public void SetupComponent(CraftingTier tier)
    {
        craftingTier = tier;
        availableRecipes = CraftingRecipes.craftingRecipes.Where(r => r.craftingTier <= tier).ToList();
    }

    public bool CanCraft(CraftingRecipe recipe, InventoryComponent inv)
    {
        if (recipe == null || inv == null) return false;
        foreach (var kv in recipe.ingredients)
        {
            short id = kv.Key;
            int req = kv.Value;
            if (inv.GetItemCount(id) < req) return false;
        }
        return true;
    }

    public Item? Craft(CraftingRecipe recipe, InventoryComponent inv)
    {
        if (!CanCraft(recipe, inv)) return null;

        foreach (var ingredient in recipe.ingredients)
        {
            inv.DecreaseItemAmount(ItemFactory.CreateItem(ingredient.Key), ingredient.Value);
        }

        return ItemFactory.CreateItem(recipe.resultItemId);
    }
}