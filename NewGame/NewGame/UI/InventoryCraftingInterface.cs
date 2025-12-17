using System.Linq;
using System.Collections.Generic;
using System.Numerics;

public class InventoryCraftingInterface : UserInterface, ISlotContainer
{
    public InventoryInterface ownerInventory;
    public List<CraftingSlot> craftingSlots = new List<CraftingSlot>();
    public WorkBenchComponent? component;

    private CraftingRecipe? selectedRecipe = null;
    private int selectedIndex = -1;

    private Rectangle recipeListRect;
    private Rectangle recipeItemRect;
    private Rectangle ingredientRect;
    private Rectangle craftButtonRect;

    public IEnumerable<Slot> Slots => craftingSlots.Cast<Slot>();

    public InventoryCraftingInterface()
    {
        name = "InventoryCrafting";

        float panelW = 520;
        float panelH = 160;
        interactionPanel = new Rectangle((Game.screenWidth - panelW) / 2, Game.screenHeight - panelH - 200, panelW, panelH);

        recipeListRect = new Rectangle(interactionPanel.X + 8, interactionPanel.Y + 8, 200, interactionPanel.Height - 16);
        recipeItemRect = new Rectangle(recipeListRect.X + 4, recipeListRect.Y + 4, recipeListRect.Width - 8, 28);
        ingredientRect = new Rectangle(recipeListRect.X + recipeListRect.Width + 10, interactionPanel.Y + 8, interactionPanel.Width - recipeListRect.Width - 30, 80);
        craftButtonRect = new Rectangle(ingredientRect.X, ingredientRect.Y + ingredientRect.Height + 8, 120, 36);

    }

    public override void Start()
    {
        base.Start();
        AddComponent<WorkBenchComponent>();
        component = GetComponent<WorkBenchComponent>();
        Initialize();
    }

    public void Initialize()
    {
        foreach (var r in component.availableRecipes)
        {
            var cs = new CraftingSlot(null);
            cs.craftingRecipe = r;
            cs.owner = this;
            craftingSlots.Add(cs);
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

        if (ownerInventory == null) return;
        if (!ownerInventory.showTiledInventory) return;
        if (SlotUtils.AnyOtherInterfaceOpen(ownerInventory)) return;

        var mouse = Raylib.GetMousePosition();

        for (int i = 0; i < craftingSlots.Count; i++)
        {
            var itemRect = new Rectangle(recipeItemRect.X, recipeItemRect.Y + i * (recipeItemRect.Height + 8), recipeItemRect.Width, recipeItemRect.Height);
            if (Raylib.CheckCollisionPointRec(mouse, itemRect))
            {
                selectedIndex = i;
                selectedRecipe = craftingSlots[i].craftingRecipe;
            }

            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, itemRect))
            {
                var player = WorldGeneration.Instance.playerRef;
                if (player != null && player.inventoryComponent != null)
                {
                    var recipe = craftingSlots[i].craftingRecipe;
                    bool canCraft = component.CanCraft(recipe, ownerInventory.inventoryComp);

                    // if (canCraft)
                    // {
                    //     foreach (var kv in recipe.ingredients)
                    //     {
                    //         var it = ItemFactory.CreateItem(kv.Key);
                    //         player.inventoryComponent.DecreaseItemAmount(it, kv.Value);
                    //     }

                    //     var created = ItemFactory.CreateItem(recipe.resultItemId);
                    //     var slot = craftingSlots[i];
                    //     UIDragContext.originSlot = slot;
                    //     UIDragContext.draggedItem = created;
                    //     UIDragContext.draggedCount = 1;
                    //     UIDragContext.isDragging = true;
                    // }
                }
            }
        }
    }

    public override void Draw()
    {
        // same visibility checks as Update
        if (ownerInventory == null) return;
        if (!ownerInventory.showTiledInventory) return;
        if (SlotUtils.AnyOtherInterfaceOpen(ownerInventory)) return;

        Raylib.DrawRectangleRec(interactionPanel, Core.UI_INTERACTION_PANEL_COLOR);
        Raylib.DrawRectangleLines((int)interactionPanel.X, (int)interactionPanel.Y, (int)interactionPanel.Width, (int)interactionPanel.Height, Color.Black);

        Raylib.DrawText("Crafting", (int)recipeListRect.X + 4, (int)recipeListRect.Y - 18, 18, Color.White);

        // draw recipe list
        for (int i = 0; i < craftingSlots.Count; i++)
        {
            var r = craftingSlots[i].craftingRecipe;
            var itemRect = new Rectangle(recipeItemRect.X, recipeItemRect.Y + i * (recipeItemRect.Height + 6), recipeItemRect.Width, recipeItemRect.Height);
            bool canCraft = false;
            var player = WorldGeneration.Instance.playerRef;
            if (player != null && player.inventoryComponent != null)
            {
                canCraft = true;
                foreach (var kv in r.ingredients)
                {
                    if (player.inventoryComponent.GetItemCount(kv.Key) < kv.Value) { canCraft = false; break; }
                }
            }

            Color bg = (i == selectedIndex) ? Color.LightGray : (canCraft ? Color.White : new Color(255, 255, 255, 80));
            Raylib.DrawRectangleRec(itemRect, bg);
            var data = ItemFactory.GetItemFromItemID(r.resultItemId);
            string name = data?.name ?? $"Item {r.resultItemId}";
            Raylib.DrawText(name, (int)itemRect.X + 6, (int)itemRect.Y + 6, 14, Color.Black);
        }

        if (selectedRecipe != null)
        {
            Raylib.DrawText("Ingredients:", (int)ingredientRect.X + 4, (int)ingredientRect.Y + 2, 16, Color.White);
            var player = WorldGeneration.Instance.playerRef;
            if (player != null && player.inventoryComponent != null)
            {
                int y = (int)ingredientRect.Y + 24;
                foreach (var kv in selectedRecipe.ingredients)
                {
                    short id = kv.Key;
                    int req = kv.Value;
                    int have = player.inventoryComponent.GetItemCount(id);
                    var itemData = ItemFactory.GetItemFromItemID(id);
                    string line = $"{itemData?.name ?? id.ToString()}: {have}/{req}";
                    Color col = (have >= req) ? Color.White : Color.Red;
                    Raylib.DrawText(line, (int)ingredientRect.X + 6, y, 14, col);
                    y += 18;
                }
            }
        }
    }
}
