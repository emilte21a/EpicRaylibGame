using System.Text.Json;

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

    // DTO used for saving/loading furnace state
    public class FurnaceStateDTO
    {
        public short inputItemId = -1;
        public int inputAmount = 0;
        public short fuelItemId = -1;
        public int fuelAmount = 0;
        public short resultItemId = -1;
        public int resultAmount = 0;
        public float smeltProgressNormalized = 0f;
        public float fuelTime = 0f;
        public bool active = false;
    }

    public FurnaceStateDTO ToDTO()
    {
        var dto = new FurnaceStateDTO();
        dto.inputItemId = (short)(inputSlot?.itemInSlot?.ID ?? -1);
        dto.inputAmount = inputSlot?.amount ?? 0;
        dto.fuelItemId = (short)(fuelSlot?.itemInSlot?.ID ?? -1);
        dto.fuelAmount = fuelSlot?.amount ?? 0;
        dto.resultItemId = (short)(resultSlot?.itemInSlot?.ID ?? -1);
        dto.resultAmount = resultSlot?.amount ?? 0;
        dto.smeltProgressNormalized = smeltProgressNormalized;
        dto.fuelTime = fuelTime;
        dto.active = active;
        return dto;
    }

    public void FromDTO(FurnaceStateDTO dto)
    {
        // helper to create Item instance without spawning into world
        Item? item;
        if (dto.inputItemId >= 0)
        {
            item = ItemFactory.CreateDroppedItem(dto.inputItemId, Vector2.Zero)?.item;
            if (inputSlot != null) { inputSlot.itemInSlot = item; inputSlot.amount = dto.inputAmount; }
        }

        if (dto.fuelItemId >= 0)
        {
            item = ItemFactory.CreateDroppedItem(dto.fuelItemId, Vector2.Zero)?.item;
            if (fuelSlot != null) { fuelSlot.itemInSlot = item; fuelSlot.amount = dto.fuelAmount; }
        }

        if (dto.resultItemId >= 0)
        {
            item = ItemFactory.CreateDroppedItem(dto.resultItemId, Vector2.Zero)?.item;
            if (resultSlot != null) { resultSlot.itemInSlot = item; resultSlot.amount = dto.resultAmount; }
        }

        smeltProgressNormalized = dto.smeltProgressNormalized;
        fuelTime = dto.fuelTime;
        active = dto.active;
    }

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
