using System.Linq;
using System.Collections.Generic;
using System.Numerics;

public class CraftingTableInterface : UserInterface, ISlotContainer
{
    public CraftingTableTile ownerTile;
    public CraftingTableComponent? component;

    public List<CraftingSlot> craftingSlots = new List<CraftingSlot>();

    private CraftingRecipe? selectedRecipe = null;
    private int selectedIndex = -1;

    // private ResultSlot resultSlot = new ResultSlot(null);

    private Rectangle recipeListRect;
    private Rectangle recipeItemRect;
    private Rectangle ingredientRect;
    private Rectangle craftButtonRect;
    private Rectangle resultSlotRect;

    Dictionary<CraftingRecipe, ResultSlot> craftingSlotsToResult = [];


    public IEnumerable<Slot> Slots => craftingSlots.Cast<Slot>().Concat(craftingSlotsToResult.Values);


    public CraftingTableInterface(CraftingTableComponent component)
    {
        interactionPanel = new Rectangle(100, 100, 900, 600);
        this.component = component;

        foreach (var recipe in component.availableRecipes)
        {
            var cs = new CraftingSlot(null);
            cs.craftingRecipe = recipe;
            craftingSlots.Add(cs);
            craftingSlotsToResult.Add(cs.craftingRecipe, new ResultSlot(null));
        }

        recipeListRect = new Raylib_cs.Rectangle(interactionPanel.X + 10, interactionPanel.Y + 10, 280, interactionPanel.Height - 20);
        recipeItemRect = new Raylib_cs.Rectangle(recipeListRect.X + 8, recipeListRect.Y + 8, Core.UI_SLOTSIZE, Core.UI_SLOTSIZE);
        ingredientRect = new Raylib_cs.Rectangle(recipeListRect.X + recipeListRect.Width + 20, interactionPanel.Y + 10, 360, 200);
        resultSlotRect = new Raylib_cs.Rectangle(ingredientRect.X, ingredientRect.Y + ingredientRect.Height + 20, Core.UI_SLOTSIZE, Core.UI_SLOTSIZE);
        craftButtonRect = new Raylib_cs.Rectangle(resultSlotRect.X + resultSlotRect.Width + 20, resultSlotRect.Y, 120, 40);


        foreach (var cfslot in craftingSlotsToResult)
        {
            cfslot.Value.owner = this;
            cfslot.Value.rectangle = new Rectangle(resultSlotRect.X, resultSlotRect.Y, resultSlotRect.Width, resultSlotRect.Height);
        }

        for (int i = 0; i < craftingSlots.Count; i++)
        {
            var slot = craftingSlots[i];
            slot.itemInSlot = ItemFactory.CreateItem(craftingSlots[i].craftingRecipe.resultItemId);
            slot.owner = this;
            slot.rectangle = new Rectangle(recipeItemRect.X, recipeItemRect.Y + i * (recipeItemRect.Height + 8), recipeItemRect.Width, recipeItemRect.Height);
        }
    }

    public override void Update()
    {
        base.Update();

        foreach (var slot in Slots)
        {
            slot.Update();
        }

        var player = WorldGeneration.Instance.playerRef;
        if (player == null || component == null) return;

        for (int i = 0; i < craftingSlots.Count; i++)
        {
            var itemRect = new Raylib_cs.Rectangle(recipeItemRect.X, recipeItemRect.Y + i * (recipeItemRect.Height + 8), recipeItemRect.Width, recipeItemRect.Height);
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), itemRect))
            {
                selectedIndex = i;
                selectedRecipe = craftingSlots[i].craftingRecipe;
                craftingSlotsToResult[selectedRecipe].Update();
            }
        }


        // resultSlot.Update();
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
            var r = craftingSlots[i].craftingRecipe;
            var itemRect = new Raylib_cs.Rectangle(recipeItemRect.X, recipeItemRect.Y + i * (recipeItemRect.Height + 8), recipeItemRect.Width, recipeItemRect.Height);
            craftingSlots[i].Draw();
            // Raylib.DrawRectangleRec(itemRect, bg);

            var itemData = ItemFactory.GetitemFromItemID(r.resultItemId);
            string name = itemData?.name ?? $"Item {r.resultItemId}";
            Raylib.DrawText(name, (int)itemRect.X + 8, (int)itemRect.Y + 8, 18, Color.White);
        }

        // Draw selected recipe ingredients and craft button
        if (selectedRecipe != null && component != null)
        {
            Raylib.DrawText("Ingredients:", (int)ingredientRect.X + 8, (int)ingredientRect.Y + 4, 20, Color.White);

            var player = WorldGeneration.Instance.playerRef;
            if (player != null)
            {
                int y = (int)ingredientRect.Y + 36;
                bool canCraft = component.CanCraft(selectedRecipe, player.GetComponent<InventoryComponent>()!);

                foreach (var kv in selectedRecipe.ingredients)
                {
                    short itemId = kv.Key;
                    int required = kv.Value;
                    int have = player.GetComponent<InventoryComponent>()!.GetItemCount(itemId);

                    var itemData = ItemFactory.GetitemFromItemID(itemId);
                    string name = itemData?.name ?? $"Item {itemId}";

                    string line = $"{name}: {have}/{required}";
                    Color col = (have >= required) ? Color.White : Color.Red;
                    Raylib.DrawText(line, (int)ingredientRect.X + 8, y, 18, col);
                    y += 22;
                }

                // draw result slot (Slot.Draw uses its rectangle)
                craftingSlotsToResult[selectedRecipe].Draw();

                // draw craft button (red when cannot craft)
                Color buttonColor = canCraft ? Color.White : Color.Red;
                Raylib.DrawRectangleRec(craftButtonRect, buttonColor);
                Raylib.DrawRectangleLines((int)craftButtonRect.X, (int)craftButtonRect.Y, (int)craftButtonRect.Width, (int)craftButtonRect.Height, Color.Black);
                Raylib.DrawText("Craft", (int)craftButtonRect.X + 20, (int)craftButtonRect.Y + 10, 20, Color.Black);

                if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), craftButtonRect))
                {
                    if (canCraft)
                    {
                        var created = component.Craft(selectedRecipe, player.GetComponent<InventoryComponent>()!);
                        if (created != null)
                        {
                            craftingSlotsToResult[selectedRecipe].itemInSlot = created;
                            craftingSlotsToResult[selectedRecipe].amount++;
                        }
                    }
                }
            }
        }
    }

    public bool HasResultAvailable() => craftingSlotsToResult[selectedRecipe!].itemInSlot != null;
}