using Raylib_cs;

public static class UIDropManager
{
    public static void HandleMouseRelease(Vector2 mousePos)
    {
        if (!UIDragContext.isDragging) return;

        bool placed = false;

        foreach (var go in SlotUtils.GetInterfaces())
        {
            if (go is ISlotContainer container && container.IsOpen())
            {
                // If this is the player's Inventory, only consider visible slots.
                if (container is Inventory inv)
                {
                    foreach (var slot in inv.Slots.Cast<InventorySlot>())
                    {
                        bool isVisible = slot.index < inv.hotBarLength || (inv.showTiledInventory && slot.index <= inv.visualInventorySize);
                        if (!isVisible) continue;

                        if (Raylib.CheckCollisionPointRec(mousePos, slot.rectangle))
                        {
                            if (SlotUtils.TryPlaceItemInSlot(slot))
                            {
                                placed = true;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // Generic container (furnace/chest) - test all slots it exposes
                    foreach (var slot in container.Slots)
                    {
                        if (Raylib.CheckCollisionPointRec(mousePos, slot.rectangle))
                        {
                            if (SlotUtils.TryPlaceItemInSlot(slot))
                            {
                                placed = true;
                                break;
                            }
                        }
                    }
                }
            }
            if (placed) break;
        }

        if (!placed)
        {
            if (UIDragContext.originSlot != null)
            {
                SlotUtils.TryPlaceItemInSlot(UIDragContext.originSlot);
            }
            else
            {
                UIDragContext.Reset();
            }
        }
    }
}