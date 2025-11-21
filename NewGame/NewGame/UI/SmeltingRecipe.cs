public class SmeltingRecipe
{
    public int inputItemID;
    public int outputItemID;
    public float smeltTime; // seconds per item

    public SmeltingRecipe(int inputItemID, int outputItemID, float smeltTime)
    {
        this.inputItemID = inputItemID;
        this.outputItemID = outputItemID;
        this.smeltTime = smeltTime;
    }
}

public static class SmeltingRecipes
{
    public static List<SmeltingRecipe> recipes =
    [
        new SmeltingRecipe((int)ItemFactory.ItemID.copperore, (int)ItemFactory.ItemID.copperingot, 5f),
        new SmeltingRecipe((int)ItemFactory.ItemID.silverore, (int)ItemFactory.ItemID.silverIngot,6f),
    ];

    public static SmeltingRecipe? GetRecipeForInput(Item input)
    {
        return recipes.FirstOrDefault(r => r.inputItemID == input.ID);
    }
}
