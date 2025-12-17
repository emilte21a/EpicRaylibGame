using System.ComponentModel.DataAnnotations;

public static class SlotUtils
{
    public static Slot? hoveredSlot = null;

    static List<UserInterface> activeInterfaces = [];

    // Returns true if any registered interface is currently open
    public static bool AnyInterfaceOpen()
    {
        return activeInterfaces.Any(u => u != null && u is not InventoryInterface && u.IsOpen());
    }

    // Returns true if any open interface exists other than the provided one
    public static bool AnyOtherInterfaceOpen(UserInterface except)
    {
        return activeInterfaces.Any(u => u != null && u != except && u.IsOpen());
    }

    public static bool TryPlaceItemInSlot(Slot targetSlot, int amountToDropInSlot)
    {
        if (!activeInterfaces.Contains(targetSlot.owner)) return false;
        Console.WriteLine($"TryPlaceItemInSlot called. target={targetSlot?.GetType().Name}");

        if (!UIDragContext.isDragging || UIDragContext.draggedItem == null)
            return false;

        // Guard against a null target slot (fixes possible null reference diagnostics)
        if (targetSlot == null)
        {
            Console.WriteLine("TryPlaceItemInSlot: targetSlot is null -> rejecting drop");
            UIDragContext.Reset();
            return false;
        }

        if (targetSlot is InventorySlot invSlot && targetSlot.owner is InventoryInterface invOwner)
        {
            if (!invOwner.IsVisible(invSlot.index))
            {
                Console.WriteLine("TryPlaceItemInSlot: target inventory slot is not visible -> rejecting drop");
                return false;
            }
        }
        
        if (!targetSlot.CanAcceptItem(hoveredSlot!.itemInSlot!)) return false;

        var draggedItem = UIDragContext.draggedItem!;
        var draggedCount = UIDragContext.draggedCount;
        var originSlot = UIDragContext.originSlot;

        if (!CanPlaceInSlot(targetSlot, draggedItem))
            return false;

        if (originSlot == targetSlot)
        {
            if (originSlot != null && draggedItem != null)
            {
                originSlot.itemInSlot = draggedItem;
                originSlot.amount += amountToDropInSlot;
                draggedCount -= amountToDropInSlot;
                UIDragContext.draggedCount = draggedCount;
            }
            if (draggedCount <= 0)
                UIDragContext.Reset();
            return true;
        }

        Item? tempItem = targetSlot.itemInSlot;
        int tempAmount = targetSlot.amount;

        if (targetSlot.itemInSlot != null && targetSlot.itemInSlot.ID == draggedItem.ID) // Same item
        {
            targetSlot.amount += amountToDropInSlot;
            if (originSlot != null && originSlot.amount <= 0)
            {
                originSlot.itemInSlot = null;
                originSlot.amount = 0;
            }
            UIDragContext.Reset();
        }
        else if (targetSlot.itemInSlot == null && originSlot?.itemInSlot != null) //place item in another slot
        {
            targetSlot.itemInSlot = draggedItem;
            targetSlot.amount += amountToDropInSlot;
            draggedCount -= amountToDropInSlot;
            UIDragContext.draggedCount = draggedCount;
            if (draggedCount <= 0)
                UIDragContext.Reset();
        }
        else
        {
            targetSlot.itemInSlot = draggedItem;
            targetSlot.amount += amountToDropInSlot;
            draggedCount -= amountToDropInSlot;
            UIDragContext.draggedCount = draggedCount;
            if (originSlot != null)
            {
                originSlot.itemInSlot = tempItem;
                originSlot.amount += tempAmount;
            }
            if (draggedCount <= 0)
                UIDragContext.Reset();
        }

        Console.WriteLine($"Placed {targetSlot.itemInSlot?.name} x{targetSlot.amount} into {targetSlot.GetType().Name}");
        return true;
    }

    public static void Update()
    {
        hoveredSlot = null;
        foreach (var ui in activeInterfaces)
        {
            if (!ui.IsOpen()) continue;
            ui.Update();

            if (ui is InventoryInterface inventory)
            {
                foreach (var slot in inventory.inventoryComp.inventorySlots)
                {
                    if (!inventory.IsVisible(slot.index)) continue;
                    slot.Update();
                    if (slot.isHovered)
                    {
                        hoveredSlot = slot;
                        break;
                    }
                    if (hoveredSlot != null) break;
                }
            }
            else
            {
                if (ui is ISlotContainer slotContainer)
                {
                    foreach (var slot in slotContainer.Slots)
                    {
                        slot.Update();
                        if (slot.isHovered)
                        {
                            hoveredSlot = slot;
                            break;
                        }
                        if (hoveredSlot != null) break;
                    }
                }
            }

            // ui.isHovering = hoveredSlot != null;


            bool left = Raylib.IsMouseButtonPressed(MouseButton.Left);
            bool right = Raylib.IsMouseButtonPressed(MouseButton.Right);
            bool shift = Raylib.IsKeyPressed(KeyboardKey.LeftShift);
            if (shift && left)
            {
                if (hoveredSlot != null && hoveredSlot.itemInSlot != null)
                {
                    if (HandleShiftClick(hoveredSlot))
                        return;
                }
            }

            if (UIDragContext.isDragging)
            {

                if (left) HandleMouseClick(Raylib.GetMousePosition(), UIDragContext.draggedCount);
                else if (right) HandleMouseClick(Raylib.GetMousePosition(), 1);
                break;
            }
            else
            {
                if (ui.isHovering)
                {

                    if (left)
                    {
                        if (hoveredSlot != null && hoveredSlot.itemInSlot != null && hoveredSlot.owner != null && hoveredSlot.owner.IsOpen())
                            HandleLeftClickDrag();
                    }
                    else if (right)
                    {
                        if (hoveredSlot != null && hoveredSlot.itemInSlot != null && hoveredSlot.owner != null && hoveredSlot.owner.IsOpen())
                            HandleRightClickDrag();
                    }
                    break;
                }
            }
        }
    }

