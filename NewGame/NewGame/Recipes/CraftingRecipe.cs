public class CraftingRecipe
{
    public CraftingTier craftingTier;
    public short resultItemId;

    public Dictionary<short, int> ingredients;

    public CraftingRecipe(CraftingTier craftingTier, short resultItemId)
    {
        this.craftingTier = craftingTier;
        this.resultItemId = resultItemId;
        ingredients = [];
        AddIngredients();
    }
    public void AddIngredients()
    {
        foreach (var ingredient in ItemFactory.GetRecipeFromItemID(resultItemId))
        {
            ingredients.Add(ingredient.Key, ingredient.Value);
        }
    }
}

public static class CraftingRecipes
{
    public static List<CraftingRecipe> craftingRecipes = [
        new CraftingRecipe(CraftingTier.tier0, (short)ItemFactory.ItemID.craftingtable),
        new CraftingRecipe(CraftingTier.tier0, (short)ItemFactory.ItemID.torch)
    ];
}

public enum CraftingTier
{
    tier0 = 0,
    tier1 = 1,
    tier2 = 2,
    tier3 = 3,
    tier4 = 4,
    tier5 = 5
}