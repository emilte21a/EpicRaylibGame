public class SmeltingRecipe
{
    public int inputItemID;
    public int outputItemID;
    public float smeltTime; // seconds per item
    public CraftingTier craftingTier;

    public SmeltingRecipe(int inputItemID, int outputItemID, float smeltTime, CraftingTier craftingTier)
    {
        this.inputItemID = inputItemID;
        this.outputItemID = outputItemID;
        this.smeltTime = smeltTime;
        this.craftingTier = craftingTier;
    }
}

public static class SmeltingRecipes
{
    public static List<SmeltingRecipe> recipes =
    [
        new SmeltingRecipe((int)ItemFactory.ItemID.copperore, (int)ItemFactory.ItemID.copperingot, 5f, CraftingTier.tier1),
        new SmeltingRecipe((int)ItemFactory.ItemID.silverore, (int)ItemFactory.ItemID.silveringot,6f, CraftingTier.tier1),
    ];

    public static SmeltingRecipe? GetRecipeForInput(Item input)
    {
        return recipes.FirstOrDefault(r => r.inputItemID == input.ID);
    }
}
