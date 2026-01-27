
public class InventoryInterface : UserInterface, ISlotContainer
{
    public InventoryComponent inventoryComp;
    public IEnumerable<Slot> Slots => inventoryComp.inventorySlots.Cast<Slot>();

    // crafting UI drawn above the inventory when tiled view is enabled
    public InventoryCraftingInterface? inventoryCraftingInterface;

    public int inventorySize = Core.PLAYER_INVENTORY_SIZE;
    public int hotBarLength = Core.PLAYER_HOTBAR_SIZE;
    public bool showTiledInventory = false;

    public int hotbarIndex;

    public InventoryInterface(InventoryComponent inventoryComponent)
    {
        name = "Inventory";
        inventoryComp = inventoryComponent;
        foreach (var slot in inventoryComp.inventorySlots)
        {
            slot.owner = this;
        }
        Initialize();
    }

    public override void Start()
    {
        base.Start();
        tag = "InventoryInterface";
    }

    public void InitializeInventoryInterface()
    {
        inventoryCraftingInterface = new InventoryCraftingInterface();
        inventoryCraftingInterface.ownerInventory = this;
        inventoryCraftingInterface.component = new WorkBenchComponent();

        inventoryCraftingInterface.component.SetupComponent(CraftingTier.tier0);
        
        SlotUtils.AddInterface(this);
        SlotUtils.AddInterface(inventoryCraftingInterface);
        inventoryCraftingInterface.Initialize();
        isOpen = true;
    }

    public override void Update()
    {
        base.Update();
        UpdateInventoryPosition();
        inventoryCraftingInterface?.Update();
    }

    public override void Draw()
    {
        Raylib.DrawRectangleRec(interactionPanel, Core.UI_INTERACTION_PANEL_COLOR);
        for (int i = 0; i < inventoryComp.inventorySlots.Count; i++)
        {
            var slot = inventoryComp.inventorySlots[i];
            if (slot.index < hotBarLength || (showTiledInventory && slot.index < inventorySize))
            {
                slot.Draw();
            }
            if (slot.index < hotBarLength)
            {
                Color borderColor = (slot.index == hotbarIndex) ? Color.Yellow : Color.Black;
                Raylib.DrawRectangleLinesEx(slot.rectangle, 3, borderColor);

            }
        }
        if (SlotUtils.hoveredSlot != null && SlotUtils.hoveredSlot.itemInSlot != null && !string.IsNullOrEmpty(SlotUtils.hoveredSlot.itemInSlot.description))
        {
            Raylib.DrawText(SlotUtils.hoveredSlot.itemInSlot.description, (int)SlotUtils.hoveredSlot.rectangle.X, (int)SlotUtils.hoveredSlot.rectangle.Y - 20, 20, Color.White);
        }

        inventoryCraftingInterface?.Draw();
    }
    public void HandleHotbarInput()
    {
        int wheel = (int)Raylib.GetMouseWheelMove();
        if (wheel != 0)
        {
            hotbarIndex -= wheel;
            if (hotbarIndex < 0) hotbarIndex = hotBarLength - 1;
            if (hotbarIndex >= hotBarLength) hotbarIndex = 0;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.One)) hotbarIndex = 0;
        if (Raylib.IsKeyPressed(KeyboardKey.Two)) hotbarIndex = 1;
        if (Raylib.IsKeyPressed(KeyboardKey.Three)) hotbarIndex = 2;
        if (Raylib.IsKeyPressed(KeyboardKey.Four)) hotbarIndex = 3;
        if (Raylib.IsKeyPressed(KeyboardKey.Five)) hotbarIndex = 4;
    }

    public Item? GetSelectedHotbarItem()
    {
        int idx = 0;
        foreach (var slot in inventoryComp.inventorySlots)
        {
            if (slot.index < hotBarLength)
            {
                if (idx == hotbarIndex)
                    return slot.itemInSlot;
                idx++;
            }
        }
        return null;
    }

    void Initialize()
    {
        int columns = hotBarLength;
        int rows = 2;
        int spacing = 10;

        int inventoryWidth = columns * Core.UI_SLOTSIZE + (columns - 1) * spacing;
        int startX = (Raylib.GetScreenWidth() - inventoryWidth) / 2;
        int hotbarY = Raylib.GetScreenHeight() - Core.UI_SLOTSIZE - spacing;
        int tiledStartY = hotbarY - (rows * Core.UI_SLOTSIZE + (rows - 1) * spacing) - spacing;

        for (int i = 0; i < inventorySize; i++)
        {
            InventorySlot slot = new(null)
            {
                index = i,
                owner = this
            };
            inventoryComp.inventorySlots.Add(slot);
        }

        foreach (var slot in inventoryComp.inventorySlots)
        {
            if (slot.index < hotBarLength)
            {
                slot.rectangle.X = startX + slot.index * (Core.UI_SLOTSIZE + spacing);
                slot.rectangle.Y = hotbarY;
            }
            else
            {
                int gridIndex = slot.index - hotBarLength;
                int col = gridIndex % columns;
                int row = gridIndex / columns;
                slot.rectangle.X = startX + col * (Core.UI_SLOTSIZE + spacing);
                slot.rectangle.Y = tiledStartY + row * (Core.UI_SLOTSIZE + spacing);
            }
        }

        if (showTiledInventory)
        {
            int height = rows * Core.UI_SLOTSIZE + (rows - 1) * spacing + Core.UI_SLOTSIZE + spacing * 2;
            interactionPanel = new Rectangle(startX - spacing / 2, tiledStartY - spacing / 2, inventoryWidth + spacing, height);
        }
        else
        {
            interactionPanel = new Rectangle(startX - spacing / 2, hotbarY - spacing / 2, inventoryWidth + spacing, Core.UI_SLOTSIZE + spacing);
        }
    }
    public void UpdateInventoryPosition()
    {
        int columns = hotBarLength;
        int rows = 2;
        int spacing = 10;
        int inventoryWidth = columns * Core.UI_SLOTSIZE + (columns - 1) * spacing;
        int startX = (Raylib.GetScreenWidth() - inventoryWidth) / 2;
        int hotbarY = Raylib.GetScreenHeight() - Core.UI_SLOTSIZE - spacing;
        int tiledStartY = hotbarY - (rows * Core.UI_SLOTSIZE + (rows - 1) * spacing) - spacing;

        for (int i = 0; i < inventorySize; i++)
        {
            var slot = inventoryComp.inventorySlots[i];
            if (slot.index < hotBarLength)
            {
                slot.rectangle.X = startX + slot.index * (Core.UI_SLOTSIZE + spacing);
                slot.rectangle.Y = hotbarY;
            }
            else
            {
                int gridIndex = slot.index - hotBarLength;
                int col = gridIndex % columns;
                int row = gridIndex / columns;
                slot.rectangle.X = startX + col * (Core.UI_SLOTSIZE + spacing);
                slot.rectangle.Y = tiledStartY + row * (Core.UI_SLOTSIZE + spacing);
            }
        }

        if (showTiledInventory)
        {
            int height = rows * Core.UI_SLOTSIZE + (rows - 1) * spacing + Core.UI_SLOTSIZE + spacing * 2;
            interactionPanel = new Rectangle(startX - spacing / 2, tiledStartY - spacing / 2, inventoryWidth + spacing, height);
        }
        else
        {
            interactionPanel = new Rectangle(startX - spacing / 2, hotbarY - spacing / 2, inventoryWidth + spacing, Core.UI_SLOTSIZE + spacing);
        }
    }

    public bool IsVisible(int index)
    {
        return index < hotBarLength || (showTiledInventory && index < inventorySize);
    }
}

