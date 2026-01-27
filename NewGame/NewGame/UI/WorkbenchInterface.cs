using System.Linq;
using System.Collections.Generic;
using System.Numerics;

public class WorkBenchInterface : UserInterface, ISlotContainer
{
    public WorkBench ownerTile;
    public WorkBenchComponent? component;

    public List<CraftingSlot> craftingSlots = new List<CraftingSlot>();

    private CraftingRecipe? selectedRecipe = null;
    private int selectedIndex = -1;

    private Rectangle recipeListRect;
    private Rectangle recipeItemRect;
    private Rectangle ingredientRect;

    public IEnumerable<Slot> Slots => craftingSlots.Cast<Slot>();

    public WorkBenchInterface()
    {
        interactionPanel = new Rectangle(100, 100, 900, 600);
        name = "Workbench";

        recipeListRect = new Rectangle(interactionPanel.X + 10, interactionPanel.Y + 10, 280, interactionPanel.Height - 20);
        recipeItemRect = new Rectangle(recipeListRect.X + 8, recipeListRect.Y + 8, Core.UI_SLOTSIZE, Core.UI_SLOTSIZE);
        ingredientRect = new Rectangle(recipeListRect.X + recipeListRect.Width + 20, interactionPanel.Y + 10, 360, 200);
    }

    public void Initialize()
    {
        foreach (var recipe in component.availableRecipes)
        {
            var cs = new CraftingSlot(recipe);
            craftingSlots.Add(cs);
        }
        for (int i = 0; i < craftingSlots.Count; i++)
        {
            var slot = craftingSlots[i];
            slot.itemInSlot = ItemFactory.CreateItem(craftingSlots[i].craftingRecipe.resultItemId);
            slot.owner = this;
            slot.rectangle.X = recipeItemRect.X;
            slot.rectangle.Y = recipeItemRect.Y + i * (recipeItemRect.Height + 8);
        }
    }

    public override void Update()
    {
        base.Update();

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
        Raylib.DrawRectangleRec(interactionPanel, Core.UI_INTERACTION_PANEL_COLOR);
        Raylib.DrawRectangleLinesEx(interactionPanel, 1, Color.Black);

        Raylib.DrawRectangleRec(recipeListRect, Color.Gray);
        Raylib.DrawText("Recipes", (int)recipeListRect.X + 8, (int)recipeListRect.Y + 4, 20, Color.White);

        for (int i = 0; i < craftingSlots.Count; i++)
        {
            var player = Game.player;
            var r = craftingSlots[i].craftingRecipe;
            bool canCraft = component.CanCraft(r, player.GetComponent<InventoryComponent>()!);
            var itemRect = new Rectangle(recipeItemRect.X, recipeItemRect.Y + i * (recipeItemRect.Height + 8), recipeItemRect.Width, recipeItemRect.Height);

            Color tint = canCraft
                ? Color.White
                : new Color(255, 255, 255, 80); // low opacity

            craftingSlots[i].itemColor = tint;
            craftingSlots[i].Draw();


            var itemData = ItemFactory.GetItemFromItemID(r.resultItemId);
            string name = itemData?.name ?? $"Item {r.resultItemId}";
            Raylib.DrawText(name, (int)itemRect.X + 8, (int)itemRect.Y + 8, 18, Color.White);
        }

        // Draw selected recipe ingredients and craft button
        if (selectedRecipe != null && component != null)
        {
            Raylib.DrawText("Ingredients:", (int)ingredientRect.X + 8, (int)ingredientRect.Y + 4, 20, Color.White);

            var player = Game.player;

            if (player != null)
            {
                int y = (int)ingredientRect.Y + 36;

                foreach (var kv in selectedRecipe.ingredients)
                {
                    short itemId = kv.Key;
                    int required = kv.Value;
                    int have = player.GetComponent<InventoryComponent>()!.GetItemCount(itemId);

                    var itemData = ItemFactory.GetItemFromItemID(itemId);
                    string name = itemData?.name ?? $"Item {itemId}";

                    string line = $"{name}: {have}/{required}";
                    Color col = (have >= required) ? Color.White : Color.Red;
                    Raylib.DrawText(line, (int)ingredientRect.X + 8, y, 18, col);
                    y += 22;
                }
            }
        }
    }
}