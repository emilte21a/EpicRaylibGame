public class FurnaceComponent : Component
{
    public FurnaceSlot? inputSlot;
    public FurnaceSlot? fuelSlot;
    public ResultSlot? resultSlot;

    private SmeltingRecipe? currentRecipe;
    private float smeltProgress = 0f;
    private float fuelTime = 0f;
    private float fuelBurnTime = 8f;
    public bool active = false;

    public float smeltProgressNormalized = 0f;

    public void Update()
    {
        if (inputSlot == null || fuelSlot == null || resultSlot == null)
            return;

        if (inputSlot.itemInSlot == null)
        {
            smeltProgress = 0;
            currentRecipe = null;
            smeltProgressNormalized = 0;
            active = false;
            return;
        }


        currentRecipe = SmeltingRecipes.GetRecipeForInput(inputSlot.itemInSlot);

        if (currentRecipe == null)
        {
            smeltProgress = 0;
            smeltProgressNormalized = 0;
            return;
        }

        if (fuelTime <= 0)
        {
            if (fuelSlot.itemInSlot != null)
            {
                fuelSlot.amount--;
                if (fuelSlot.amount <= 0)
                    fuelSlot.itemInSlot = null;

                fuelTime = fuelBurnTime;
            }
            else
            {
                fuelTime = 0;
                return;
            }
        }

        fuelTime -= Raylib.GetFrameTime();
        smeltProgress += Raylib.GetFrameTime();
        active = true;
        smeltProgressNormalized = smeltProgress / currentRecipe.smeltTime;

        if (smeltProgress >= currentRecipe.smeltTime)
        {
            active = false;
            ProduceResult();
            smeltProgress = 0;
            active = false;
        }
    }

    private void ProduceResult()
    {
        if (resultSlot.itemInSlot == null)
        {
            resultSlot.itemInSlot = ItemFactory.CreateItem((short)currentRecipe.outputItemID);
            resultSlot.amount = 1;
        }
        else if (resultSlot.itemInSlot.ID == currentRecipe.outputItemID)
        {
            resultSlot.amount++;
        }

        inputSlot.amount--;
        if (inputSlot.amount <= 0)
            inputSlot.itemInSlot = null;
    }

    public void SetupSlots(FurnaceSlot fuelSlot, FurnaceSlot inputSlot, ResultSlot resultSlot)
    {
        this.fuelSlot = fuelSlot;
        this.inputSlot = inputSlot;
        this.resultSlot = resultSlot;
    }

    public float GetSmeltProgressNormalized() => smeltProgressNormalized;
    public float GetFuelProgressNormalized() => fuelTime / fuelBurnTime;
}
