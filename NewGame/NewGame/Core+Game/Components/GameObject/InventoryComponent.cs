public class InventoryComponent : Component
{
    public List<InventorySlot> inventorySlots = [];

    public class InventoryStateDTO
    {
        public List<InventorySlot> inventorySlots = [];
    }

    public InventoryStateDTO ToDTO()
    {
        var dto = new InventoryStateDTO();
        inventorySlots.ForEach(i => dto.inventorySlots.Add(i));
        return dto;
    }

    public void FromDTO(InventoryStateDTO dto)
    {
        if (dto.inventorySlots.Count != 0)
        {
            inventorySlots = [.. dto.inventorySlots];
        }
    }

    public override void Start()
    {
        base.Start();
    }

    public override void Update()
    {
        base.Update();
    }

    public bool AddItem(Item item)
    {
        foreach (var slot in inventorySlots)
        {
            if (slot.itemInSlot != null && slot.itemInSlot.ID.Equals(item.ID))
            {
                slot.amount++;
                return true;
            }
        }

        for (int i = 0; i < Core.PLAYER_INVENTORY_SIZE - 1; i++)
        {
            if (inventorySlots[i].itemInSlot == null)
            {
                inventorySlots[i].itemInSlot = item;
                inventorySlots[i].amount = 1;
                return true;
            }
        }
        return false;
    }

    public void RemoveItem(Item item)
    {
        foreach (var slot in inventorySlots)
        {
            if (slot.itemInSlot != null && slot.itemInSlot.ID.Equals(item.ID))
            {
                slot.itemInSlot = null;
                break;
            }
        }
    }

    public void DecreaseItemAmount(Item item, int amountToDecrease)
    {
        if (item == null) return;

        foreach (var slot in inventorySlots.ToList())
        {
            if (slot.itemInSlot == null) continue;

            if (slot.itemInSlot.ID.Equals(item.ID))
            {
                slot.amount -= amountToDecrease;
                if (slot.amount <= 0)
                {
                    RemoveItem(slot.itemInSlot);
                }
            }

        }
    }

    public bool IsFull()
    {
        return !inventorySlots.Any(slot => slot.itemInSlot == null);
    }

    public int GetItemCount(short itemId)
    {
        var slot = inventorySlots.FirstOrDefault(s => s.itemInSlot != null && s.itemInSlot.ID == itemId);

        if (slot == null)
            return 0;

        return slot.amount;
    }

    public int FindFirstEmptyFromOriginIndex(int originIndex)
    {
        var index = 0;
        bool found = false;

        if (originIndex < Core.PLAYER_HOTBAR_SIZE)
        {
            for (int i = 0; i < Core.PLAYER_HOTBAR_SIZE; i++)
            {
                if (inventorySlots[i] != null) continue;

                index = i;
                found = true;
            }
        }
        else
        {
            for (int i = Core.PLAYER_HOTBAR_SIZE; i < Core.PLAYER_INVENTORY_SIZE; i++)
            {
                if (inventorySlots[i] != null) continue;

                index = i;
                found = true;
            }

        }
        return found ? index : originIndex;
    }
}