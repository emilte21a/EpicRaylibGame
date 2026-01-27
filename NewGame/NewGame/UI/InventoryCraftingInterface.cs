using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using LibNoise.Combiner;
using System.Linq.Expressions;

public class InventoryCraftingInterface : UserInterface, ISlotContainer
{
    public InventoryInterface ownerInventory;
    public WorkBenchComponent? component;
    public List<CraftingSlot> craftingSlots = new List<CraftingSlot>();

    private CraftingRecipe? selectedRecipe = null;
    private int selectedIndex = -1;

    private Rectangle recipeListRect;
    private Rectangle recipeItemRect;
    private Rectangle ingredientRect;

    List<UserInterface> exceptInterfaces = [];

    public IEnumerable<Slot> Slots => craftingSlots.Cast<Slot>();

    public InventoryCraftingInterface()
    {
        name = "InventoryCrafting";

        float panelW = 520;
        float panelH = 160;
        interactionPanel = new Rectangle((Game.screenWidth - panelW) / 2, Game.screenHeight / 2 - panelH, panelW, panelH);

        recipeListRect = new Rectangle(interactionPanel.X + 8, interactionPanel.Y + 8, 200, interactionPanel.Height - 16);
        recipeItemRect = new Rectangle(recipeListRect.X + 4, recipeListRect.Y + 4, recipeListRect.Width - 8, 28);
        ingredientRect = new Rectangle(recipeListRect.X + recipeListRect.Width + 10, interactionPanel.Y + 8, interactionPanel.Width - recipeListRect.Width - 30, 80);
    }

    public void Initialize()
    {
        foreach (var r in component.availableRecipes)
        {
            var cs = new CraftingSlot(r);
            craftingSlots.Add(cs);
        }
        int spacing = 8;
        for (int i = 0; i < craftingSlots.Count; i++)
        {
            var slot = craftingSlots[i];
            slot.itemInSlot = ItemFactory.CreateItem(slot.craftingRecipe.resultItemId);
            slot.owner = this;

            slot.rectangle.X = recipeItemRect.X + i * (recipeItemRect.Width + spacing);
            slot.rectangle.Y = recipeItemRect.Y;
        }
        exceptInterfaces.Add(this);
        exceptInterfaces.Add(ownerInventory);
    }

    public override void Update()
    {
        base.Update();

        if (ownerInventory == null) return;

        bool shouldBeOpen = ownerInventory.showTiledInventory &&
                       !SlotUtils.AnyOtherInterfaceOpen(exceptInterfaces);
        if (shouldBeOpen && !isOpen)
            Open();
        else if (!shouldBeOpen && isOpen)
            Close();

        var player = Game.player;
        if (player == null || component == null) return;

        for (int i = 0; i < craftingSlots.Count; i++)
        {
            if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), craftingSlots[i].rectangle))
            {
                selectedIndex = i;
                selectedRecipe = craftingSlots[i].craftingRecipe;
            }
        }
        foreach (var slot in craftingSlots)
        {
            if (UIDragContext.draggedItem != null && UIDragContext.draggedItem.ID != slot.craftingRecipe.resultItemId) continue;
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), slot.rectangle))
            {
                bool canCraft = component.CanCraft(slot.craftingRecipe, player.GetComponent<InventoryComponent>()!);
                if (canCraft)
                {
                    var created = component.Craft(slot.craftingRecipe, player.GetComponent<InventoryComponent>()!);
                    if (created != null)
                    {
                        UIDragContext.originSlot = slot;
                        UIDragContext.draggedItem = created;
                        UIDragContext.draggedCount++;
                        UIDragContext.isDragging = true;
                    }
                }
            }
        }
    }

    public override void Draw()
    {
        base.Draw();
        if (ownerInventory == null) return;
        if (!ownerInventory.showTiledInventory) return;
        if (SlotUtils.AnyOtherInterfaceOpen(exceptInterfaces)) return;

        Raylib.DrawRectangleRec(interactionPanel, Core.UI_INTERACTION_PANEL_COLOR);
        Raylib.DrawRectangleLines((int)interactionPanel.X, (int)interactionPanel.Y, (int)interactionPanel.Width, (int)interactionPanel.Height, Color.Black);

        Raylib.DrawText("Crafting", (int)recipeListRect.X + 4, (int)recipeListRect.Y - 18, 18, Color.White);
        for (int i = 0; i < craftingSlots.Count; i++)
        {
            var player = Game.player;
            var r = craftingSlots[i].craftingRecipe;
            bool canCraft = component.CanCraft(r, player.GetComponent<InventoryComponent>()!);

            Color tint = canCraft
                ? Color.White
                : new Color(255, 255, 255, 80);

            craftingSlots[i].itemColor = tint;
            craftingSlots[i].Draw();

            var itemData = ItemFactory.GetItemFromItemID(r.resultItemId);
            string name = itemData?.name ?? $"Item {r.resultItemId}";
            Raylib.DrawText(name, (int)craftingSlots[i].rectangle.X + 8, (int)craftingSlots[i].rectangle.Y + 8, 18, Color.White);
        }

        // if (selectedRecipe != null && component != null)
        // {
        //     Raylib.DrawText("Ingredients:", (int)ingredientRect.X + 8, (int)ingredientRect.Y + 4, 20, Color.White);

        //     var player = WorldGeneration.Instance.playerRef;

        //     if (player != null)
        //     {
        //         int y = (int)ingredientRect.Y + 36;

        //         foreach (var kv in selectedRecipe.ingredients)
        //         {
        //             short itemId = kv.Key;
        //             int required = kv.Value;
        //             int have = player.GetComponent<InventoryComponent>()!.GetItemCount(itemId);

        //             var itemData = ItemFactory.GetItemFromItemID(itemId);
        //             string name = itemData?.name ?? $"Item {itemId}";

        //             string line = $"{name}: {have}/{required}";
        //             Color col = (have >= required) ? Color.White : Color.Red;
        //             Raylib.DrawText(line, (int)ingredientRect.X + 8, y, 18, col);
        //             y += 22;
        //         }
        //     }
        // }
    }
}