    static void HandleLeftClickDrag()
    {
        if (!hoveredSlot.CanBeDragged) return;
        UIDragContext.originSlot = hoveredSlot;
        UIDragContext.draggedItem = hoveredSlot.itemInSlot;
        UIDragContext.draggedCount = hoveredSlot.amount;
        UIDragContext.isDragging = true;

        hoveredSlot.itemInSlot = null;
        hoveredSlot.amount = 0;

    }

    static void HandleRightClickDrag()
    {
        if (!hoveredSlot.CanBeDragged) return;
        UIDragContext.originSlot = hoveredSlot;
        UIDragContext.draggedItem = hoveredSlot.itemInSlot;
        UIDragContext.draggedCount = (int)Math.Ceiling((double)hoveredSlot.amount / 2);
        UIDragContext.isDragging = true;

        if (hoveredSlot.amount == 1)
        {
            hoveredSlot.itemInSlot = null;
            hoveredSlot.amount = 0;
        }
        else
        {
            hoveredSlot.amount -= (int)Math.Ceiling((double)hoveredSlot.amount / 2);
            if (hoveredSlot.amount <= 0)
                hoveredSlot = null;
        }

    }

    public static void DrawDraggingSlot()
    {
        if (UIDragContext.isDragging && UIDragContext.draggedItem != null)
        {
            Vector2 mousePos = Raylib.GetMousePosition();
            int slotSize = Core.UI_SLOTSIZE;
            Raylib.DrawTexturePro(
                UIDragContext.draggedItem.texture,
                new Rectangle(0, 0, UIDragContext.draggedItem.texture.Width, UIDragContext.draggedItem.texture.Height),
                new Rectangle(mousePos.X - slotSize / 4, mousePos.Y - slotSize / 4, slotSize / 2, slotSize / 2),
                Vector2.Zero, 0, Color.White
            );
            if (UIDragContext.draggedCount > 0)
                Raylib.DrawText($"{UIDragContext.draggedCount}", (int)(mousePos.X - 10), (int)(mousePos.Y - 10), 30, Color.White);
        }
    }

    public static void HandleMouseClick(Vector2 mousePos, int amount)
    {
        if (!UIDragContext.isDragging) return;

        bool placed = false;

        foreach (var go in activeInterfaces)
        {
            if (go is ISlotContainer container && container.IsOpen())
            {
                if (container is InventoryInterface inv)
                {
                    foreach (var slot in inv.Slots.Cast<InventorySlot>())
                    {
                        if (!inv.IsVisible(slot.index)) continue;

                        if (Raylib.CheckCollisionPointRec(mousePos, slot.rectangle))
                        {
                            if (TryPlaceItemInSlot(slot, amount))
                            {
                                placed = true;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var slot in container.Slots)
                    {
                        if (Raylib.CheckCollisionPointRec(mousePos, slot.rectangle))
                        {
                            if (TryPlaceItemInSlot(slot, amount))
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
                TryPlaceItemInSlot(UIDragContext.originSlot, UIDragContext.draggedCount);
            }
            else
            {
                UIDragContext.Reset();
            }
        }
    }

    public static void AddInterface(UserInterface userInterface)
    {
        if (!activeInterfaces.Contains(userInterface))
        {
            activeInterfaces.Add(userInterface);
            System.Console.WriteLine("Add interface: " + userInterface);
        }
    }

    public static void RemoveInterface(UserInterface userInterface)
    {
        activeInterfaces.Remove(userInterface);
        System.Console.WriteLine("Remove interface: " + userInterface);
    }

    public static void Clear()
    {
        activeInterfaces.Clear();
    }

    public static List<UserInterface> GetInterfaces()
    {
        return activeInterfaces;
    }

    private static bool CanPlaceInSlot(Slot? targetSlot, Item draggedItem)
    {
        if (!targetSlot.CanAcceptItem(draggedItem)) return false;

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

        return true;
    }

    public static bool HandleShiftClick(Slot originSlot)
    {
        if (originSlot == null || originSlot.itemInSlot == null)
            return false;

        var item = originSlot.itemInSlot;
        int amount = originSlot.amount;

        foreach (var ui in activeInterfaces)
        {
            if (!ui.IsOpen()) continue;
            if (ui is InventoryInterface) continue;

            if (ui is ISlotContainer container)
            {
                foreach (var slot in container.Slots)
                {
                    if (CanPlaceInSlot(slot, item))
                    {
                        if (TryPlaceItemInSlot(slot, amount))
                            return true;
                    }
                }
            }
        }

        foreach (var ui in activeInterfaces)
        {
            if (ui is InventoryInterface inv && inv.IsOpen())
            {
                if (inv.showTiledInventory)
                {
                    int emptyIndex = inv.inventoryComp.FindFirstEmptyFromOriginIndex(originSlot is InventorySlot invSlot
                        ? invSlot.index
                        : 0);

                    if (emptyIndex >= 0)
                    {
                        var target = inv.inventoryComp.inventorySlots[emptyIndex];

                        if (TryPlaceItemInSlot(target, amount))
                            return true;
                    }
                }
            }
        }

        return false;
    }

}
