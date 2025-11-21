public static class SlotUtils
{
    public static InventorySlot? hoveredSlot = null;

    static List<UserInterface> activeInterfaces = [];

    public static bool TryPlaceItemInSlot(Slot targetSlot)
    {
        Console.WriteLine($"TryPlaceItemInSlot called. target={targetSlot?.GetType().Name}");

        if (!UIDragContext.isDragging || UIDragContext.draggedItem == null)
            return false;

        if (targetSlot is InventorySlot invSlot && targetSlot.owner is Inventory invOwner)
        {
            bool isVisible = invSlot.index < invOwner.hotBarLength || (invOwner.showTiledInventory && invSlot.index <= invOwner.visualInventorySize);
            if (!isVisible)
            {
                Console.WriteLine("TryPlaceItemInSlot: target inventory slot is not visible -> rejecting drop");
                return false;
            }
        }

        var draggedItem = UIDragContext.draggedItem;
        var draggedCount = UIDragContext.draggedCount;
        var originSlot = UIDragContext.originSlot;

        if (targetSlot is ResultSlot)
        {
            Console.WriteLine("Cannot place item into furnace result slot.");
            return false;
        }

        if (targetSlot is FurnaceSlot)
        {
            bool isSmeltable = SmeltingRecipes.GetRecipeForInput(draggedItem) != null;
            var draggedId = (ItemFactory.ItemID)draggedItem.ID;
            bool isFuel = draggedId switch
            {
                ItemFactory.ItemID.coalore => true,
                ItemFactory.ItemID.wood => true,
                _ => false
            };

            if (!isSmeltable && !isFuel)
            {
                Console.WriteLine($"Item '{draggedItem.name}' is not valid for furnace (not smeltable nor fuel).");
                return false;
            }
        }

        if (originSlot == targetSlot)
        {
            if (originSlot != null && draggedItem != null)
            {
                originSlot.itemInSlot = draggedItem;
                originSlot.amount = draggedCount;
            }
            UIDragContext.Reset();
            return true;
        }

        Item? tempItem = targetSlot.itemInSlot;
        int tempAmount = targetSlot.amount;

        if (targetSlot.itemInSlot != null && targetSlot.itemInSlot.ID == draggedItem.ID)
        {
            targetSlot.amount += draggedCount;
            if (originSlot != null)
            {
                originSlot.itemInSlot = null;
                originSlot.amount = 0;
            }
        }
        else
        {
            targetSlot.itemInSlot = draggedItem;
            targetSlot.amount = draggedCount;
            if (originSlot != null)
            {
                originSlot.itemInSlot = tempItem;
                originSlot.amount = tempAmount;
            }
        }

        UIDragContext.Reset();
        Console.WriteLine($"Placed {targetSlot.itemInSlot?.name} x{targetSlot.amount} into {targetSlot.GetType().Name}");
        return true;
    }

    public static void Update()
    {
        hoveredSlot = null;
        Vector2 mousePos = Raylib.GetMousePosition();

        foreach (var ui in activeInterfaces)
        {
            if (!ui.IsOpen()) continue;

            if (ui is Inventory inventory)
            {
                foreach (var slot in inventory.inventorySlots)
                {
                    bool isVisible = slot.index < inventory.hotBarLength || (inventory.showTiledInventory && slot.index <= inventory.visualInventorySize);
                    if (!isVisible) continue;
                    slot.Update();
                    if (slot.isHovered) hoveredSlot = slot;
                }

            }
            else
            {
                // for other UIs that expose slots via ISlotContainer the UI itself will call slot.Update in its Update()
            }

            ui.isHovering = hoveredSlot != null;

            if (!UIDragContext.isDragging && Raylib.IsMouseButtonPressed(Raylib_cs.MouseButton.Left))
            {
                if (hoveredSlot != null && hoveredSlot.itemInSlot != null && ui.IsOpen())
                {
                    UIDragContext.originSlot = hoveredSlot;
                    UIDragContext.draggedItem = hoveredSlot.itemInSlot;
                    UIDragContext.draggedCount = hoveredSlot.amount;
                    UIDragContext.isDragging = true;

                    hoveredSlot.itemInSlot = null;
                    hoveredSlot.amount = 0;
                }
            }
        }
        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            UIDropManager.HandleMouseRelease(Raylib.GetMousePosition());
        }
    }

    public static void DrawDraggingSlot()
    {
        if (UIDragContext.isDragging && UIDragContext.draggedItem != null)
        {
            Vector2 mousePos = Raylib.GetMousePosition();
            int size = Core.UI_SLOTSIZE;
            Raylib.DrawTexturePro(
                UIDragContext.draggedItem.texture,
                new Rectangle(0, 0, UIDragContext.draggedItem.texture.Width, UIDragContext.draggedItem.texture.Height),
                new Rectangle(mousePos.X - size / 2, mousePos.Y - size / 2, size, size),
                Vector2.Zero, 0, Color.White
            );

            if (UIDragContext.draggedCount > 1)
                Raylib.DrawText($"{UIDragContext.draggedCount}", (int)(mousePos.X + 8), (int)(mousePos.Y + 8), 12, Color.White);
        }
    }

    public static void AddInterface(UserInterface userInterface)
    {
        if (!activeInterfaces.Contains(userInterface))
            activeInterfaces.Add(userInterface);
    }

    public static void RemoveInterface(UserInterface userInterface)
    {
        if (activeInterfaces.Contains(userInterface))
            activeInterfaces.Remove(userInterface);
    }

    public static List<UserInterface> GetInterfaces()
    {
        return activeInterfaces;
    }
}
